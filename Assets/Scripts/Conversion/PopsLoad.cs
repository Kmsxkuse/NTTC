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
        public static BlobAssetReference<MarketMatrix> Main(Dictionary<string, Entity> tagLookup)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var pmi = MarketConvert.Main(JsonConvert.DeserializeObject<MarketJson>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Pop.json"))));

            // DEBUG: 6 hardcoded!
            using (var inventory = new NativeArray<Inventory>(6, Allocator.Temp))
            using (var provinces =
                em.CreateEntityQuery(typeof(Province), typeof(Cores)).ToEntityArray(Allocator.TempJob))
            {
                var uncolonized = tagLookup["UNCOLONIZED"];
                foreach (var province in provinces)
                {
                    // Ocean doesn't have cores. Or tagged province.
                    if (em.GetComponentData<Province>(province).Owner == uncolonized)
                        continue;

                    var rand = Random.Range(3, 8); // 3 to 7 different types of pops.
                    var randomPop = new NativeArray<PopWrapper>(rand, Allocator.Temp);
                    for (var i = 0; i < rand; i++)
                    {
                        var targetPop = em.CreateEntity(typeof(Population), typeof(Ethnicity),
                            typeof(Identity), typeof(Inventory), typeof(Wallet));
                        em.SetComponentData(targetPop, new Population
                        {
                            Employment = 0,
                            Quantity = Random.Range(300, 1500),
                            Satisfaction = 0
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
                        em.GetBuffer<Inventory>(targetPop).AddRange(inventory);

                        randomPop[i] = targetPop;
                    }

                    em.AddBuffer<PopWrapper>(province).AddRange(randomPop);
                    randomPop.Dispose();
                }
            }

            return pmi;
        }
    }
}