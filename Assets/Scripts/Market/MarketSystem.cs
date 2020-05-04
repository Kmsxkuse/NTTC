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

        private int _skipCounter, _totalCounter;
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

            if (_initialization)
            {
                _initialization = false;
                DebugSpawnFactories();
            }
            
            // Organize pops into multi hash map by state
            var popsByState = new NativeMultiHashMap<Entity, Entity>(
                EntityManager.CreateEntityQuery(typeof(Population)).CalculateEntityCount(),
                Allocator.TempJob);

            var pbsInput = popsByState.AsParallelWriter();
            Entities
                .WithAll<Population>()
                .ForEach((Entity entity, in Location location) =>
                {
                    pbsInput.Add(location.State, entity);
                }).WithBurst().ScheduleParallel();

            
            // Assign pops to jobs. Assuming max employment regardless of financial situation.
            /*
            var ecbCon = _commandBufferSystem.CreateCommandBuffer().ToConcurrent();
            Entities
                .WithReadOnly(popsByState)
                .WithAll<Inhabited>()
                .ForEach((Entity entity, int entityInQueryIndex, in DynamicBuffer<FactoryWrapper> factories, in State state) =>
                {
                    Debug.Log("T1");
                    if (!popsByState.TryGetFirstValue(entity, out var popEntity, out var iterator))
                        return;
                    Debug.Log("T2");
                    for (var factoryIndex = 0; factoryIndex < factories.Length; factoryIndex++)
                    {
                        Debug.Log("T3");
                        var targetFactory = factories[factoryIndex];
                        var remainingCapacity = GetComponent<Factory>(targetFactory).MaximumEmployment;
                        while (true)
                        {
                            Debug.Log("T4");
                            var popQuantity = GetComponent<Population>(popEntity).Quantity;
                            remainingCapacity -= popQuantity;
                            if (remainingCapacity > 0)
                            {
                                ecbCon.AppendToBuffer<Employee>(entityInQueryIndex, targetFactory, popEntity);
                                ecbCon.SetComponent<Employer>(entityInQueryIndex, popEntity, targetFactory.Factory);
                                if (!popsByState.TryGetNextValue(out popEntity, ref iterator))
                                    return;
                                continue;
                            }

                            if (remainingCapacity == 0)
                                // Wow, perfect fit!
                                break;
                            
                            // Split pop.
                            var employed = ecbCon.Instantiate(entityInQueryIndex, popEntity);
                            
                            var oldPopData = GetComponent<Population>(employed);
                            oldPopData.Quantity = popQuantity + remainingCapacity;
                            
                            ecbCon.SetComponent(entityInQueryIndex, employed, oldPopData);
                            ecbCon.AppendToBuffer<Employee>(entityInQueryIndex, targetFactory, employed);
                            ecbCon.SetComponent<Employer>(entityInQueryIndex, employed, targetFactory.Factory);

                            oldPopData.Quantity = -remainingCapacity;
                            ecbCon.SetComponent(entityInQueryIndex, popEntity, oldPopData);
                            break;
                        }
                    }
                }).WithoutBurst().Schedule();
                */

            popsByState.Dispose(Dependency);
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
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
                em.CreateEntity(typeof(Factory), typeof(Wallet),
                    typeof(Employee), typeof(Inventory), typeof(Identity)),
                em.CreateEntity(typeof(Factory), typeof(Wallet),
                    typeof(Employee), typeof(Inventory), typeof(Identity)),
                em.CreateEntity(typeof(Factory), typeof(Wallet),
                    typeof(Employee), typeof(Inventory), typeof(Identity))
            }, Allocator.Persistent);
            
            em.SetComponentData(_debugFactories[0], new Factory
            {
                MaximumEmployment = maxEmploy[3]
            });
            em.SetComponentData(_debugFactories[1], new Factory
            {
                MaximumEmployment = maxEmploy[4]
            });
            em.SetComponentData(_debugFactories[2], new Factory
            {
                MaximumEmployment = maxEmploy[5]
            });
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
                    var numFactories = rand.NextInt(4); // 0 to 3
                    for (var cursor = 0; cursor < numFactories; cursor++)
                        ecbCon.AppendToBuffer<FactoryWrapper>(entityInQueryIndex, entity, 
                            ecbCon.Instantiate(entityInQueryIndex, debugFactories[rand.NextInt(3)]));
                }).WithBurst().ScheduleParallel();
        }
    }
}