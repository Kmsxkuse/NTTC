using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public static class LoadGeneric
    {
        // Used by event modifiers and national values.
        public static (Entity, List<string>) Main<TEntity, TCollection>(IEnumerable<string> paths,
            Action<Entity, int> setEntityIndex)
        {
            var fileTree = new List<KeyValuePair<int, object>>();
            var values = new List<string>();

            var names = new List<string>();
            var entities = new NativeList<EntityWrapper>(Allocator.Temp);

            var em = World.Active.EntityManager;

            foreach (var path in paths)
                FileUnpacker.ParseFile(path, fileTree, values, NationalValueMagicOverride);

            using (var parentLocation = new NativeMultiHashMap<int, int>(1, Allocator.Temp))
            {
                foreach (var nodeKvP in fileTree)
                {
                    var ranges = new List<DataRange>();
                    var actions = new List<DataValue>();

                    parentLocation.Add(nodeKvP.Key, ranges.Count);
                    ranges.Add(new DataRange(nodeKvP.Key, -1, -1));

                    FileUnpacker.ProcessQueue(nodeKvP, actions, ranges,
                        parentLocation, values, (s, pair) => false);

                    if (ranges.Count > 2) // 1 is throwaway. 2 is BreakCore. There can not be a third.
                        throw new Exception("Invalid nested value in Event Modifiers or National Values!"
                                            + (LoadVariables) ranges[2].Type);

                    var currentValue = em.CreateEntity(typeof(TEntity));
                    setEntityIndex(currentValue, nodeKvP.Key - (int) MagicUnifiedNumbers.Placeholder);

                    using (var tempValue = new NativeArray<DataValue>(actions.ToArray(), Allocator.Temp))
                    {
                        em.AddBuffer<DataValue>(currentValue).AddRange(tempValue);
                    }

                    entities.Add(currentValue);
                }
            }

            var collector = FileUnpacker.GetCollector<TCollection>(entities);
            entities.Dispose();

            return (collector, names);

            int NationalValueMagicOverride(int parent, string str)
            {
                if (parent != -1)
                    return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                names.Add(str);
                return (int) MagicUnifiedNumbers.Placeholder + names.Count - 1;
            }
        }
    }
}