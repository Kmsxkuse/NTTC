using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct BuildingCollection : IComponentData
    {
    }

    public struct BuildingEntity : IComponentData
    {
        public int Index;
    }

    public static class BuildingsLoad
    {
        public static (Entity, List<string>) Main()
        {
            var fileTree = new List<KeyValuePair<int, object>>();
            var values = new List<string>();

            var buildings = new NativeList<EntityWrapper>(Allocator.Temp);
            var buildingNames = new List<string>();

            var em = World.Active.EntityManager;

            FileUnpacker.ParseFile(Path.Combine(Application.streamingAssetsPath, "Common", "buildings.txt"), fileTree,
                values, BuildingMagicOverride);

            foreach (var buildingKvp in fileTree)
            {
                var currentEntity = buildings[buildingKvp.Key - (int) MagicUnifiedNumbers.Building].Entity;
                // ReSharper disable once ConvertToUsingDeclaration
                using (var buildingActions = new NativeList<DataValue>(Allocator.Temp))
                {
                    foreach (var attribute in (List<KeyValuePair<int, object>>) buildingKvp.Value)
                    {
                        var targetStr = attribute.Key < (int) LoadVariables.BreakCore
                            ? values[(int) attribute.Value]
                            : "";
                        switch ((LoadVariables) attribute.Key)
                        {
                            case LoadVariables.MaxLevel:
                            case LoadVariables.Time:
                            case LoadVariables.FortLevel:
                            case LoadVariables.ColonialRange:
                            case LoadVariables.NavalCapacity:
                            case LoadVariables.LocalShipBuild:
                            case LoadVariables.Cost:
                            case LoadVariables.Infrastructure:
                            case LoadVariables.MovementCost:
                            case LoadVariables.ColonialMultiplier:
                            case LoadVariables.ColonialBase:
                                if (!float.TryParse(targetStr, out var floatValue))
                                    throw new Exception("Unknown float: " + targetStr);

                                buildingActions.Add(new DataValue(attribute.Key, floatValue));
                                break;
                            case LoadVariables.DefaultEnabled:
                            case LoadVariables.Province:
                            case LoadVariables.PopBuildFactory:
                            case LoadVariables.OnePerState:
                                buildingActions.Add(
                                    new DataValue(attribute.Key, LoadMethods.YesNoConverter(targetStr) ? 1 : 0));
                                break;
                            case LoadVariables.GoodsCost:
                                using (var buildingGoods = new NativeList<DataGood>(Allocator.Temp))
                                {
                                    foreach (var goodKvp in (List<KeyValuePair<int, object>>) attribute.Value)
                                    {
                                        if (goodKvp.Key / 10000 != (int) MagicUnifiedNumbers.Goods / 10000)
                                            throw new Exception("Unknown good located in Goods Cost "
                                                                + buildingNames[
                                                                    buildingKvp.Key -
                                                                    (int) MagicUnifiedNumbers.Building]);

                                        if (!float.TryParse(values[(int) goodKvp.Value], out var goodValue))
                                            throw new Exception("Unknown goods float: " + values[(int) goodKvp.Value]);

                                        buildingGoods.Add(
                                            new DataGood(goodKvp.Key - (int) MagicUnifiedNumbers.Goods, goodValue));
                                    }

                                    em.AddBuffer<DataGood>(currentEntity).AddRange(buildingGoods);
                                }

                                break;
                        }
                    }

                    em.AddBuffer<DataValue>(currentEntity).AddRange(buildingActions);
                }
            }

            var buildingCollectorEntity = FileUnpacker.GetCollector<BuildingCollection>(buildings);
            buildings.Dispose();

            return (buildingCollectorEntity, buildingNames);

            int BuildingMagicOverride(int parent, string str)
            {
                if (parent != -1)
                    return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                var targetBuilding = em.CreateEntity(typeof(BuildingEntity));
                em.SetComponentData(targetBuilding, new BuildingEntity {Index = buildingNames.Count});
                buildings.Add(targetBuilding);

                buildingNames.Add(str);
                return (int) MagicUnifiedNumbers.Building + buildingNames.Count - 1;
            }
        }
    }
}