using System;
using System.Collections.Generic;
using Conversion;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Market
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MarketSystem : SystemBase
    {
        public static TextMeshProUGUI TickText;
        
        private static int _incrementCount = -1;

        private int _skipCounter, _totalCounter, _popNumber;

        private const int GoodsCount = LoadChain.GoodNum;

        protected override void OnUpdate()
        {
            // System is manually updated at a rate handled by in Timer.cs.
            if (_skipCounter++ < _incrementCount || _incrementCount == -1)
                return;
            _skipCounter = 0;
            TickText.text = (_totalCounter++).ToString();

            _popNumber = EntityManager.CreateEntityQuery(typeof(Population)).CalculateEntityCount();
            
            // Employment calculations every week.
            if (_totalCounter % 7 == 0)
                Employment();

            CpClearingHouse();
        }

        private void CpClearingHouse()
        {
            // For all entities with inventory, process C and P in inventory then generates offers/bids for supply/demand.
            // Factories and province RGOs first.
            var bidCapacity = EntityManager.CreateEntityQuery(typeof(Factory)).CalculateEntityCount()
                              * GoodsCount;
            
            var factoryBids = new NativeMultiHashMap<BidKey, BidOffers>(bidCapacity, Allocator.TempJob);
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
            
            var processedBids = new NativeMultiHashMap<Entity, BidRecord>(bidCapacity, Allocator.TempJob);
            var pbCon = processedBids.AsParallelWriter();
            
            var countryBids = new NativeMultiHashMap<BidKey, BidOffers>(bidCapacity, Allocator.TempJob);
            var cbCon = countryBids.AsParallelWriter();
            
            Entities
                .WithName("Factory_State_CH")
                .WithReadOnly(factoryBids)
                .WithAll<Inhabited>()
                .ForEach((Entity entity, ref DynamicBuffer<Inventory> goodsTraded, in State state) =>
                {
                    // Unity and Burst does NOT like generic functions with Action parameters.
                    // Thus, this is copied for both state and country bid completion.
                    
                    for (var goodIndex = 0; goodIndex < GoodsCount; goodIndex++)
                    {
                        var remainingOffers = 0;
                        var totalTraded = 0f;
                        
                        if (HasComponent<PartialOwnership>(entity))
                            // For state level clearing house skipping.
                            goto RecordRemaining;

                        if (!factoryBids.TryGetFirstValue(new BidKey(entity, goodIndex, BidKey.Transaction.Sell),
                            out var bidSell, out var iteratorSell))
                            remainingOffers += 1;
                        
                        if (!factoryBids.TryGetFirstValue(new BidKey(entity, goodIndex, BidKey.Transaction.Buy),
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
                                pbCon.Add(bidBuy.Source, new BidRecord
                                {
                                    Good = goodIndex,
                                    Quantity = quantityTraded
                                });
                                
                                // Adding to seller
                                pbCon.Add(bidSell.Source, new BidRecord
                                {
                                    Good = goodIndex,
                                    Quantity = -quantityTraded
                                });
                                
                                // Appending trading.
                                totalTraded += quantityTraded;

                                /* Done on a separate job.
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
                                */
                            }
                            
                            if (bidSell.Quantity < 0.1 && !factoryBids.TryGetNextValue(out bidSell, ref iteratorSell))
                                remainingOffers += 1;
                            if (bidBuy.Quantity < 0.1 && !factoryBids.TryGetNextValue(out bidBuy, ref iteratorBuy)) 
                                remainingOffers += 2;
                        
                            if (remainingOffers != 0)
                                goto RecordRemaining;
                        }
                        
                        RecordRemaining:
                        switch (remainingOffers)
                        {
                            case 0:
                                // For partial ownership of state. Dumping offers to next level country wide clearing.
                                TransferBids(BidKey.Transaction.Buy);
                                TransferBids(BidKey.Transaction.Sell);
                                
                                // Total transactions within state should be zero.
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
                        
                        goodsTraded[goodIndex] = new Inventory(totalTraded);

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
            
            // Summing state goods traded and transferring to country.
            
            // Process country based clearing house.
            Entities
                .WithReadOnly(countryBids)
                .WithAll<Country>()
                .ForEach((Entity entity, ref DynamicBuffer<Prices> prices, in DynamicBuffer<Inventory> goodsTradedInStates) =>
                {
                    // Unity and Burst does NOT like generic functions with Action parameters.
                    // Thus, this is copied for both state and country bid completion.
                    
                    for (var goodIndex = 0; goodIndex < GoodsCount; goodIndex++)
                    {
                        var remainingOffers = 0;

                        if (!countryBids.TryGetFirstValue(new BidKey(entity, goodIndex, BidKey.Transaction.Sell),
                            out var bidSell, out var iteratorSell))
                            remainingOffers += 1;
                        
                        if (!countryBids.TryGetFirstValue(new BidKey(entity, goodIndex, BidKey.Transaction.Buy),
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
                                pbCon.Add(bidBuy.Source, new BidRecord
                                {
                                    Good = goodIndex,
                                    Quantity = quantityTraded
                                });
                                
                                // Adding to seller
                                pbCon.Add(bidSell.Source, new BidRecord
                                {
                                    Good = goodIndex,
                                    Quantity = -quantityTraded
                                });
                            }
                            
                            if (bidSell.Quantity < 0.1 && !countryBids.TryGetNextValue(out bidSell, ref iteratorSell))
                                remainingOffers += 1;
                            if (bidBuy.Quantity < 0.1 && !countryBids.TryGetNextValue(out bidBuy, ref iteratorBuy)) 
                                remainingOffers += 2;
                        
                            if (remainingOffers != 0)
                                goto RecordRemaining;
                        }
                        
                        RecordRemaining:
                        CountryRemaining(remainingOffers, goodIndex);
                    }

                    void CountryRemaining(int remainingOffers, int goodIndex)
                    {
                        // Calculating final price using remaining goods.
                        
                        // Passing to international clearing house.
                        // TBD
                    }
                }).ScheduleParallel();

            countryBids.Dispose(Dependency);
            
            
            processedBids.Dispose(Dependency);
            //throw new Exception("TEST!");
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
    }
}