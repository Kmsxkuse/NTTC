using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Market
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MarketSystem : SystemBase
    {
        public static TextMeshProUGUI TickText;
        
        private static NativeArray<Entity> _debugFactories;
        private static int _incrementCount = -1;

        private int _skipCounter, _totalCounter, _popNumber;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;

        private bool _initialization = true;

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
            }
            
            ProcessEmployment();
            //PopMergeByProvince();

            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }

        private void ProcessEmployment()
        {
            // Organize pops into multi hash map by state
            var popsByState = new NativeMultiHashMap<Entity, Entity>(_popNumber, Allocator.TempJob);

            var pbsInput = popsByState.AsParallelWriter();
            Entities
                .WithAll<Population>()
                .ForEach((Entity entity, in Location location) =>
                {
                    pbsInput.Add(location.State, entity);
                }).WithBurst().ScheduleParallel();
            
            // Clearing old employment lists
            Entities
                .ForEach((ref DynamicBuffer<PopEmployment> employmentLists, ref Population population) =>
                {
                    employmentLists.Clear();
                    population.Employed = 0;
                }).WithBurst().ScheduleParallel();

            // Assign pops to jobs. Assuming max employment regardless of financial situation.
            var popJobOpportunities = new NativeMultiHashMap<Entity, PopEmployment>(_popNumber, Allocator.TempJob);
            var pjoCon = popJobOpportunities.AsParallelWriter();
            var ecbConAssignment = _commandBufferSystem.CreateCommandBuffer().ToConcurrent();
            Entities
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
                        var factory = GetComponent<Factory>(targetFactory);
                        var remainingCapacity = factory.MaximumEmployment;
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
                            factory.TotalEmployed = factory.MaximumEmployment - remainingCapacity;
                            ecbConAssignment.SetComponent(entityInQueryIndex, targetFactory, factory);
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
                }).WithBurst().ScheduleParallel();
            
            popsByState.Dispose(Dependency);

            Entities
                .WithReadOnly(popJobOpportunities)
                .ForEach((Entity entity, ref DynamicBuffer<PopEmployment> popEmployments, ref Population population) =>
                {
                    if (!popJobOpportunities.TryGetFirstValue(entity, out var popEmployment, out var iterator))
                        return;

                    do
                    {
                        popEmployments.Add(popEmployment);
                        population.Employed += popEmployment.Employed;
                    } while (popJobOpportunities.TryGetNextValue(out popEmployment, ref iterator));
                }).WithBurst().ScheduleParallel();

            popJobOpportunities.Dispose(Dependency);
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
                em.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory), typeof(Identity)),
                em.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory), typeof(Identity)),
                em.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory), typeof(Identity))
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
            using (var emptyInventory = new NativeArray<Inventory>(6, Allocator.Temp))
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

        protected override void OnDestroy()
        {
            //_debugFactories.Dispose();
        }

        private void DebugSpawnFactories()
        {
            var ecbCon = _commandBufferSystem.CreateCommandBuffer().ToConcurrent();
            var debugFactories = _debugFactories;
            
            // Spawns random 4, 5, or 6 factory in every region with an owned province.
            Entities
                .WithAll<Inhabited>()
                .WithDeallocateOnJobCompletion(debugFactories)
                .ForEach((Entity entity, int entityInQueryIndex, in State state) =>
                {
                    // Check for state initial inhabited status done in Province Load.

                    var rand = new Random((uint) ((entityInQueryIndex + 21) * (state.Index + 12)));
                    var numFactories = rand.NextInt(3, 10); // 3 to 9
                    for (var cursor = 0; cursor < numFactories; cursor++)
                        ecbCon.AppendToBuffer<FactoryWrapper>(entityInQueryIndex, entity, 
                            ecbCon.Instantiate(entityInQueryIndex, debugFactories[rand.NextInt(3)]));
                }).WithBurst().ScheduleParallel();
        }
    }
}