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
        public static void Main(IReadOnlyDictionary<int, Entity> provEntityLookup, IReadOnlyDictionary<string, Entity> tagLookup)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var provinces = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "history", "provinces"),
                "*.txt", SearchOption.AllDirectories);
            using (var cores = new NativeList<Cores>(Allocator.Temp))
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

                    foreach (var (key, value) in fileTree)
                    {
                        switch (key)
                        {
                            case "owner":
                                target.Owner = tagLookup[(string) value];
                                continue;
                            case "controller":
                                target.Controller = tagLookup[(string) value];
                                continue;
                            case "add_core":
                                cores.Add(tagLookup[(string) value]);
                                continue;
                            case "trade_goods":
                                target.TradeGoods = Random.Range(1, 4); // 1, 2, or 3.
                                continue;
                            case "life_rating":
                                target.LifeRating = int.Parse((string) value);
                                continue;
                        }
                    }

                    if (target.Owner == tagLookup["OCEAN"])
                        target.Owner = tagLookup["UNCOLONIZED"];

                    em.SetComponentData(provEntity, target);
                    em.AddBuffer<Cores>(provEntity).AddRange(cores);
                }
        }
    }
}
