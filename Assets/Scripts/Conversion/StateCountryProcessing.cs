using System;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Conversion
{
    [DisableAutoCreation]
    public class StateCountryProcessing : SystemBase
    {
        public static Entity UncolonizedEntity;

        public static ManualMethodCall CallMethod;

        public enum ManualMethodCall
        {
            // Used by update to determine which method to call.
            TagOwnedStates,
            DebugSpawnFactories
        }
        
        private static NativeArray<Entity> _debugFactories;
        
        protected override void OnUpdate()
        {
            switch (CallMethod)
            {
                case ManualMethodCall.TagOwnedStates:
                    TagOwnedStates();
                    break;
                case ManualMethodCall.DebugSpawnFactories:
                    DebugSpawnFactories();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return;
        }

        private void TagOwnedStates()
        {
            Entities
                .WithName("Tag_States")
                .WithStructuralChanges()
                .WithAll<Inhabited>()
                .ForEach((Entity entity, int entityInQueryIndex, ref State state) =>
                {
                    ref var provinces = ref state.StateToProv.Value.Lookup[state.Index];
                    var owner = GetComponent<Province>(provinces[0]).Owner;
                    for (var provIndex = 1; provIndex < provinces.Length; provIndex++)
                    {
                        if (GetComponent<Province>(provinces[provIndex]).Owner == owner)
                            continue;
                        
                        EntityManager.AddComponent<PartialOwnership>(entity);
                        return;
                    }

                    state.Owner = owner;
                }).WithoutBurst().Run();
        }

        public void AttachStatesToCountry()
        {
            
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
            using (var emptyInventory = new NativeArray<Inventory>(LoadChain.GoodNum, Allocator.Temp))
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
            // Spawns random 4, 5, or 6 factory in every region with an owned province.
            Entities
                .WithName("Debug_Spawn_Rand_Factories")
                .WithStructuralChanges()
                .WithAll<Inhabited>()
                .ForEach((Entity entity, in State state) =>
                {
                    // Check for state initial inhabited status done in Province Load.
                    ref var provInState = ref state.StateToProv.Value.Lookup[state.Index];

                    var rand = new Random((uint) state.Index + 12);
                    var numFactories = rand.NextInt(3, 10); // 3 to 9

                    for (var cursor = 0; cursor < numFactories; cursor++)
                    {
                        var province = provInState[rand.NextInt(provInState.Length)];
                        var owner = GetComponent<Province>(province).Owner;

                        // Uncolonized provinces should not have factories within them.
                        var loopCounter = 0;
                        while (owner == UncolonizedEntity)
                        {
                            province = provInState[rand.NextInt(provInState.Length)];
                            owner = GetComponent<Province>(province).Owner;
                            
                            if (loopCounter++ > 20)
                                throw new Exception("Uncolonized province search timed out!");
                        }
                        
                        var targetFactory = EntityManager.Instantiate(_debugFactories[rand.NextInt(3)]);
                        EntityManager.SetComponentData(targetFactory,new Location(province, entity));
                        EntityManager.GetBuffer<FactoryWrapper>(entity).Add(targetFactory);
                    }
                }).WithoutBurst().Run();
        }
    }
}