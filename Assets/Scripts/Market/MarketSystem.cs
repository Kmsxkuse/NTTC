using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Market
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MarketSystem : SystemBase
    {
        public static TextMeshProUGUI TickText;

        private static IReadOnlyDictionary<string, Entity> _tagLookup;
        private static NativeArray<Entity> _debugFactories;
        private static int _incrementCount = -1;

        private int _skipCounter, _totalCounter, _popNumber;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;

        private bool _initialization = true;

        private const int GoodsCount = 6;

        protected override void OnStartRunning()
        {
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // System is manually updated at a rate handled by in Timer.cs.
            if (_skipCounter++ < _incrementCount || _incrementCount == -1)
                return;
            _skipCounter = 0;
            TickText.text = (_totalCounter++).ToString();

            _popNumber = EntityManager.CreateEntityQuery(typeof(Population)).CalculateEntityCount();

            if (_initialization)
            {
                _initialization = false;
                DebugSpawnFactories();
                TagOwnedStates();
            
                _commandBufferSystem.AddJobHandleForProducer(Dependency);
            }
            
            Employment();
            CpClearingHouse();
        }

        private void TagOwnedStates()
        {
            var ecbCon = _commandBufferSystem.CreateCommandBuffer().ToConcurrent();
            
            Entities
                .WithName("Tag_States")
                .WithAll<Inhabited>()
                .ForEach((Entity entity, int entityInQueryIndex, ref State state) =>
                {
                    ref var provinces = ref state.StateToProv.Value.Lookup[state.Index];
                    var owner = GetComponent<Province>(provinces[0]).Owner;
                    for (var provIndex = 1; provIndex < provinces.Length; provIndex++)
                    {
                        if (GetComponent<Province>(provinces[provIndex]).Owner == owner)
                            continue;
                        
                        ecbCon.AddComponent<PartialOwnership>(entityInQueryIndex, entity);
                        return;
                    }

                    state.Owner = owner;
                }).ScheduleParallel();
        }

        private void CpClearingHouse()
        {
            // For all entities with inventory, process C and P in inventory then generates offers/bids for supply/demand.
            // Factories and province RGOs first.
            var factoryBids = new NativeMultiHashMap<BidKey, BidOffers>(
                EntityManager.CreateEntityQuery(typeof(Factory)).CalculateEntityCount() * GoodsCount,
                Allocator.TempJob);
            var fbCon = factoryBids.AsParallelWriter();
            Entities
                .WithName("Factory_CP_OB")
                .ForEach((Entity facEntity, ref DynamicBuffer<Inventory> inventory, in Identity identity,
                    in Factory factory, in Location location) =>
                {
                    ref var deltas = ref identity.MarketIdentity.Value.Deltas;
                    
                    // Calculate maximum production capacity in terms of workers depending on current inventory.
                    var maximumPossibleManPower = float.PositiveInfinity;
                    for (var goodIndex = 0; goodIndex < inventory.Length; goodIndex++)
                    {
                        if (deltas[goodIndex] >= 0)
                            continue;

                        // Consumption is indicated by negative delta value.
                        maximumPossibleManPower = math.min(maximumPossibleManPower, 
                            inventory[goodIndex].Value / -deltas[goodIndex]);
                    }

                    // Determine if there is enough workers to work maximum or if there is too much.
                    var goodsMultiplier = math.min(factory.TotalEmployed, maximumPossibleManPower);
                    
                    for (var goodIndex = 0; goodIndex < inventory.Length; goodIndex++)
                    {
                        // Apply consumption production pattern.
                        var targetInventory = inventory[goodIndex];
                        targetInventory.Value += goodsMultiplier * deltas[goodIndex];
                        inventory[goodIndex] = targetInventory;
                        
                        if (math.abs(inventory[goodIndex].Value - factory.TotalEmployed * deltas[goodIndex]) < 1)
                            continue;
                        
                        // Add bids to collector categorized by region and goods for region first exchange.
                        var quantity = math.min(factory.TotalEmployed * deltas[goodIndex], 0) + targetInventory.Value;
                        fbCon.Add(new BidKey(location.State, goodIndex, quantity), new BidOffers
                        {
                            Source = facEntity,
                            Quantity = math.abs(quantity)
                        });
                    }
                }).ScheduleParallel();
            
            // Processes state based factory clearing house.
            var inventories = GetBufferFromEntity<Inventory>();
            
            var countryBids = new NativeMultiHashMap<BidKey, BidOffers>(factoryBids.Count(), Allocator.Temp);
            var cbCon = countryBids.AsParallelWriter();
            Entities
                .WithName("Factory_State_CH")
                .WithReadOnly(factoryBids)
                .WithNativeDisableParallelForRestriction(inventories) // I know what I'm doing unity.
                .WithAll<Inhabited>()
                .ForEach((Entity entity) =>
                {
                    ClearingHouseResolve(entity, factoryBids, StateRemaining);

                    void StateRemaining(int remainingOffers, int goodIndex)
                    {
                        switch (remainingOffers)
                        {
                            case 0:
                                // For partial ownership of state. Dumping offers to next level country wide clearing.
                                TransferBids(BidKey.Transaction.Buy);
                                TransferBids(BidKey.Transaction.Sell);
                                break;
                            case 1:
                                // Sell offers ran out, buy offers left.
                                TransferBids(BidKey.Transaction.Buy);
                                break;
                            case 2:
                                // Buy offers ran out, sell offers left.
                                TransferBids(BidKey.Transaction.Sell);
                                break;
                            case 3:
                                // Both offers ran out.
                                break;
                        }

                        void TransferBids(BidKey.Transaction transaction)
                        {
                            // Transferring to country level bidding.
                            
                            if (!factoryBids.TryGetFirstValue(new BidKey(entity, goodIndex, transaction),
                                out var bid, out var iterator))
                                return;

                            do
                            {
                                var country = GetComponent<Province>(GetComponent<Location>(bid.Source).Province).Owner;
                                cbCon.Add(new BidKey(country, goodIndex, transaction), bid);
                            } while (factoryBids.TryGetNextValue(out bid, ref iterator));
                        }
                    }
                }).ScheduleParallel();
            
            factoryBids.Dispose(Dependency);
            
            // Process country based clearing house.
            
            
            throw new Exception("TEST!");
            
            void ClearingHouseResolve(Entity location, NativeMultiHashMap<BidKey, BidOffers> bids,
                Action<int, int> recordRemaining)
            {
                for (var goodIndex = 0; goodIndex < GoodsCount; goodIndex++)
                {
                    var remainingOffers = 0;
                        
                    if (HasComponent<PartialOwnership>(location))
                        // For state level clearing house skipping.
                        goto RecordRemaining;

                    if (!bids.TryGetFirstValue(new BidKey(location, goodIndex, BidKey.Transaction.Sell),
                        out var bidSell, out var iteratorSell))
                        remainingOffers += 1;
                        
                    if (!bids.TryGetFirstValue(new BidKey(location, goodIndex, BidKey.Transaction.Buy),
                        out var bidBuy, out var iteratorBuy))
                        remainingOffers += 2;
                        
                    if (remainingOffers != 0)
                        goto RecordRemaining;

                    while (true)
                    {
                        // Adapted from Simulation Dream.
                        var quantityTraded = math.min(bidBuy.Quantity, bidSell.Quantity);

                        if (quantityTraded > 0)
                        {
                            // Transferring units.
                            bidBuy.Quantity -= quantityTraded;
                            bidSell.Quantity -= quantityTraded;

                            // Adding to buyer
                            var targetInv = inventories[bidBuy.Source];
                            var inventory = targetInv[goodIndex];
                            inventory.Value += quantityTraded;
                            targetInv[goodIndex] = inventory;
                                
                            // Subtracting from seller
                            targetInv = inventories[bidSell.Source];
                            inventory = targetInv[goodIndex];
                            inventory.Value -= quantityTraded;
                            targetInv[goodIndex] = inventory;
                        }
                            
                        if (bidSell.Quantity < 0.1 && !bids.TryGetNextValue(out bidSell, ref iteratorSell))
                            remainingOffers += 1;
                        if (bidBuy.Quantity < 0.1 && !bids.TryGetNextValue(out bidBuy, ref iteratorBuy)) 
                            remainingOffers += 2;
                        
                        if (remainingOffers != 0)
                            goto RecordRemaining;
                    }
                        
                    RecordRemaining:
                    recordRemaining(remainingOffers, goodIndex);
                }
            }
        }

        private void Employment()
        {
            // Organize pops into multi hash map by state
            var popsByState = new NativeMultiHashMap<Entity, Entity>(_popNumber, Allocator.TempJob);

            var pbsInput = popsByState.AsParallelWriter();
            Entities
                .WithName("State_Pops")
                .WithAll<Population>()
                .ForEach((Entity entity, in Location location) =>
                {
                    pbsInput.Add(location.State, entity);
                }).ScheduleParallel();

            // Assign pops to jobs. Assuming max employment regardless of financial situation.
            var popJobOpportunities = new NativeMultiHashMap<Entity, PopEmployment>(_popNumber, Allocator.TempJob);
            var factoryEmploymentNumbers = new NativeHashMap<Entity, int>(
                EntityManager.CreateEntityQuery(typeof(Factory),
                ComponentType.Exclude<RgoGood>()).CalculateEntityCount(), Allocator.TempJob);
            var pjoCon = popJobOpportunities.AsParallelWriter();
            var fenCon = factoryEmploymentNumbers.AsParallelWriter();
            Entities
                .WithName("Match_Factories_W_Pops")
                .WithReadOnly(popsByState)
                .WithAll<Inhabited>()
                .ForEach((Entity entity, int entityInQueryIndex, in DynamicBuffer<FactoryWrapper> factories, in State state) =>
                {
                    if (!popsByState.TryGetFirstValue(entity, out var popEntity, out var iterator))
                        return;

                    var currentAvailableToBeEmployed = GetComponent<Population>(popEntity).Quantity;
                    
                    // Foreach not supported in burst compiled jobs.
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var factoryIndex = 0; factoryIndex < factories.Length; factoryIndex++)
                    {
                        var targetFactory = factories[factoryIndex];
                        var remainingCapacity = GetComponent<Factory>(targetFactory).MaximumEmployment;
                        while (true)
                        {
                            remainingCapacity -= currentAvailableToBeEmployed;
                            if (remainingCapacity > 0)
                            {
                                AssignPopEmployment(currentAvailableToBeEmployed);
                                if (!popsByState.TryGetNextValue(out popEntity, ref iterator))
                                {
                                    SetTotalEmployed();
                                    return;
                                }
                                currentAvailableToBeEmployed = GetComponent<Population>(popEntity).Quantity;
                                continue;
                            }

                            if (remainingCapacity == 0)
                            {
                                // Wow, perfect fit!
                                AssignPopEmployment(currentAvailableToBeEmployed);
                                currentAvailableToBeEmployed = 0;
                                break;
                            }

                            AssignPopEmployment(remainingCapacity + currentAvailableToBeEmployed);
                            currentAvailableToBeEmployed = -remainingCapacity;
                            remainingCapacity = 0;
                            break;
                        }
                        
                        SetTotalEmployed();

                        void SetTotalEmployed()
                        {
                            fenCon.TryAdd(targetFactory, remainingCapacity);

                            //ecbConAssignment.SetComponent(entityInQueryIndex, targetFactory, factory);
                        }

                        void AssignPopEmployment(int numEmployed)
                        {
                            pjoCon.Add(popEntity, new PopEmployment
                            {
                                Factory = targetFactory,
                                Employed = numEmployed
                            });
                        }
                    }
                }).ScheduleParallel();
            
            popsByState.Dispose(Dependency);

            var rgoUnemployed = new NativeMultiHashMap<Entity, int>(_popNumber, Allocator.TempJob);
            var ruCon = rgoUnemployed.AsParallelWriter();
            Entities
                .WithName("Reflect_Employment_on_Pops")
                .WithReadOnly(popJobOpportunities)
                .ForEach((Entity entity, ref DynamicBuffer<PopEmployment> popEmployments, ref Population population, in Location location) =>
                {
                    // Reset employment numbers.
                    popEmployments.Clear();
                    population.Employed = 0;
                    
                    if (popJobOpportunities.TryGetFirstValue(entity, out var popEmployment, out var iterator))
                        do
                        {
                            popEmployments.Add(popEmployment);
                            population.Employed += popEmployment.Employed;
                        } while (popJobOpportunities.TryGetNextValue(out popEmployment, ref iterator));
                    
                    // Unemployed. Goes to province RGO.
                    ruCon.Add(location.Province, population.Quantity - population.Employed);
                }).ScheduleParallel();

            popJobOpportunities.Dispose(Dependency);

            Entities
                .WithName("Set_Factory_Employment")
                .WithReadOnly(factoryEmploymentNumbers)
                .WithNone<RgoGood>()
                .ForEach((Entity entity, ref Factory factory) =>
                {
                    if (!factoryEmploymentNumbers.TryGetValue(entity, out var remainingUnfilled))
                        return;
                    factory.TotalEmployed = factory.MaximumEmployment - remainingUnfilled;
                }).ScheduleParallel();

            factoryEmploymentNumbers.Dispose(Dependency);

            Entities
                .WithName("Set_RGO_Employment")
                .WithReadOnly(rgoUnemployed)
                .WithAll<RgoGood>()
                .ForEach((ref Factory rgoFactory, in Location location) =>
                {
                    rgoFactory.TotalEmployed = 0;
                    
                    if (!rgoUnemployed.TryGetFirstValue(location.Province, out var popEmployment, out var iterator)) 
                        return;
                    do
                    {
                        rgoFactory.TotalEmployed += popEmployment;
                    } while (rgoUnemployed.TryGetNextValue(out popEmployment, ref iterator));
                }).ScheduleParallel();

            rgoUnemployed.Dispose(Dependency);
        }
        
        public static ref int GetIncrementCount()
        {
            // Used in Timer to bootstrap between UI buttons and actual update frequency.
            return ref _incrementCount;
        }

        public static void SetDebugValues(BlobAssetReference<MarketMatrix>[] marketIdentities, int[] maxEmploy)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _debugFactories = new NativeArray<Entity>(new []
            {
                em.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory), typeof(Identity), typeof(Location)),
                em.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory), typeof(Identity), typeof(Location)),
                em.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory), typeof(Identity), typeof(Location))
            }, Allocator.Persistent);
            
            em.SetComponentData(_debugFactories[0], new Factory
            {
                MaximumEmployment = maxEmploy[3],
                TotalEmployed = 0
            });
            em.SetComponentData(_debugFactories[1], new Factory
            {
                MaximumEmployment = maxEmploy[4],
                TotalEmployed = 0
            });
            em.SetComponentData(_debugFactories[2], new Factory
            {
                MaximumEmployment = maxEmploy[5],
                TotalEmployed = 0
            });
            using (var emptyInventory = new NativeArray<Inventory>(GoodsCount, Allocator.Temp))
            {
                em.GetBuffer<Inventory>(_debugFactories[0]).AddRange(emptyInventory);
                em.GetBuffer<Inventory>(_debugFactories[1]).AddRange(emptyInventory);
                em.GetBuffer<Inventory>(_debugFactories[2]).AddRange(emptyInventory);
            }
            em.SetComponentData<Identity>(_debugFactories[0], marketIdentities[3]);
            em.SetComponentData<Identity>(_debugFactories[1], marketIdentities[4]);
            em.SetComponentData<Identity>(_debugFactories[2], marketIdentities[5]);
            em.SetComponentData(_debugFactories[0], new Wallet {Wealth = 1000});
            em.SetComponentData(_debugFactories[1], new Wallet {Wealth = 1000});
            em.SetComponentData(_debugFactories[2], new Wallet {Wealth = 1000});
        }

        private void DebugSpawnFactories()
        {
            var ecbCon = _commandBufferSystem.CreateCommandBuffer().ToConcurrent();
            var debugFactories = _debugFactories;

            var uncolonizedEntity = new NativeArray<Entity>(1, Allocator.Temp)
            {
                [0] = _tagLookup["UNCOLONIZED"]
            };

            // Spawns random 4, 5, or 6 factory in every region with an owned province.
            Entities
                .WithAll<Inhabited>()
                .WithReadOnly(debugFactories)
                .WithReadOnly(uncolonizedEntity)
                .ForEach((Entity entity, int entityInQueryIndex, in State state) =>
                {
                    // Check for state initial inhabited status done in Province Load.
                    
                    ref var provLookup = ref state.StateToProv.Value;
                    ref var provInState = ref provLookup.Lookup[state.Index];

                    var rand = new Random((uint) ((entityInQueryIndex + 21) * (state.Index + 12)));
                    var numFactories = rand.NextInt(3, 10); // 3 to 9

                    for (var cursor = 0; cursor < numFactories; cursor++)
                    {
                        var province = provInState[rand.NextInt(provInState.Length)];
                        var owner = GetComponent<Province>(province).Owner;

                        // Uncolonized provinces should not have factories within them.
                        var loopCounter = 0;
                        while (owner == uncolonizedEntity[0])
                        {
                            province = provInState[rand.NextInt(provInState.Length)];
                            owner = GetComponent<Province>(province).Owner;
                            
                            if (loopCounter++ > 20)
                                throw new Exception("Uncolonized province search timed out!");
                        }
                        
                        var targetFactory = ecbCon.Instantiate(entityInQueryIndex, debugFactories[rand.NextInt(3)]);
                        ecbCon.SetComponent(entityInQueryIndex, targetFactory, 
                            new Location(province, entity));
                        // Can not use the dynamic buffer in for each lambda title as target factory is not yet created.
                        ecbCon.AppendToBuffer<FactoryWrapper>(entityInQueryIndex, entity, targetFactory);
                    }
                }).ScheduleParallel();

            debugFactories.Dispose(Dependency);
            uncolonizedEntity.Dispose(Dependency);
        }
    }
}