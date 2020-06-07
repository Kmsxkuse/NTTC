using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public static class PopsLoad
    {
        public static BlobAssetReference<MarketMatrix> Main(BlobAssetReference<ProvToState> provToState)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var pmi = MarketConvert.Main(JsonConvert.DeserializeObject<MarketJson>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Pop.json"))));

            // DEBUG: 6 hardcoded!
            using (var inventory = new NativeArray<Inventory>(LoadChain.GoodNum, Allocator.Temp))
            using (var provinces =
                // Ocean doesn't have cores. Or tagged province.
                em.CreateEntityQuery(typeof(Province), typeof(Cores), ComponentType.Exclude<OceanCountry>(),
                    ComponentType.Exclude<UncolonizedCountry>()).ToEntityArray(Allocator.TempJob))
            {
                foreach (var province in provinces)
                {
                    var rand = Random.Range(15, 26); // 15 to 25 different types of pops.
                    for (var i = 0; i < rand; i++)
                    {
                        var targetPop = em.CreateEntity(typeof(Population), typeof(Ethnicity),
                            typeof(Identity), typeof(Inventory), typeof(Wallet), typeof(Location), typeof(PopEmployment));
                        em.SetComponentData(targetPop, new Population
                        {
                            Quantity = Random.Range(300, 700)
                        });
                        em.SetComponentData(targetPop, new Ethnicity
                        {
                            Culture = i
                        });
                        em.SetComponentData(targetPop, new Identity
                        {
                            MarketIdentity = pmi
                        });
                        em.SetComponentData(targetPop, new Wallet
                        {
                            Wealth = Random.Range(500, 1000)
                        });
                        em.SetComponentData(targetPop, new Location(province,
                            provToState.Value.Lookup[em.GetComponentData<Province>(province).Index]));
                        em.GetBuffer<Inventory>(targetPop).AddRange(inventory);
                    }
                }
            }

            return pmi;
        }
    }
}