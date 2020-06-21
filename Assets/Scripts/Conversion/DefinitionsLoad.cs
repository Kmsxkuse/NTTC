using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public static class DefinitionsLoad
    {
        public static (IEnumerable<string>, Dictionary<int, int>, Dictionary<int, Entity>) Main(NativeHashMap<Color, int> colorLookup,
            Entity oceanDefault)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var definedNames = new List<string>();
            var idLookup = new Dictionary<int, int>();
            var provEntityLookup = new Dictionary<int, Entity>();

            foreach (var rawLine in File.ReadLines(Path.Combine(Application.streamingAssetsPath,
                "map", "definition.csv")))
            {
                if (CommentDetector(rawLine, out var line))
                    continue;

                var subStringed = line.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                if (subStringed.Length < 5) throw new Exception("Definitions file not following format: " + line);
                // Num;R;G;B;Name;x
                int.TryParse(subStringed[0], out var provNum);
                byte.TryParse(subStringed[1], out var red);
                byte.TryParse(subStringed[2], out var green);
                byte.TryParse(subStringed[3], out var blue);
                var foundColor = new Color32(red, green, blue, 255);

                if (provNum >= 10000)
                    // LIMITATION:
                    // MAXIMUM PROVINCE NUMBER OF 9999. Limited by unified magic numbers.
                    throw new Exception("Province number out of range. Max 9999. Number used: " + provNum);

                idLookup.Add(provNum, definedNames.Count);
                colorLookup.Add(foundColor, definedNames.Count);

                var provEntity = em.CreateEntity(typeof(Province), typeof(OceanProvince));
                em.SetComponentData(provEntity, new Province {Index = definedNames.Count, Owner = oceanDefault});
                provEntityLookup.Add(provNum, provEntity);

                definedNames.Add(subStringed[4].Trim());
                em.SetName(provEntity, "Province: " + definedNames.Last()); // DEBUG
            }

            return (definedNames, idLookup, provEntityLookup);

            static bool CommentDetector(string line, out string sliced)
            {
                // Comment Detector. Will also lowercase everything. Throwing away comments.
                sliced = line.ToLowerInvariant().Split(new[] {"#"}, StringSplitOptions.None)[0].Trim();
                return sliced.Length == 0;
            }
        }

        /*
        [Serializable]
        private struct DefinitionOutput
        {
            public List<string> DefinedNames;
            public List<int> IdIndexes;
            public List<Color32> FoundColors;

            public DefinitionOutput(List<string> definedNames, List<int> idIndexes, List<Color32> foundColors)
            {
                DefinedNames = definedNames;
                IdIndexes = idIndexes;
                FoundColors = foundColors;
            }
            
            public void Deconstruct(out List<string> definedNames, out List<int> idIndexes, out List<Color32> foundColors)
            {
                definedNames = DefinedNames;
                idIndexes = IdIndexes;
                foundColors = FoundColors;
            }
        }
        */
    }
}