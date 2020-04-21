﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public class RegionsLoad : MonoBehaviour
    {
        public static IEnumerable<string> Main(Dictionary<int, int> idIndex, NativeHashMap<int, int> stateLookup)
        {
            var slicedText = File.ReadLines(Path.Combine(Application.streamingAssetsPath, "map", "region.txt"),
                Encoding.GetEncoding(1252));
            
            var stateIdNames = new List<string>();

            foreach (var rawLine in slicedText)
            {
                if (CommentDetector(rawLine, out var line))
                    continue;

                var choppedLine = line.Split(new[] {'{', '}'}, StringSplitOptions.RemoveEmptyEntries);
                // 0. StateId = | 1. Prov 1 Prov 2 Prov 3... | 2. #StateName

                var innerChopped = choppedLine[1].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var colorNum in innerChopped)
                {
                    if (!int.TryParse(colorNum, out var lookupNum))
                        throw new Exception("Invalid province ID. Must be int number.");
                    stateLookup.Add(idIndex[lookupNum], stateIdNames.Count);
                }

                stateIdNames.Add(Regex.Match(choppedLine[0], @"^.+?(?=\=)").Value.Trim());
            }
            
            return stateIdNames;
            
            bool CommentDetector(string line, out string sliced)
            {
                // Comment Detector. Will also lowercase everything. Throwing away comments.
                sliced = line.ToLowerInvariant().Split(new[] {"#"}, StringSplitOptions.None)[0].Trim();
                return sliced.Length == 0;
            }
        }
    }
}