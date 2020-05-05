using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Conversion
{
    public static class ProvinceLoad
    {
        public static void Main(IReadOnlyDictionary<int, Entity> provEntityLookup, IReadOnlyDictionary<string, Entity> tagLookup,
            BlobAssetReference<MarketMatrix>[] marketIdentities, int[] maxEmploy, BlobAssetReference<ProvToState> provToState)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var provinces = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "history", "provinces"),
                "*.txt", SearchOption.AllDirectories);
            using (var cores = new NativeList<Cores>(Allocator.Temp))
            {
                foreach (var province in provinces)
                {
                    cores.Clear();

                    var fileTree = new List<(string, object)>();
                    ParseFile.Main(province, fileTree);

                    var idString = Regex.Match(Path.GetFileNameWithoutExtension(province)
                                               ?? throw new Exception("Invalid province file path, null path."), @"\d+").Value;
                    if (!int.TryParse(idString, out var provId))
                        throw new Exception("Invalid province file name. Must include province ID. " + province);

                    var provEntity = provEntityLookup[provId];
                    var target = em.GetComponentData<Province>(provEntity);
                    ref var state = ref provToState.Value.Lookup[target.Index];

                    foreach (var (key, value) in fileTree)
                        switch (key)
                        {
                            case "owner":
                                target.Owner = tagLookup[(string) value];
                                if (!em.HasComponent<Inhabited>(state))
                                    em.AddComponent(state, typeof(Inhabited));
                                continue;
                            case "controller":
                                target.Controller = tagLookup[(string) value];
                                continue;
                            case "add_core":
                                cores.Add(tagLookup[(string) value]);
                                continue;
                            case "trade_goods":
                                var provFactory = em.CreateEntity(typeof(Factory), typeof(Wallet),
                                    typeof(Inventory), typeof(Identity), typeof(RgoGood));

                                var rand = Random.Range(0f, 10f);
                                // 1 (50%), 2 (30%), or 3(20%).
                                var tradeGood = (int) math.ceil(math.pow(rand, 3) /
                                    600f - math.pow(rand, 2) / 200f + 11 * rand / 60);
                                em.SetComponentData(provFactory, new Factory
                                {
                                    MaximumEmployment = maxEmploy[tradeGood - 1] // -1 = infinite employment.
                                });
                                em.SetComponentData<Identity>(provFactory, marketIdentities[tradeGood - 1]);

                                target.TradeGood = provFactory;
                                continue;
                            case "life_rating":
                                target.LifeRating = int.Parse((string) value);
                                continue;
                        }

                    if (target.Owner == tagLookup["OCEAN"])
                        target.Owner = tagLookup["UNCOLONIZED"];

                    em.SetComponentData(provEntity, target);
                    em.AddBuffer<Cores>(provEntity).AddRange(cores);
                }
            }
        }
    }
}