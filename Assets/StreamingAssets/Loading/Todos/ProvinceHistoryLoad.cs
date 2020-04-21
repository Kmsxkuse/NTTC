using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct ProvinceCollection : IComponentData
    {
    }

    public struct ProvinceEntity : IComponentData
    {
        public int Owner,
            Controller,
            LifeRating,
            Production,
            Colonial,
            Terrain;
    }

    public static class ProvinceHistoryLoad
    {
        public static Entity Main(NativeHashMap<int, int> idIndex)
        {
            // Assuming start date of only 1836.1.1

            var provinces = new NativeArray<EntityWrapper>(idIndex.Length, Allocator.Temp);

            var em = World.Active.EntityManager;

            using (var cores = new NativeList<DataInt>(Allocator.Temp))
            {
                foreach (var continent in Directory.GetDirectories(Path.Combine(Application.streamingAssetsPath,
                    "History", "provinces")))
                foreach (var filePath in Directory.GetFiles(continent, "*.txt"))
                {
                    var fileTree = new List<KeyValuePair<int, object>>();
                    var values = new List<string>();

                    FileUnpacker.ParseFile(filePath, fileTree, values, ProvinceMagicOverride);

                    var idString = Regex.Match(Path.GetFileNameWithoutExtension(filePath), @"\d+").Value;
                    if (!int.TryParse(idString, out var provId))
                        throw new Exception("Invalid province file name. Must include province ID. " + filePath);

                    var provIndex = idIndex[provId];
                    var currentProvince = new ProvinceEntity {Owner = -1, Controller = -1, Terrain = -1, Colonial = -1};

                    foreach (var property in fileTree)
                    {
                        if (!(property.Value is int))
                            continue;

                        var targetStr = values[(int) property.Value];
                        switch ((LoadVariables) property.Key)
                        {
                            case LoadVariables.Owner: // initial color
                                if (!LookupDictionaries.CountryTags.TryGetValue(targetStr, out var ownerIndex))
                                    throw new Exception("Unknown owner. " + targetStr);

                                currentProvince.Owner = ownerIndex;
                                break;
                            case LoadVariables.Controller: // siege ownership
                                if (!LookupDictionaries.CountryTags.TryGetValue(targetStr, out var controllerIndex))
                                    throw new Exception("Unknown controller. " + targetStr);

                                currentProvince.Controller = controllerIndex;
                                break;
                            case LoadVariables.AddCore: // cores on province
                                if (!LookupDictionaries.CountryTags.TryGetValue(targetStr, out var coreIndex))
                                    throw new Exception("Unknown core. " + targetStr);

                                cores.Add(coreIndex);
                                break;
                            case LoadVariables.TradeGoods: // good produced
                                if (!LookupDictionaries.Goods.TryGetValue(targetStr, out var goodIndex))
                                    throw new Exception("Unknown good. " + targetStr);

                                currentProvince.Production = goodIndex;
                                break;
                            case LoadVariables.LifeRating:
                                if (!int.TryParse(targetStr, out var lifeRating))
                                    throw new Exception("Unknown life rating. " + targetStr);

                                currentProvince.LifeRating = lifeRating;
                                break;
                            case LoadVariables.Terrain:
                                if (!LookupDictionaries.Terrain.TryGetValue(targetStr, out var terrainIndex))
                                    throw new Exception("Unknown terrain. " + targetStr);

                                currentProvince.Terrain = terrainIndex;
                                break;
                            case LoadVariables.Colonial:
                                if (!int.TryParse(targetStr, out var colonialLevel))
                                    throw new Exception("Unknown colonialLevel. " + targetStr);

                                currentProvince.Colonial = colonialLevel;
                                break;
                        }
                    }

                    if (currentProvince.Owner == -1)
                    {
                        if (currentProvince.LifeRating < 1)
                        {
                            currentProvince.Owner = LookupDictionaries.CountryTags["ocn"];
                        }
                        else
                        {
                            currentProvince.Owner = LookupDictionaries.CountryTags["ucn"];
                            if (currentProvince.Colonial < 1)
                                currentProvince.Colonial = 0;
                        }
                    }

                    var targetProvince = em.CreateEntity(typeof(ProvinceEntity));
                    em.SetComponentData(targetProvince, currentProvince);
                    em.AddBuffer<DataInt>(targetProvince).AddRange(cores);
                    em.AddBuffer<ProvPopInfo>(targetProvince); // Used for populations.
                    cores.Clear();

                    provinces[provIndex] = targetProvince;

                    int ProvinceMagicOverride(int parent, string target)
                    {
                        // Skipping other start dates
                        return Regex.IsMatch(target, @"\d+")
                            ? (int) MagicUnifiedNumbers.Placeholder
                            : (int) MagicUnifiedNumbers.ContinueMagicNumbers;
                    }
                }
            }

            var provinceCollection = FileUnpacker.GetCollector<ProvinceCollection>(provinces);
            provinces.Dispose();

            return provinceCollection;
        }
    }
}