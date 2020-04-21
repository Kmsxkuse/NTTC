using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public struct PopTypeCollection : IComponentData
    {
    }

    public struct PopTypeEntity : IComponentData
    {
        public enum Standing
        {
            Rich,
            Middle,
            Poor
        }

        public Color32 Color;
        public int Index;
        public Standing Strata;
        public bool StateCapitalOnly;

        public int2 lifeRange,
            everydayRange,
            luxuryRange,
            countryMigrationRange,
            provinceMigrationRange,
            promoteToRange,
            ideologiesRange;
    }

    public static class PopTypesLoad
    {
        public static (Entity, List<string>) Main()
        {
            var popFiles = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "PopTypes"), "*.txt");

            // Output arrays
            var popTypes = new NativeList<EntityWrapper>(Allocator.Temp);
            var popNames = new List<string>();
            popNames.AddRange(popFiles.Select(Path.GetFileNameWithoutExtension));

            var em = World.Active.EntityManager;

            using (var needsList = new NativeList<DataGood>(Allocator.Temp))
            using (var popRebels = new NativeList<DataValue>(Allocator.Temp))
            {
                for (var index = 0; index < popTypes.Length; index++)
                {
                    // Generating file tree
                    var fileTree = new List<KeyValuePair<int, object>>();
                    var values = new List<string>();

                    FileUnpacker.ParseFile(popFiles[index], fileTree, values,
                        (i, s) => (int) MagicUnifiedNumbers.ContinueMagicNumbers);

                    var currentEntity = em.CreateEntity(typeof(PopTypeEntity));
                    var currentPopType = new PopTypeEntity {Index = index};

                    foreach (var topLevel in fileTree)
                        switch ((LoadVariables) topLevel.Key)
                        {
                            case LoadVariables.Sprite:
                                // Skipping sprite
                                break;
                            case LoadVariables.Color:
                                currentPopType.Color = LoadMethods.ParseColor32(values[(int) topLevel.Value]);
                                break;
                            case LoadVariables.Strata:
                                if (!Enum.TryParse(values[(int) topLevel.Value], true,
                                    out PopTypeEntity.Standing strata))
                                    throw new Exception("Strata unknown: " + values[(int) topLevel.Value]);
                                currentPopType.Strata = strata;
                                break;
                            case LoadVariables.StateCapitalOnly:
                                currentPopType.StateCapitalOnly =
                                    LoadMethods.YesNoConverter(values[(int) topLevel.Value]);
                                break;
                            case LoadVariables.Rebel:
                                RebelParser(topLevel);
                                em.AddBuffer<DataValue>(currentEntity).AddRange(popRebels);
                                popRebels.Clear();
                                break;
                            case LoadVariables.CountryMigrationTarget:
                            case LoadVariables.MigrationTarget:
                            case LoadVariables.PromoteTo:
                            case LoadVariables.Ideologies:
                                var collapsedList = new List<float2>();
                                RestParser(topLevel, collapsedList);
                                break;
                            case LoadVariables.LifeNeeds:
                            case LoadVariables.EverydayNeeds:
                            case LoadVariables.LuxuryNeeds:
                                NeedsParser(topLevel);
                                break;
                        }

                    em.AddBuffer<DataGood>(currentEntity).AddRange(needsList);
                    em.SetComponentData(currentEntity, currentPopType);
                    popTypes.Add(currentEntity);
                    needsList.Clear();

                    // DEBUG
                    //break;

                    void RestParser(KeyValuePair<int, object> currentBranch, List<float2> targetList)
                    {
                    }

                    void NeedsParser(KeyValuePair<int, object> currentBranch)
                    {
                        // There should be no nested values within needs.
                        var startingNeeds = needsList.Length;

                        foreach (var goodsKvp in (List<KeyValuePair<int, object>>) currentBranch.Value)
                        {
                            if (!float.TryParse(values[(int) goodsKvp.Value], out var goodsNeeded))
                                throw new Exception("Unknown goods needed: " + values[(int) goodsKvp.Value]);

                            needsList.Add(new DataGood(goodsKvp.Key - (int) MagicUnifiedNumbers.Goods, goodsNeeded));
                        }

                        switch ((LoadVariables) currentBranch.Key)
                        {
                            case LoadVariables.LifeNeeds:
                                currentPopType.lifeRange = new int2(startingNeeds, needsList.Length);
                                break;
                            case LoadVariables.EverydayNeeds:
                                currentPopType.everydayRange = new int2(startingNeeds, needsList.Length);
                                break;
                            case LoadVariables.LuxuryNeeds:
                                currentPopType.luxuryRange = new int2(startingNeeds, needsList.Length);
                                break;
                        }
                    }

                    void RebelParser(KeyValuePair<int, object> currentBranch)
                    {
                        foreach (var armyKvp in (List<KeyValuePair<int, object>>) currentBranch.Value)
                        {
                            if (!float.TryParse(values[(int) armyKvp.Value], out var armyRatio))
                                throw new Exception("Unknown army ratio: " + values[(int) armyKvp.Value]);

                            if (armyRatio < 0.001)
                                continue;

                            popRebels.Add(new DataValue(armyKvp.Key - (int) MagicUnifiedNumbers.Unit, armyRatio));
                        }
                    }
                }
            }

            var popTypeCollectorEntity = FileUnpacker.GetCollector<PopTypeCollection>(popTypes);
            popTypes.Dispose();

            return (popTypeCollectorEntity, popNames);
        }
    }
}