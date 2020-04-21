﻿using System;
using System.Collections.Generic;
using System.IO;
 using System.Linq;
 using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct IdeologyCollection : IComponentData
    {
    }

    [Serializable]
    public struct IdeologyEntity : IComponentData, IDataEntity, IDataName
    {
        public int Date,
            AddPolitical,
            RemovePolitical,
            AddSocial,
            RemoveSocial,
            AddEconomic,
            RemoveEconomic,
            AddMilitary,
            RemoveMilitary;
        
        public Color32 Color;
        public bool Uncivilized, CanReduceMilitancy;
        public NativeString64 Group, Name;

        public LoadVariables[] DefaultTypes()
        {
            return new[] {LoadVariables.Color, LoadVariables.Uncivilized, LoadVariables.CanReduceMilitancy, LoadVariables.Date};
        }

        public LoadVariables[] RangeTypes()
        {
            // All range types will be compressed into one dynamic buffer.
            // Indices for determining each found in the int properties above.
            return new[]
            {
                LoadVariables.AddPoliticalReform, LoadVariables.RemovePoliticalReform, LoadVariables.AddSocialReform,
                LoadVariables.RemoveSocialReform, LoadVariables.AddMilitaryReform, LoadVariables.RemoveMilitaryReform,
                LoadVariables.AddEconomicReform, LoadVariables.RemoveEconomicReform
            };
        }

        public void AssignDefaults(IReadOnlyList<string> construction)
        {
            Color = LoadMethods.ParseColor32(construction[0]);
            Uncivilized = LoadMethods.YesNoConverter(construction[1]);
            CanReduceMilitancy = LoadMethods.YesNoConverter(construction[2]);
            
            if (!DateTime.TryParse(construction[3], out var dateResult))
                throw new Exception("Unknown date. " + construction[3]);
            int.TryParse("9" + dateResult.ToString("yyyyMMdd"), out Date);
        }
        
        public void SetName(string name)
        {
            Name = name;
        }

        public bool GroupType()
        {
            return true;
        }

        public void SetGroup(string group)
        {
            Group = group;
        }
    }

    public static class IdeologiesLoad
    {
        public static (List<string>, List<string>) Main(bool cache)
        {
            
        }
        public static IdeologyOutput ParseParadoxFile()
        {
            var fileTree = new List<(int, object)>();

            var ideologyNames = new List<string>();
            var groupNames = new List<string>();
            var ideologies = new List<IdeologyEntity>();
            var ideologyFirstLevels = new List<List<FirstLevelCore>>();

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            FileUnpacker.ParseFile(Path.Combine(Application.streamingAssetsPath, "common", "ideologies.txt"),
                fileTree, IdeologyMagicOverride);


            for (var index = 0; index < fileTree.Count; index++)
            {
                var currentIdeology = ideologies[index];

                var (breakCores, reformModifiers) =
                    FileUnpacker.AssignFirstLevelDistributeWorkload(ref currentIdeology, fileTree[index], IdeologyOverride);

                ideologyFirstLevels.Add(breakCores);
                

                bool IdeologyOverride((int Key, object Value) target)
                {
                    return false;
                }
            }

            return (ideologyNames, groupNames);
            
            int IdeologyMagicOverride(int parent, string str)
            {
                if (parent == -1)
                {
                    groupNames.Add(str);

                    return (int) MagicUnifiedNumbers.IdeologyGroup + groupNames.Count - 1;
                }

                if (parent / 10000 == (int) MagicUnifiedNumbers.IdeologyGroup / 10000)
                {
                    ideologies.Add(new IdeologyEntity {Name = str, Group = groupNames.Last()});
                    ideologyNames.Add(str);

                    return (int) MagicUnifiedNumbers.Ideology + ideologyNames.Count - 1;
                }

                return (int) MagicUnifiedNumbers.ContinueMagicNumbers;
            }

            /*
            var counter = 0;

            using (var parentLocation = new NativeMultiHashMap<int, int>(10, Allocator.Temp))
            {
                foreach (var ideologyGroup in fileTree)
                foreach (var ideKvp in (List<KeyValuePair<int, object>>) ideologyGroup.Value)
                {
                    var currentEntity = ideologies[counter++];
                    var currentIdeology = em.GetComponentData<IdeologyEntity>(currentEntity);

                    var ideologyRanges = new List<DataRange>();
                    var ideologyActions = new List<DataValue>();

                    FileUnpacker.ProcessQueue(ideKvp, ideologyActions, ideologyRanges, parentLocation, values,
                        IdeologySwitchOverride);

                    using (var tempRange = new NativeArray<DataRange>(ideologyRanges.ToArray(), Allocator.Temp))
                    {
                        em.AddBuffer<DataRange>(currentEntity).AddRange(tempRange);
                    }

                    using (var tempAction = new NativeArray<DataValue>(ideologyActions.ToArray(), Allocator.Temp))
                    {
                        em.AddBuffer<DataValue>(currentEntity).AddRange(tempAction);
                    }

                    em.SetComponentData(currentEntity, currentIdeology);

                    bool IdeologySwitchOverride(string targetStr, KeyValuePair<int, object> subSection)
                    {
                        switch ((LoadVariables) subSection.Key)
                        {
                            case LoadVariables.CanReduceMilitancy:
                            case LoadVariables.Uncivilized:
                                currentIdeology.Uncivilized =
                                    LoadMethods.YesNoConverter(values[(int) subSection.Value]);
                                return true;
                            case LoadVariables.Color:
                                currentIdeology.Color = LoadMethods.ParseColor32(values[(int) subSection.Value]);
                                return true;
                            case LoadVariables.Date:
                                if (!DateTime.TryParse(values[(int) subSection.Value], out var date))
                                    throw new Exception("Unknown date: " + values[(int) subSection.Value]);

                                int.TryParse(date.ToString("yyyyMMdd"), out var dateInt);
                                currentIdeology.Date = dateInt;
                                return true;
                            case LoadVariables.AddPoliticalReform:
                                currentIdeology.AddPolitical = ideologyRanges.Count;
                                return false;
                            case LoadVariables.RemovePoliticalReform:
                                currentIdeology.RemovePolitical = ideologyRanges.Count;
                                return false;
                            case LoadVariables.AddSocialReform:
                                currentIdeology.AddSocial = ideologyRanges.Count;
                                return false;
                            case LoadVariables.RemoveSocialReform:
                                currentIdeology.RemoveSocial = ideologyRanges.Count;
                                return false;
                            case LoadVariables.AddMilitaryReform:
                                currentIdeology.AddMilitary = ideologyRanges.Count;
                                return false;
                            case LoadVariables.AddEconomicReform:
                                currentIdeology.AddEconomic = ideologyRanges.Count;
                                return false;
                            default:
                                return false;
                        }
                    }
                }
            }

            var ideologyCollectorEntity = FileUnpacker.GetCollector<IdeologyCollection>(ideologies);
            ideologies.Dispose();

            var groupCollectorEntity = FileUnpacker.GetCollector<IdeologyGroupCollection>(groups);
            groups.Dispose();

            return (ideologyCollectorEntity, groupCollectorEntity, ideologyNames, groupNames);

            // Special file parsing instructions.
            // Assigns magic numbers.
            int IdeMagicOverride(int parent, string str)
            {
                if (parent == -1)
                {
                    var targetGroup = em.CreateEntity(typeof(IdeologyGroupEntity));
                    em.SetComponentData(targetGroup, new IdeologyGroupEntity {Index = groupNames.Count});

                    currentGroup = targetGroup;

                    groups.Add(targetGroup);
                    groupNames.Add(str);

                    return (int) MagicUnifiedNumbers.IdeologyGroup + groupNames.Count - 1;
                }

                if (parent / 10000 == (int) MagicUnifiedNumbers.IdeologyGroup / 10000)
                {
                    var targetIdeology = em.CreateEntity(typeof(IdeologyEntity));
                    em.SetComponentData(targetIdeology,
                        new IdeologyEntity {Index = ideologyNames.Count, Group = currentGroup});

                    ideologies.Add(targetIdeology);
                    ideologyNames.Add(str);

                    return (int) MagicUnifiedNumbers.Ideology + ideologyNames.Count - 1;
                }

                return (int) MagicUnifiedNumbers.ContinueMagicNumbers;
            }
            */
        }
    }
}