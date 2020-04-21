using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public struct GameEventInfo
    {
        public bool Fired, FireOnlyOnce;
        public int Index;

        public override string ToString()
        {
            return $"Index: {Index}.";
        }
    }

    public static class EventsLoad
    {
        public static void Main()
        {
            // TODO: Needs policies and ideologies.

            // Output events list.
            var events = new List<GameEventInfo>();

            // Collapse tree into arrays
            var eventRanges = new List<int3>(); // x: type. z: start index, inclusive. w: end index, exclusive
            // If type == random_list, x is replaced with chance.
            // Value arrays
            var eventActions = new List<float2>(); // x: type. z: threshold
            var stringValues = new List<string>();

            // TODO: Possibly convert all disposes of parent location to using?
            var parentLocation = new NativeMultiHashMap<int, int>(10, Allocator.TempJob);

            foreach (var eventFile in Directory.EnumerateFiles(Path.Combine(Application.streamingAssetsPath, "Events"),
                "*.txt"))
            {
                var fileTree = new List<KeyValuePair<int, object>>();
                var values = new List<string>();

                FileUnpacker.ParseFile(eventFile, fileTree, values, EventMagicOverride);

                GameEventInfo currentEvent;

                foreach (var parsedEvent in fileTree)
                {
                    currentEvent = new GameEventInfo {Fired = false, Index = eventRanges.Count};
                    parentLocation.Add(parsedEvent.Key, eventRanges.Count);
                    eventRanges.Add(new int3(parsedEvent.Key, -1, -1));

                    //FileUnpacker.ProcessQueue(parsedEvent, eventActions, eventRanges,
                    //parentLocation, values, EventSwitchOverride);

                    events.Add(currentEvent);
                }

                bool EventSwitchOverride(string targetStr, KeyValuePair<int, object> kvpObject)
                {
                    switch ((LoadVariables) kvpObject.Key)
                    {
                        case LoadVariables.FireOnlyOnce:
                            currentEvent.FireOnlyOnce = LoadMethods.YesNoConverter(targetStr);
                            return true;
                        case LoadVariables.ChangeRegionName:
                        case LoadVariables.ChangeProvinceName:
                        case LoadVariables.Title:
                        case LoadVariables.Desc:
                        case LoadVariables.Picture:
                        case LoadVariables.Name:
                            eventActions.Add(new float2(kvpObject.Key, stringValues.Count));
                            stringValues.Add(targetStr.Replace("\"", ""));
                            return true;
                        case LoadVariables.HasLeader: // String
                            // Skipping
                            return true;
                        default:
                            return false;
                    }
                }

                // Assigns magic numbers.
                int EventMagicOverride(int parent, string str)
                {
                    if ((parent == (int) LoadVariables.AddCasusBelli
                         || parent == (int) LoadVariables.CasusBelli)
                        && str.Equals("type"))
                        return (int) LoadVariables.TypeCasusBelli;

                    if (parent != (int) LoadVariables.RandomList)
                        return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                    if (!int.TryParse(str, out var probability))
                        throw new Exception("Random list probability unknown! " + str);
                    return (int) MagicUnifiedNumbers.Probabilities + probability;
                }
            }

            parentLocation.Dispose();
        }
    }
}