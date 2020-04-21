using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct CrimeCollector : IComponentData
    {
    }

    public struct CrimeEntity : IComponentData
    {
        public int Index;
    }

    public static class CrimeLoad
    {
        public static (Entity, List<string>) Main()
        {
            var fileTree = new List<KeyValuePair<int, object>>();
            var values = new List<string>();

            var crimes = new NativeList<EntityWrapper>(Allocator.Temp);
            var crimeNames = new List<string>();

            var em = World.Active.EntityManager;

            FileUnpacker.ParseFile(Path.Combine(Application.streamingAssetsPath, "Common", "crime.txt"), fileTree,
                values, CrimeMagicOverride);

            using (var parentLocation = new NativeMultiHashMap<int, int>(1, Allocator.Temp))
            {
                foreach (var crimeKvp in fileTree)
                {
                    var crimeRanges = new List<DataRange>();
                    var crimeActions = new List<DataValue>();

                    parentLocation.Add(crimeKvp.Key, crimeRanges.Count);
                    crimeRanges.Add(new DataRange(crimeKvp.Key, -1, -1));

                    FileUnpacker.ProcessQueue(crimeKvp, crimeActions, crimeRanges, parentLocation, values,
                        (s, pair) => false);

                    var currentCrime = crimes[crimeKvp.Key - (int) MagicUnifiedNumbers.Placeholder];

                    using (var tempRange = new NativeArray<DataRange>(crimeRanges.ToArray(), Allocator.TempJob))
                    {
                        em.AddBuffer<DataRange>(currentCrime).AddRange(tempRange);
                    }

                    using (var tempValues = new NativeArray<DataValue>(crimeActions.ToArray(), Allocator.TempJob))
                    {
                        em.AddBuffer<DataValue>(currentCrime).AddRange(tempValues);
                    }
                }
            }

            var crimeCollector = FileUnpacker.GetCollector<CrimeCollector>(crimes);
            crimes.Dispose();

            return (crimeCollector, crimeNames);

            int CrimeMagicOverride(int parent, string str)
            {
                if (parent != -1)
                    return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                crimes.Add(em.CreateEntity(typeof(CrimeEntity)));
                crimeNames.Add(str);
                return (int) MagicUnifiedNumbers.Placeholder + crimeNames.Count - 1;
            }
        }
    }
}