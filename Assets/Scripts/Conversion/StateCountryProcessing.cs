using System;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Conversion
{
    [DisableAutoCreation]
    public class StateCountryProcessing : SystemBase
    {
        public enum ManualMethodCall
        {
            // Used by update to determine which method to call.
            TagOwnedStatesAndAttachToCountry,
            DebugSpawnFactories,
            SetDebugValues,
            DisposeDebugFactoryTemplates
        }

        public static ManualMethodCall CallMethod;

        public static BlobAssetReference<MarketMatrix>[] MarketIdentities;
        public static int[] MaxEmploy;

        private NativeArray<Entity> _debugFactories;

        protected override void OnUpdate()
        {
            switch (CallMethod)
            {
                case ManualMethodCall.TagOwnedStatesAndAttachToCountry:
                    TagOwnedStatesAndAttachToCountry();
                    break;
                case ManualMethodCall.DebugSpawnFactories:
                    DebugSpawnFactories();
                    break;
                case ManualMethodCall.SetDebugValues:
                    SetDebugValues();
                    break;
                case ManualMethodCall.DisposeDebugFactoryTemplates:
                    DisposeDebugFactoryTemplates();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void TagOwnedStatesAndAttachToCountry()
        {
            // Wew, that function name.
            Entities
                .WithName("Tag_States")
                .WithStructuralChanges()
                .WithAll<Inhabited>()
                .ForEach((Entity entity, ref State state) =>
                {
                    ref var provinces = ref state.StateToProv.Value.Lookup[state.Index];
                    var owner = GetComponent<Province>(provinces[0]).Owner;
                    var completeOwnership = true;

                    var owners = new NativeList<Entity>(Allocator.Temp);
                    owners.Add(owner);

                    // Tagging complete or partial ownership
                    for (var provIndex = 1; provIndex < provinces.Length; provIndex++)
                    {
                        var currentOwner = GetComponent<Province>(provinces[provIndex]).Owner;
                        if (currentOwner == owner)
                            continue;

                        if (completeOwnership)
                        {
                            completeOwnership = false;
                            EntityManager.AddComponent<PartialOwnership>(entity);
                        }

                        owners.Add(currentOwner);
                    }

                    if (completeOwnership)
                        state.Owner = owner;

                    // Adding state to country list and adding tag for countries that own at least one province.
                    foreach (var country in owners)
                    {
                        if (HasComponent<OceanCountry>(country) || HasComponent<UncolonizedCountry>(country))
                            continue;

                        // Adding state
                        EntityManager.GetBuffer<StateWrapper>(country).Add(entity);

                        // Adding tag
                        if (!HasComponent<RelevantCountry>(country))
                            EntityManager.AddComponent<RelevantCountry>(country);
                    }
                }).WithoutBurst().Run();
        }

        private void SetDebugValues()
        {
            _debugFactories = new NativeArray<Entity>(new[]
            {
                EntityManager.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory),
                    typeof(Identity), typeof(Location), typeof(FactoryEmployment)),
                EntityManager.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory),
                    typeof(Identity), typeof(Location), typeof(FactoryEmployment)),
                EntityManager.CreateEntity(typeof(Factory), typeof(Wallet), typeof(Inventory),
                    typeof(Identity), typeof(Location), typeof(FactoryEmployment))
            }, Allocator.TempJob);

            for (var index = 0; index < _debugFactories.Length; index++)
            {
                var factory = _debugFactories[index];

                EntityManager.SetComponentData(factory, new Factory
                {
                    MaximumEmployment = MaxEmploy[index + 3],
                    TotalEmployed = 0
                });

                using (var emptyInventory = new NativeArray<Inventory>(LoadChain.GoodNum, Allocator.Temp))
                {
                    EntityManager.GetBuffer<Inventory>(factory).AddRange(emptyInventory);
                }

                EntityManager.SetComponentData<Identity>(factory, MarketIdentities[index + 3]);
                EntityManager.SetComponentData(factory, new Wallet {Wealth = 1000});
            }
        }

        private void DebugSpawnFactories()
        {
            // Spawns random factories in every region with an owned province.

            var rand = new Random(123456789);

            Entities
                .WithName("Debug_Spawn_Rand_Factories")
                .WithStructuralChanges()
                .WithNone<OceanProvince, UncolonizedProvince>()
                .ForEach((Entity entity, in Province province) =>
                {
                    // Check for state initial inhabited status done in Province Load.
                    ref var provToState = ref province.ProvToState.Value.Lookup;
                    var state = provToState[province.Index];

                    var numFactories = rand.NextInt(0, 4); // 0 to 3

                    for (var cursor = 0; cursor < numFactories; cursor++)
                    {
                        var type = rand.NextInt(3);
                        var targetFactory = EntityManager.Instantiate(_debugFactories[type]);
                        EntityManager.SetComponentData(targetFactory, new Location(entity, state));
                        EntityManager.GetBuffer<FactoryWrapper>(entity).Add(new FactoryWrapper(targetFactory));
                        EntityManager.SetName(targetFactory, $"Type: {type}.");
                    }
                }).WithoutBurst().Run();
        }

        private void DisposeDebugFactoryTemplates()
        {
            foreach (var factory in _debugFactories)
                EntityManager.DestroyEntity(factory);

            _debugFactories.Dispose();
        }
    }
}