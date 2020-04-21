﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;

namespace Conversion
{
    public static class FileUnpacker
    {
        /*
         * INSTRUCTIONS:
         * Open brackets MUST BE IN THE SAME LINE as value declaration.
         *      So no:
         *      Example = { # Good.
         *      { # No.
         *          Foo = Bar
         *      }
         * Examples from stock V2 files that must be modified include the first school
         *     in technology.txt, Army_Tech open bracket is on new line.
         *
         * It is recommended to merge redundant object declaration into 1 object with multiple values.
         * Examples include the rgo_goods_output in technologies.
         *     Instead of listing rgo_goods_output = { good1 = 0.25 }
         *     rgo_goods_output = { good2 = 0.25 }...
         *     The files should be rgo_goods_output = { good1 = 0.25 good2 = 0.25... }
         *
         * All images must be converted to PNG. Unity can not import other loss-less forms of images as usable Color32 arrays.
         *     Significant example is the Flags folder. Use batch image converting software to convert the folder from tga to png.
         *
         * Terrain.txt requires extensive rewriting.
         *     1. Add to each terrain the colors that it is assigned in the terrain.png file as an palette = HTML color code.
         *         You can find those values by opening up the file in GIMP, locating the colormap window,
         *         and writing down each color corresponding to color = { # } in the original terrain file.
         *     2. Delete everything except for the Category object.
         *     3. Delete the "Category = " and the { } that it is assigned to.
         *     Terrain types should end up something like:
         *         arctic = {
         *             ... # terrain modifiers
         *             # Colors corresponding to terrain.png
	     *              palette = ececec
	     *              palette = d2d2d2
	     *              palette = b0b0b0
	     *              palette = 8c8c8c
	     *              palette = 707070
         *          }
         */
        public static void ParseFile(string target, List<(int Key, object Value)> fileTree, Func<int, string, int> magicOverride)
        {
            var currentBranch = fileTree;
            var currentChain = new List<int>();
            var parent = -1;

            foreach (var rawLine in File.ReadLines(target))
            {
                if (LoadMethods.CommentDetector(rawLine, out var line))
                    continue;

                var curEquals = SplitByFirstEquals(line);

                while (true)
                {
                    CheckClosingBrackets(ref curEquals[0], ref currentBranch);

                    if (DetectBranchType(curEquals, out var curLevel))
                    {
                        if (CheckOpenBrackets(ref curEquals[1]))
                        {
                            // Updating trackers
                            currentChain.Add(currentBranch.Count);
                            parent = curLevel;

                            var newBranch = new List<(int Key, object Value)>();
                            currentBranch.Add((curLevel, newBranch));
                            currentBranch = newBranch;
                        }
                        else
                        {
                            curLevel = ReplaceFunc(curLevel);

                            currentBranch.Add((curLevel, InlineSplitter(ref curEquals[1])));
                        }
                    }

                    if (string.IsNullOrEmpty(curEquals[0]) || curLevel == (int) CallContinue.Skip)
                        break;

                    curEquals = SplitByFirstEquals(curEquals[1]);
                }
            }

            int ReplaceFunc(int curLevel)
            {
                switch ((LoadVariables) curLevel)
                {
                    // Blame Events
                    case LoadVariables.Ideology:
                        return (int) LoadVariables.IdeologyProperty;
                    case LoadVariables.CountryEvent:
                        return (int) LoadVariables.CountryEventProperty;
                    case LoadVariables.Chance:
                        return (int) LoadVariables.ChanceProperty;
                    case LoadVariables.War:
                        return (int) LoadVariables.WarProperty;
                    case LoadVariables.Country:
                        return (int) LoadVariables.CountryProperty;
                    case LoadVariables.CasusBelli:
                        return (int) LoadVariables.CasusBelliProperty;
                    case LoadVariables.From:
                        return (int) LoadVariables.FromProperty;
                    case LoadVariables.LifeNeeds:
                        return (int) LoadVariables.LifeNeedsProperty;
                    case LoadVariables.EverydayNeeds:
                        return (int) LoadVariables.EverydayNeedsProperty;
                    case LoadVariables.LuxuryNeeds:
                        return (int) LoadVariables.LuxuryNeedsProperty;
                    default:
                        switch ((MagicUnifiedNumbers) (curLevel / 10000 * 10000))
                        {
                            case MagicUnifiedNumbers.SubPolicy:
                                return (int) MagicUnifiedNumbers.PopSubPolicy + curLevel -
                                       (int) MagicUnifiedNumbers.SubPolicy;
                        }

                        return curLevel;
                }
            }

            bool DetectBranchType(IList<string> raw, out int parsed)
            {
                if (string.IsNullOrEmpty(raw[0]) || string.IsNullOrEmpty(raw[1]))
                {
                    // If i were to implement open bracket on new line, it will be here.
                    parsed = (int) CallContinue.Null;
                    return false;
                }

                parsed = UnifiedVariables.MagicNumbers(parent, raw, magicOverride);

                // Color overrides
                if (parsed != (int) LoadVariables.Color &&
                    parsed / 10000 != (int) MagicUnifiedNumbers.Government / 10000)
                    return parsed >= 0;

                // TODO: add checker in case color line contains an exit bracket. IE: color = { 0 0 0 } >}<
                if (string.IsNullOrEmpty(raw[1]))
                    return false;

                currentBranch.Add((parsed, raw[1]));

                parsed = (int) CallContinue.Skip;
                return false;
            }

            bool CheckOpenBrackets(ref string raw)
            {
                /* LIMITATION:
             * Open brackets MUST BE IN THE SAME LINE as value declaration.
             * So no:
             * Example =
             * { # No.
             *     Hello = World
             * }
             * This is because of IEnumerable limitations.
             * Sure, I can do ReadAllLines but that requires loading the entire file into memory.
             * This mainly concerns technology in common, Army_Tech open bracket is on new line.
             */
                var indexBracket = raw.IndexOf('{');
                var indexEqual = raw.IndexOf('=');

                if (indexBracket == -1)
                    return false;

                if (indexEqual > -1 && indexBracket > indexEqual)
                    return false;

                raw = raw.Remove(indexBracket, 1);
                return true;
            }

            string InlineSplitter(ref string raw)
            {
                var value = Regex.Match(raw,
                    raw[0].Equals('"')
                        ? @"^.+?\"""
                        : @"^.+?(?![a-zA-Z0-9\._])");

                if (!value.Success)
                    throw new Exception("Value not found. " + raw);

                raw = raw.Substring(value.Length).Trim();
                return value.Value;
            }

            void CheckClosingBrackets(ref string raw, ref List<(int Key, object Value)> deltaBranch)
            {
                var numClosed = raw.Count(c => c == '}');

                if (numClosed == 0)
                    return;

                raw = raw.Replace("}", "");

                var min = currentChain.Count - numClosed;
                if (min < 0)
                {
                    numClosed += min;
                    min = 0;
                }

                currentChain.RemoveRange(min, numClosed);
                deltaBranch = fileTree;
                foreach (var newChain in currentChain)
                {
                    // Updating parent
                    parent = deltaBranch[newChain].Key;
                    deltaBranch = (List<(int Key, object Value)>) deltaBranch[newChain].Value;
                }

                if (currentChain.Count != 0)
                    return;

                parent = -1;
            }

            string[] SplitByFirstEquals(string input)
            {
                input = input.Trim();
                var equals = Regex.Match(input, @"^.+?(?=\=)");

                var output = new[] {input, string.Empty};
                if (equals.Success)
                    output = new[] {equals.Value.Trim(), input.Substring(equals.Length + 1).Trim()};

                return output;
            }
        }
        private enum CallContinue
        {
            Null = -1000,
            Skip = -2000
        }

//        public static void ProcessQueue(KeyValuePair<int, object> rawLevel,
//            List<DataValue> coreActions, List<DataRange> rangeLevels,
//            NativeMultiHashMap<int, int> parentLocation, List<string> values,
//            Func<string, KeyValuePair<int, object>, bool> switchOverrides)
//        {
//            /*
//         * Work flow:
//         * 1. Identify and allocate core actions.
//         * 2. Record core range.
//         * 3. Create placeholders for sub-levels in current level.
//         * 4. Record current level range.
//         * 5. Complete parent range.
//         * 6. Recursion to next range.
//         */
//
//            var newQueue = new Queue<KeyValuePair<int, object>>();
//
//            var coreStartIndex = coreActions.Count;
//            var subRangeStartIndex = rangeLevels.Count;
//
//            try
//            {
//                foreach (var kvpObject in (List<KeyValuePair<int, object>>) rawLevel.Value)
//                    UnifiedVariables.HandlePart(kvpObject, newQueue, values, coreActions, switchOverrides);
//            }
//            catch (Exception _)
//            {
//                Debug.Log((LoadVariables) rawLevel.Key);
//                throw;
//            }
//
//            // Core levels.
//            if (coreStartIndex != coreActions.Count)
//                rangeLevels.Add(new DataRange((int) LoadVariables.BreakCore, coreStartIndex, coreActions.Count));
//
//            foreach (var newParent in newQueue)
//            {
//                parentLocation.Add(newParent.Key, rangeLevels.Count);
//                rangeLevels.Add(new DataRange(newParent.Key, -1, -1));
//            }
//
//            // Grabbing parent
//            parentLocation.TryGetFirstValue(rawLevel.Key, out var index, out var remover);
//
//            // Completing parent
//            rangeLevels[index] = new DataRange(rangeLevels[index].Type, subRangeStartIndex, rangeLevels.Count);
//
//            // Removing used parent.
//            parentLocation.Remove(remover);
//
//            while (newQueue.Any())
//                ProcessQueue(newQueue.Dequeue(), coreActions, rangeLevels, parentLocation, values, switchOverrides);
//        }
//
//        public static (List<DataValue> CoreActions, Queue<KeyValuePair<int, object>> NextQueue)
//            TrimmedProcessQueue(KeyValuePair<int, object> rawLevel, List<string> values,
//                Func<string, KeyValuePair<int, object>, bool> switchOverrides)
//        {
//            var newQueue = new Queue<KeyValuePair<int, object>>();
//            var coreActions = new List<DataValue>();
//
//            try
//            {
//                foreach (var kvpObject in (List<KeyValuePair<int, object>>) rawLevel.Value)
//                    UnifiedVariables.HandlePart(kvpObject, newQueue, values, coreActions, switchOverrides);
//            }
//            catch (Exception _)
//            {
//                Debug.Log((LoadVariables) rawLevel.Key);
//                throw;
//            }
//
//            return (coreActions, newQueue);
//        }

/*
        public static void GetCollector<TCollectionType>()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Entity collectorEntity;

            using (var entities = em.CreateEntityQuery(typeof(TCollectionType)))
            using (var wrappers = entities.ToEntityArray(Allocator.TempJob))
            {
                collectorEntity = em.CreateEntity(typeof(DataCollection), typeof(TCollectionType));

                em.AddBuffer<EntityWrapper>(collectorEntity).AddRange(wrappers.Reinterpret<EntityWrapper>());
                // Eliminating entity wrapper struct. Direct entity array.
                em.GetBuffer<EntityWrapper>(collectorEntity).Reinterpret<Entity>();
            }
        }

        public static (List<FirstLevelCore>, List<(int Key, object Value)>)
            AssignFirstLevelDistributeWorkload<TData>(ref TData input, (int Key, object Value) rawLevel,
                Func<(int Key, object Value), bool> firstLevelOverrides, Func<string, (int, object), (bool, float)> switchOverrides = null)
            where TData : IDataEntity
        {
            // Assigns default values to input and distributes rest of file tree object into specified array for later processing.

            var defaults = Array.ConvertAll(input.DefaultTypes(), item => (int) item);
            var runningConstruction = new string[defaults.Length];

            var ranges = Array.ConvertAll(input.RangeTypes(), item => (int) item);
            var runningWorkload = new List<(int Key, object Value)>(ranges.Length);

            var breakCoreRaw = new List<(int Key, string Value)>();

            foreach (var property in (List<(int Key, object Value)>) rawLevel.Value)
            {
                if (firstLevelOverrides(property))
                    continue;

                var foundPosition = Array.IndexOf(defaults, property.Key);
                if (foundPosition != -1)
                {
                    runningConstruction[foundPosition] = (string) property.Value;
                    continue;
                }

                foundPosition = Array.IndexOf(ranges, property.Key);
                if (foundPosition != -1)
                {
                    runningWorkload[foundPosition] = property;
                    continue;
                }

                if (property.Key > (int) LoadVariables.BreakCore)
                    throw new Exception("Unknown range type in first level distribution: " + (LoadVariables) property.Key);

                breakCoreRaw.Add((property.Key, (string) property.Value));
            }

            input.AssignDefaults(runningConstruction);

            var breakCoreRanges = new List<FirstLevelCore>(breakCoreRaw.Count);

            foreach (var data in breakCoreRaw)
            {
                var target = new FirstLevelCore();
                UnifiedVariables.ConvertToType(ref target, data, switchOverrides);
                breakCoreRanges.Add(target);
            }

            return (breakCoreRanges, runningWorkload);
        }

        public static List<TWrapper> Flatten<TWrapper>(TWrapper wrapperConstructor, IEnumerable<(int, object)> inputBranch)
            where TWrapper : IDataValue<TWrapper>
        {
            var output = new List<DataValue>();
            foreach (var (key, value) in inputBranch)
            {
                if (key < (int) LoadVariables.BreakCore)
                {
                    // Value type at second level. No parent modifiers.
                    
                }
            }

            return output.ConvertAll(wrapperConstructor.GenerateWrapper);
        }
        */
    }
}