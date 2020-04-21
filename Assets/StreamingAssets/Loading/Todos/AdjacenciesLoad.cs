using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public static class AdjacenciesLoad
    {
        public enum CrossingTypes
        {
            Sea,
            Land,
            Impassable,
            Canal
        }

        public static AdjacenciesOutput Main()
        {
            var validConnections = new List<int2>();
            var impassables = new List<int2>();

            foreach (var rawLine in File.ReadLines(Path.Combine(Application.streamingAssetsPath, "Map/adjacencies.csv"))
            )
            {
                if (LoadMethods.CommentDetector(rawLine, out var line))
                    continue;

                var subStringed = line.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                if (subStringed.Length < 3) throw new Exception("Adjacencies file not following format! " + line);

                // From;To;Type;Through;Data;Comment
                var checker = int.TryParse(subStringed[0], out var provFrom);
                checker = int.TryParse(subStringed[1], out var provTo) && checker;

                if (!checker)
                    throw new Exception("Error in parsing adjancencies! " + line);

                // TODO: Make crossing type relevant.

                if (!Enum.TryParse(subStringed[2], true, out CrossingTypes crossingType))
                    throw new Exception("Unknown crossing! " + line);

                switch (crossingType)
                {
                    case CrossingTypes.Sea:
                    case CrossingTypes.Land:
                        validConnections.Add(new int2(provFrom, provTo));
                        break;
                    case CrossingTypes.Impassable:
                        impassables.Add(new int2(provFrom, provTo));
                        break;
                    case CrossingTypes.Canal:
                        break;
                }
            }

            return new AdjacenciesOutput
            {
                validConnections = validConnections,
                impassables = impassables
            };
        }

        [Serializable]
        public struct AdjacenciesOutput
        {
            public List<int2> validConnections;
            public List<int2> impassables;
        }
    }
}