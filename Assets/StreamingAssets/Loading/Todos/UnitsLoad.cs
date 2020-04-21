using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public enum UnitTypes
    {
        Land,
        Naval,

        Support,
        Infantry,
        BigShip,
        LightShip,
        Cavalry,
        Transport,
        Special
    }

    public struct UnitCollection : IComponentData
    {
    }

    public struct UnitEntity : IComponentData
    {
        public int Index, BuildCost, SupplyCost;
    }

    public static class UnitsLoad
    {
        public static (Entity, List<string>) Main()
        {
            // Output Arrays
            var unitNames = new List<string>();
            var units = new NativeList<EntityWrapper>(Allocator.Temp);

            var em = World.Active.EntityManager;

            using (var generalList = new NativeList<DataValue>(Allocator.Temp))
            using (var goodsList = new NativeList<DataGood>(Allocator.Temp))
            {
                foreach (var unitPaths in Directory.EnumerateFiles(Path.Combine(Application.streamingAssetsPath,
                    "Units")))
                {
                    // Resetting fileTree and values
                    var fileTree = new List<KeyValuePair<int, object>>();
                    var values = new List<string>();

                    FileUnpacker.ParseFile(unitPaths, fileTree, values, UnitsMagicOverride);

                    foreach (var unit in fileTree)
                    {
                        var currentEntity = units[unit.Key - (int) MagicUnifiedNumbers.Unit];
                        var currentUnit = new UnitEntity {Index = unit.Key - (int) MagicUnifiedNumbers.Unit};

                        foreach (var quality in (List<KeyValuePair<int, object>>) unit.Value)
                            // TODO: Possible conversion to UnifiedVariables?
                            switch ((LoadVariables) quality.Key)
                            {
                                case LoadVariables.UnitType:
                                case LoadVariables.Type:
                                    if (!Enum.TryParse(values[(int) quality.Value].Replace("_", ""), true,
                                        out UnitTypes unitType))
                                        throw new Exception("Unknown unit type: " + values[(int) quality.Value]);
                                    generalList.Add(new DataValue(quality.Key, (int) unitType));
                                    break;
                                case LoadVariables.Capital:
                                    generalList.Add(new DataValue(quality.Key,
                                        LoadMethods.YesNoConverter(values[(int) quality.Value]) ? 1f : 0f));
                                    break;
                                case LoadVariables.Priority:
                                case LoadVariables.MaxStrength:
                                case LoadVariables.DefaultOrganisation:
                                case LoadVariables.WeightedValue:
                                case LoadVariables.MaximumSpeed:
                                case LoadVariables.ColonialPoints:
                                    if (!float.TryParse(values[(int) quality.Value], out var coreFloat))
                                        throw new Exception("Unknown core float: " + values[(int) quality.Value]);
                                    generalList.Add(new DataValue(quality.Key, coreFloat));
                                    break;
                                case LoadVariables.LimitPerPort:
                                case LoadVariables.MinPortLevel:
                                case LoadVariables.BuildTime:
                                    if (!float.TryParse(values[(int) quality.Value], out var buildFloat))
                                        throw new Exception("Unknown build float: " + values[(int) quality.Value]);
                                    generalList.Add(new DataValue(quality.Key, buildFloat));
                                    break;
                                case LoadVariables.CanBuildOverseas:
                                    generalList.Add(new DataValue(quality.Key,
                                        LoadMethods.YesNoConverter(values[(int) quality.Value]) ? 1f : 0f));
                                    break;
                                case LoadVariables.PrimaryCulture:
                                    generalList.Add(new DataValue(quality.Key,
                                        LoadMethods.YesNoConverter(values[(int) quality.Value]) ? 1f : 0f));
                                    generalList.Add(new DataValue(quality.Key,
                                        LoadMethods.YesNoConverter(values[(int) quality.Value]) ? 1f : 0f));
                                    break;
                                case LoadVariables.SupplyConsumptionScore:
                                case LoadVariables.SupplyConsumption:
                                    if (!float.TryParse(values[(int) quality.Value], out var supplyFloat))
                                        throw new Exception("Unknown supply float: " + values[(int) quality.Value]);
                                    generalList.Add(new DataValue(quality.Key, supplyFloat));
                                    break;
                                case LoadVariables.Reconnaissance:
                                case LoadVariables.Attack:
                                case LoadVariables.Defence:
                                case LoadVariables.Discipline:
                                case LoadVariables.Support:
                                case LoadVariables.Maneuver:
                                case LoadVariables.Siege:
                                case LoadVariables.Hull:
                                case LoadVariables.GunPower:
                                case LoadVariables.FireRange:
                                case LoadVariables.Evasion:
                                case LoadVariables.TorpedoAttack:
                                    if (!float.TryParse(values[(int) quality.Value], out var abilityFloat))
                                        throw new Exception("Unknown ability float: " + values[(int) quality.Value]);
                                    generalList.Add(new DataValue(quality.Key, abilityFloat));
                                    break;
                                case LoadVariables.BuildCost:
                                    if (!(quality.Value is List<KeyValuePair<int, object>> buildCostActual))
                                        throw new Exception("Unknown build cost.");

                                    foreach (var goodKvp in buildCostActual)
                                    {
                                        if (!float.TryParse(values[(int) goodKvp.Value], out var buildCostFloat))
                                            throw new Exception(
                                                "Unknown buildCost float: " + values[(int) goodKvp.Value]);
                                        goodsList.Add(new DataGood(goodKvp.Key, buildCostFloat));
                                    }

                                    currentUnit.BuildCost = goodsList.Length;
                                    break;
                                case LoadVariables.SupplyCost:
                                    if (!(quality.Value is List<KeyValuePair<int, object>> supplyCostActual))
                                        throw new Exception("Unknown supply cost.");

                                    foreach (var goodKvp in supplyCostActual)
                                    {
                                        if (!float.TryParse(values[(int) goodKvp.Value], out var supplyCostFloat))
                                            throw new Exception(
                                                "Unknown supplyCost float: " + values[(int) goodKvp.Value]);
                                        goodsList.Add(new DataGood(goodKvp.Key, supplyCostFloat));
                                    }

                                    currentUnit.SupplyCost = goodsList.Length;
                                    break;
                            }

                        em.AddBuffer<DataValue>(currentEntity).AddRange(generalList);
                        generalList.Clear();

                        em.AddBuffer<DataGood>(currentEntity).AddRange(goodsList);
                        goodsList.Clear();

                        em.SetComponentData(currentEntity, currentUnit);
                    }

                    int UnitsMagicOverride(int parent, string str)
                    {
                        if (parent != -1)
                            return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                        units.Add(em.CreateEntity(typeof(UnitEntity)));

                        unitNames.Add(str);
                        return (int) MagicUnifiedNumbers.Unit + unitNames.Count - 1;
                    }
                }
            }

            var unitCollectorEntity = FileUnpacker.GetCollector<UnitCollection>(units);
            units.Dispose();

            return (unitCollectorEntity, unitNames);
        }
    }
}