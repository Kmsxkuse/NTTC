using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct InventionCollection : IComponentData
    {
    }

    public struct InventionEntity : IComponentData
    {
        public int Index;
        public Entity Technology;
    }

    public static class InventionsLoad
    {
        public static Entity Main(Entity technologies, ref StringBox stringBox)
        {
            var inventions = new NativeList<EntityWrapper>(Allocator.Temp);
            var inventionNames = new List<string>();
            var allIncompleteInventions = new List<List<IncompleteInventions>>();

            var em = World.Active.EntityManager;

            using (var technologyList = em.GetBuffer<EntityWrapper>(technologies).ToNativeArray(Allocator.Temp))
            using (var parentLocation = new NativeMultiHashMap<int, int>(1, Allocator.Temp))
            {
                foreach (var inventPath in Directory.EnumerateFiles(
                    Path.Combine(Application.streamingAssetsPath, "Inventions"), "*.txt"))
                {
                    var fileTree = new List<KeyValuePair<int, object>>();
                    var values = new List<string>();

                    FileUnpacker.ParseFile(inventPath, fileTree, values, InventMagicOverride);

                    foreach (var inventionKvP in fileTree)
                    {
                        var targetInvention = new InventionEntity
                        {
                            Index = inventionKvP.Key - (int) MagicUnifiedNumbers.Invention
                        };
                        var inventionEntity = inventions[targetInvention.Index];

                        var inventValues = new List<DataValue>();
                        var inventRanges = new List<DataRange>();
                        var tempInventions = new List<IncompleteInventions>();

                        parentLocation.Add(inventionKvP.Key, inventRanges.Count);
                        inventRanges.Add(new DataRange(inventionKvP.Key, -1, -1));

                        FileUnpacker.ProcessQueue(inventionKvP, inventValues, inventRanges,
                            parentLocation, values, InventSwitchOverride);

                        em.SetComponentData(inventionEntity, targetInvention);

                        allIncompleteInventions.Add(tempInventions);

                        using (var tempRange = new NativeArray<DataRange>(inventRanges.ToArray(), Allocator.Temp))
                        {
                            em.AddBuffer<DataRange>(inventionEntity).AddRange(tempRange);
                        }

                        using (var tempValues = new NativeArray<DataValue>(inventValues.ToArray(), Allocator.Temp))
                        {
                            em.AddBuffer<DataValue>(inventionEntity).AddRange(tempValues);
                        }

                        bool InventSwitchOverride(string targetStr, KeyValuePair<int, object> target)
                        {
                            switch ((MagicUnifiedNumbers) (target.Key / 10000 * 10000))
                            {
                                case MagicUnifiedNumbers.Placeholder:
                                    var targetTech = technologyList[target.Key - (int) MagicUnifiedNumbers.Placeholder];
                                    targetInvention.Technology = targetTech;

                                    em.GetBuffer<EntityWrapper>(targetTech).Add(inventionEntity);
                                    return true;
                            }

                            if (target.Key != (int) LoadVariables.Invention)
                                return false;

                            tempInventions.Add(new IncompleteInventions(targetStr, inventValues.Count));
                            inventValues.Add(new DataValue((int) LoadVariables.Invention, -1));
                            return true;
                        }
                    }
                }
            }

            stringBox.InventionNames = inventionNames;

            var inventionCollector = FileUnpacker.GetCollector<InventionCollection>(inventions);
            inventions.Dispose();

            CompleteInventions(inventionCollector, allIncompleteInventions);

            return inventionCollector;

            int InventMagicOverride(int parent, string raw)
            {
                if (parent == (int) LoadVariables.Limit)
                    // God damn C# trash pointers.
                    if (LookupDictionaries.Techs.TryGetValue(raw, out var techIndex))
                        return (int) MagicUnifiedNumbers.Placeholder + techIndex;

                if (parent != -1)
                    return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                inventions.Add(em.CreateEntity(typeof(InventionEntity)));
                inventionNames.Add(raw);
                return (int) MagicUnifiedNumbers.Invention + inventionNames.Count - 1;
            }
        }

        // Technology and inventions
        public static void CompleteInventions(Entity technologies,
            List<List<IncompleteInventions>> finishInventions)
        {
            var em = World.Active.EntityManager;
            var entityList = em.GetBuffer<EntityWrapper>(technologies).AsNativeArray();

            for (var index = 0; index < finishInventions.Count; index++)
            {
                var currentActions = em.GetBuffer<DataValue>(entityList[index]).AsNativeArray();
                foreach (var inventionReplacement in finishInventions[index])
                {
                    if (!LookupDictionaries.Inventions.TryGetValue(inventionReplacement.Raw, out var inventIndex))
                        throw new Exception("Unknown technology invention. " + inventionReplacement.Raw);

                    currentActions[inventionReplacement.Index] =
                        new DataValue((int) LoadVariables.Invention, inventIndex);
                }
            }
        }
    }

    // Used in technology and invention load
    public struct IncompleteInventions
    {
        public readonly string Raw;
        public readonly int Index;

        public IncompleteInventions(string raw, int index)
        {
            Raw = raw;
            Index = index;
        }
    }
}