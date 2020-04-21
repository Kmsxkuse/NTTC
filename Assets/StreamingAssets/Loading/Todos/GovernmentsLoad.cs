using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct GovernmentCollection : IComponentData
    {
    }

    public struct GovernmentEntity : IComponentData
    {
        public enum Flag
        {
            Republic,
            Monarchy,
            Fascist,
            Communist
        }

        public Flag FlagType;

        public bool Election, AppointRulingParty;
        public int Duration, Index;
    }


    public static class GovernmentsLoad
    {
        public static (Entity, List<string>) Main()
        {
            var governments = new NativeList<EntityWrapper>(Allocator.Temp);
            var governmentNames = new List<string>();

            var fileTree = new List<KeyValuePair<int, object>>();
            var values = new List<string>();

            var em = World.Active.EntityManager;

            FileUnpacker.ParseFile(Path.Combine(Application.streamingAssetsPath, "Common", "governments.txt"), fileTree,
                values, GovernmentMagicOverride);

            using (var governIdeologies = new NativeList<DataInt>(Allocator.Temp))
            {
                for (var index = 0; index < fileTree.Count; index++)
                {
                    var currentEntity = governments[index];
                    var currentGovernment = new GovernmentEntity
                    {
                        Index = fileTree[index].Key - (int) MagicUnifiedNumbers.Placeholder
                    };

                    foreach (var govProp in (List<KeyValuePair<int, object>>) fileTree[index].Value)
                    {
                        var targetStr = values[(int) govProp.Value];
                        switch (govProp.Key / 10000)
                        {
                            case 0:
                                switch ((LoadVariables) govProp.Key)
                                {
                                    case LoadVariables.AppointRulingParty:
                                        currentGovernment.AppointRulingParty = LoadMethods.YesNoConverter(targetStr);
                                        break;
                                    case LoadVariables.Election:
                                        currentGovernment.Election = LoadMethods.YesNoConverter(targetStr);
                                        break;
                                    case LoadVariables.Duration: // Election cycle
                                        if (!int.TryParse(targetStr, out var duration))
                                            throw new Exception("Unknown government duration. " + targetStr);

                                        currentGovernment.Duration = duration;
                                        break;
                                    case LoadVariables.FlagType:
                                        if (!Enum.TryParse(targetStr, true, out GovernmentEntity.Flag flagType))
                                            throw new Exception("Unknown government flag type. " + targetStr);

                                        currentGovernment.FlagType = flagType;
                                        break;
                                    default:
                                        throw new Exception("Invalid government file structure. " +
                                                            (LoadVariables) govProp.Key);
                                }

                                break;
                            case (int) MagicUnifiedNumbers.Ideology / 10000:
                                if (LoadMethods.YesNoConverter(targetStr))
                                    governIdeologies.Add(govProp.Key - (int) MagicUnifiedNumbers.Ideology);
                                break;
                            default:
                                throw new Exception("Invalid magic number detected in governments.");
                        }
                    }

                    em.AddBuffer<DataInt>(currentEntity).AddRange(governIdeologies);
                    governIdeologies.Clear();
                    em.SetComponentData(currentEntity, currentGovernment);
                }
            }

            var governmentCollectorEntity = FileUnpacker.GetCollector<GovernmentCollection>(governments);
            governments.Dispose();

            return (governmentCollectorEntity, governmentNames);

            int GovernmentMagicOverride(int parent, string raw)
            {
                if (parent != -1)
                    return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                governments.Add(em.CreateEntity(typeof(GovernmentEntity)));

                governmentNames.Add(raw);
                // Government is used as a color override.
                return (int) MagicUnifiedNumbers.Placeholder + governmentNames.Count - 1;
            }
        }
    }
}