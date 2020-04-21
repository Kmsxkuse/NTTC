using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    [Serializable]
    public struct TerrainEntity : IComponentData, IDataEntity, IDataName
    {
        public Color32 Color;
        public NativeString64 Name;

        public float MovementCost;
        //public int Index;
        // Rest are located in dynamic buffers.

        public LoadVariables[] DefaultTypes()
        {
            return new[] {LoadVariables.Color, LoadVariables.MovementCost};
        }

        public LoadVariables[] RangeTypes()
        {
            // Terrain Entity should not have any Ranges
            return new LoadVariables[0];
        }

        public void AssignDefaults(IReadOnlyList<string> construction)
        {
            Color = LoadMethods.ParseColor32(construction[0]);
            MovementCost = float.Parse(construction[1]);
            //Index = (int) construction[2];
        }

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

    public static class TerrainLoad
    {
        private static TerrainOutput ParseParadoxFile()
        {
            var fileTree = new List<(int Key, object Value)>();

            var terrainNames = new List<string>();
            var terrains = new List<TerrainEntity>();
            var terrainCache = new List<List<FirstLevelCore>>();
            var paletteLookups = new List<(Color Palette, int Index)>();

            // Generating file tree representation.
            FileUnpacker.ParseFile(Path.Combine(Application.streamingAssetsPath, "map", "terrain.txt"),
                fileTree, TerrainMagicOverride);

            for (var index = 0; index < fileTree.Count; index++)
            {
                var currentTerrain = terrains[index];

                var (breakCores, _) = FileUnpacker.AssignFirstLevelDistributeWorkload(ref currentTerrain, fileTree[index],
                    TerrainOverride);

                terrainCache.Add(breakCores);

                bool TerrainOverride((int Key, object Value) target)
                {
                    switch ((LoadVariables) target.Key)
                    {
                        case LoadVariables.Palette:
                            if (!ColorUtility.TryParseHtmlString("#" + (string) target.Value, out var paletteColor))
                                throw new Exception("Unknown hex color. " + (string) target.Value);

                            paletteLookups.Add((paletteColor, index));
                            return true;
                        default:
                            return false;
                    }
                }
            }

            int TerrainMagicOverride(int parent, string str)
            {
                if (parent != -1)
                    return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                terrainNames.Add(str);
                
                terrains.Add(new TerrainEntity {Name = str});
                return (int) MagicUnifiedNumbers.Terrain + terrainNames.Count - 1;
            }

            var output = new TerrainOutput(terrainNames, terrains, terrainCache, paletteLookups);

            //File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "JsonData", "terrain.txt"), 
            //JsonUtility.ToJson(output, true));

            File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "JsonData", "terrain.txt"),
                LoadMethods.Zip(JsonUtility.ToJson(output)));

            return output;
        }

        public static (List<string>, List<(Color Palette, int Index)>) Main(bool cache)
        {
            var (terrainNames, terrainEntities, terrainData,
                paletteLookups) = cache ? JsonUtility.FromJson<TerrainOutput>(
                    LoadMethods.Unzip(File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "JsonData", "terrain.txt"))))
                : ParseParadoxFile();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            for (var index = 0; index < terrainEntities.Count; index++)
            {
                var terrain = em.CreateEntity(typeof(TerrainEntity));
                var data = terrainEntities[index];
                data.SetName(terrainNames[index]);
                em.SetComponentData(terrain, data);

                using (var currentCoreActions = new NativeArray<FirstLevelCore>(
                    ((List<FirstLevelCore>) terrainData[index]).ToArray(), Allocator.Temp))
                {
                    em.AddBuffer<FirstLevelCore>(terrain).AddRange(currentCoreActions);
                }
            }

            // Creating collection entity.
            FileUnpacker.GetCollector<TerrainEntity>();

            /* DEBUG
            var test = em.GetBuffer<Terrains>(terrainCollectorEntity);
            var test1 = test.AsNativeArray();
            for (var index = 0; index < test1.Length; index++)
            {
                var terrainType = test1[index];
                var color = em.GetComponentData<TerrainEntity>(terrainType.TerrainEntity).Color;
                Debug.Log(terrainNames[index] + " color: " + color);
                var coreActions = em.GetBuffer<TerrainValues>(terrainType.TerrainEntity).AsNativeArray();
                foreach (var action in coreActions)
                    Debug.Log(action);
            }
            */
            return (terrainNames,
                paletteLookups.ConvertAll(input => ((Color, int)) input));
        }

        [Serializable]
        private struct TerrainOutput
        {
            public List<string> TerrainNames;
            public List<TerrainEntity> TerrainEntities;
            public List<JsonListWrapper<FirstLevelCore>> TerrainData;
            public List<JsonTupleWrapper<Color, int>> PaletteLookups;

            public TerrainOutput(List<string> terrainNames, List<TerrainEntity> terrainEntities,
                IEnumerable<List<FirstLevelCore>> terrainData, List<(Color, int)> paletteLookups)
            {
                TerrainNames = terrainNames;
                TerrainEntities = terrainEntities;
                TerrainData = new List<JsonListWrapper<FirstLevelCore>>();
                foreach (var terrain in terrainData)
                    TerrainData.Add(terrain);

                PaletteLookups = paletteLookups.ConvertAll(input => (JsonTupleWrapper<Color, int>) input);
            }
            
            public void Deconstruct(out List<string> terrainNames, out List<TerrainEntity> terrainEntities,
                out List<JsonListWrapper<FirstLevelCore>> terrainData, out List<JsonTupleWrapper<Color, int>> paletteLookups)
            {
                terrainNames = TerrainNames;
                terrainEntities = TerrainEntities;
                terrainData = TerrainData;
                paletteLookups = PaletteLookups;
            }
        }
    }
}