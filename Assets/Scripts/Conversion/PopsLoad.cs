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
            using var provinces =
                // Ocean doesn't have cores. Or tagged province.
                em.CreateEntityQuery(typeof(Province), typeof(Cores), ComponentType.Exclude<OceanCountry>(),
                    ComponentType.Exclude<UncolonizedCountry>()).ToEntityArray(Allocator.TempJob);
            foreach (var province in provinces)
            {
                var rand = Random.Range(5, 11); // 5 to 10 different types of pops.
                for (var i = 0; i < rand; i++)
                    em.GetBuffer<Population>(province).Add(new Population
                    {
                        // Maximum factory employment is 1000.
                        Quantity = Random.Range(400, 800),
                        Employed = 0,
                        Wealth = Random.Range(100, 200)
                    });
            }

            return pmi;
        }
    }
}