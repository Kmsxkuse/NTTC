using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct ContinentCollection : IComponentData
    {
    }

    [Serializable]
    public struct ContinentEntity : IComponentData, IDataName
    {
        public float AssimilationRate,
            FarmRgoSize,
            MineRgoSize;

        public NativeString64 Name; // Max ~30 Characters long. Afro-Eurasia = 12 Characters long. Please, no longer.

        public void SetName(string name)
        {
            Name = name;
        }

        public bool GroupType()
        {
            return false;
        }

        public void SetGroup(string group)
        {
            throw new NotImplementedException();
        }
    }

    public static class ContinentLoad
    {
        public static (List<(int, int)>, List<string>) Main(bool cache, NativeHashMap<int, int> idIndex)
        {
            var provinces = new List<(int, int)>();
            var continentNames = new List<string>();
            var continentEntities = new List<ContinentEntity>();

            if (cache)
            {
                (provinces, continentEntities, continentNames) = JsonUtility.FromJson<ContinentOutput>(
                    LoadMethods.Unzip(File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "JsonData", "continent.txt"))));

                LoadMethods.GenerateCacheEntities<ContinentEntity, ContinentCollection>(continentEntities, continentNames);

                return (provinces, continentNames);
            }

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            var continents = new NativeList<EntityWrapper>(Allocator.Temp);

            var outerToggle = false;
            var provToggle = false;
            var currentCont = new ContinentEntity();

            foreach (var rawLine in File.ReadLines(Path.Combine(Application.streamingAssetsPath, "map", "continent.txt")))
            {
                if (LoadMethods.CommentDetector(rawLine, out var line))
                    continue;

                var equalSplit = Regex.Match(line, @"^.*?(?=\=)");

                if (line.Contains("{"))
                {
                    var newLine = line.Substring(equalSplit.Length + 1).Replace("{", "").Trim();
                    if (!outerToggle)
                    {
                        outerToggle = true;
                        var name = equalSplit.Value.Trim();
                        currentCont = new ContinentEntity {Name = name}; // {Index = continentNames.Count};
                        continentNames.Add(name);

                        if (newLine == string.Empty)
                            continue;

                        equalSplit = Regex.Match(newLine, @"^.*?(?=\=)");
                    }

                    if (!equalSplit.Value.Contains("provinces"))
                        throw new Exception("Unknown nested value: " + equalSplit);

                    provToggle = true;

                    if (newLine == string.Empty)
                        continue;

                    line = newLine;
                }

                if (provToggle)
                {
                    var numbers = Regex.Match(line, @"[\d\s]+");
                    if (numbers.Success)
                    {
                        var individualProv = numbers.Value.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var provId in individualProv)
                        {
                            if (!int.TryParse(provId, out var num))
                                continue;

                            provinces.Add((idIndex[num], continents.Length));
                        }
                    }
                }
                else
                {
                    var newLine = line.Substring(equalSplit.Length + 1).Replace("}", "").Trim();
                    switch (equalSplit.Value.Trim())
                    {
                        case "assimilation_rate":
                            if (!float.TryParse(newLine, out var assimilationRate))
                                throw new Exception("Unknown assimilation rate: " + newLine);
                            currentCont.AssimilationRate = assimilationRate;
                            break;
                        case "farm_rgo_size":
                            if (!float.TryParse(newLine, out var farmSize))
                                throw new Exception("Unknown farm RGO size: " + newLine);
                            currentCont.FarmRgoSize = farmSize;
                            break;
                        case "mine_rgo_size":
                            if (!float.TryParse(newLine, out var mineSize))
                                throw new Exception("Unknown mine RGO size: " + newLine);
                            currentCont.MineRgoSize = mineSize;
                            break;
                    }
                }

                if (!line.Contains("}"))
                    continue;

                if (provToggle)
                {
                    provToggle = false;
                }
                else
                {
                    outerToggle = false;
                    var target = em.CreateEntity(typeof(ContinentEntity));
                    em.SetComponentData(target, currentCont);
                    continents.Add(target);
                    continentEntities.Add(currentCont);
                }
            }

            FileUnpacker.GetCollector<ContinentCollection>();
            continents.Dispose();

            //File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "JsonData", "continent.txt"), 
            //JsonUtility.ToJson(new ContinentOutput(continentNames, provinces, continentEntities), true));

            File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "JsonData", "continent.txt"),
                LoadMethods.Zip(JsonUtility.ToJson(new ContinentOutput(continentNames, provinces, continentEntities))));

            return (provinces, continentNames);
        }

        [Serializable]
        public struct ContinentOutput
        {
            public List<string> ContinentNames;
            public List<JsonTupleWrapper<int, int>> Provinces;
            public List<ContinentEntity> ContinentEntities;

            public ContinentOutput(List<string> continentNames, List<(int, int)> provinces, List<ContinentEntity> continentEntities)
            {
                Provinces = provinces.ConvertAll(input => (JsonTupleWrapper<int, int>) input);
                ContinentEntities = continentEntities;
                ContinentNames = continentNames;
            }

            public void Deconstruct(out List<(int, int)> provinces, out List<ContinentEntity> continentEntities,
                out List<string> continentNames)
            {
                provinces = Provinces.ConvertAll(input => ((int, int)) input);
                continentEntities = ContinentEntities;
                continentNames = ContinentNames;
            }
        }
    }
}