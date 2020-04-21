using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct SubPolicyCollection : IComponentData
    {
    }

    public struct PolicyGroupCollection : IComponentData
    {
    }

    public struct PolicyGroupEntity : IComponentData
    {
        public int Index;
        public bool NextStepOnly, Administrative;
    }

    public struct SubPolicyEntity : IComponentData
    {
        public int Index;
        public Entity Group;
    }

    public static class IssuesLoad
    {
        public static (List<KeyValuePair<int, object>>, List<string>, Entity, Entity, List<string>, List<string>)
            PolicyNames()
        {
            // Exports first policy names
            var fileTree = new List<KeyValuePair<int, object>>();
            var values = new List<string>();

            var policyGroups = new NativeList<EntityWrapper>(Allocator.Temp);
            var subPolicies = new NativeList<EntityWrapper>(Allocator.Temp);
            var policyGroupNames = new List<string>();
            var subPolicyNames = new List<string>();
            var policyGroupLookup = new Dictionary<string, int>();

            var em = World.Active.EntityManager;

            FileUnpacker.ParseFile(Path.Combine(Application.streamingAssetsPath, "Common", "issues.txt"),
                fileTree, values, IssueMagicOverride);

            var policyGroupCollectorEntity = FileUnpacker.GetCollector<SubPolicyCollection>(policyGroups);
            policyGroups.Dispose();

            var subPolicyCollectorEntity = FileUnpacker.GetCollector<PolicyGroupCollection>(subPolicies);
            subPolicies.Dispose();

            return (fileTree, values, subPolicyCollectorEntity, policyGroupCollectorEntity, subPolicyNames,
                policyGroupNames);

            // Assigns magic numbers.
            int IssueMagicOverride(int parent, string str)
            {
                // Following passes into SubPolicies due to being in same level as actual sub policies
                if (str.Equals("next_step_only") || str.Equals("administrative"))
                    return (int) MagicUnifiedNumbers.ContinueMagicNumbers;

                if (policyGroupLookup.TryGetValue(str, out var policyIndex))
                    return policyIndex + (int) MagicUnifiedNumbers.PolicyGroup;

                if (parent < (int) LoadVariables.BreakPoliciesEnd && parent > (int) LoadVariables.BreakPoliciesStart)
                {
                    policyGroupLookup.Add(str, policyGroupNames.Count);
                    // Entity setting is done in Main().
                    policyGroups.Add(em.CreateEntity(typeof(PolicyGroupEntity)));
                    policyGroupNames.Add(str);
                    return (int) MagicUnifiedNumbers.PolicyGroup + policyGroupNames.Count - 1;
                }

                if (parent / 10000 == (int) MagicUnifiedNumbers.PolicyGroup / 10000)
                {
                    subPolicies.Add(em.CreateEntity(typeof(SubPolicyEntity)));
                    subPolicyNames.Add(str);
                    return (int) MagicUnifiedNumbers.SubPolicy + subPolicyNames.Count - 1;
                }

                return (int) MagicUnifiedNumbers.ContinueMagicNumbers;
            }
        }

        public static void Main(in IEnumerable<KeyValuePair<int, object>> fileTree, in List<string> values,
            Entity subPolicies, Entity policyGroups)
        {
            var issueRanges = new List<DataRange>();
            var issueActions = new List<DataValue>();

            var em = World.Active.EntityManager;

            // Unity doesnt fully support C#8
            // ReSharper disable once ConvertToUsingDeclaration
            using (var policyGroupsArray = em.GetBuffer<EntityWrapper>(policyGroups).ToNativeArray(Allocator.Temp))
            using (var subPoliciesArray = em.GetBuffer<EntityWrapper>(subPolicies).ToNativeArray(Allocator.Temp))
            using (var subCollector = new NativeList<EntityWrapper>(Allocator.Temp))
            using (var parentLocation = new NativeMultiHashMap<int, int>(1, Allocator.Temp))
            {
                foreach (var hardCodedGroup in fileTree) // E.g: PartyIssues, SocialReforms
                foreach (var policyGroup in (List<KeyValuePair<int, object>>) hardCodedGroup.Value)
                {
                    var targetPolicyGroupEntity =
                        policyGroupsArray[policyGroup.Key - (int) MagicUnifiedNumbers.PolicyGroup];
                    var currentPolicyGroup = new PolicyGroupEntity
                        {Index = policyGroup.Key - (int) MagicUnifiedNumbers.PolicyGroup};

                    foreach (var subPolicy in (List<KeyValuePair<int, object>>) policyGroup.Value)
                    {
                        switch ((LoadVariables) subPolicy.Key)
                        {
                            case LoadVariables.NextStepOnly:
                                currentPolicyGroup.NextStepOnly =
                                    LoadMethods.YesNoConverter(values[(int) subPolicy.Value]);
                                continue;
                            case LoadVariables.Administrative:
                                currentPolicyGroup.Administrative =
                                    LoadMethods.YesNoConverter(values[(int) subPolicy.Value]);
                                continue;
                        }

                        var targetSubPolicyEntity =
                            subPoliciesArray[subPolicy.Key - (int) MagicUnifiedNumbers.SubPolicy];
                        subCollector.Add(targetSubPolicyEntity);

                        var currentSubPolicy = new SubPolicyEntity
                        {
                            Index = subPolicy.Key - (int) MagicUnifiedNumbers.SubPolicy,
                            Group = targetPolicyGroupEntity
                        };
                        em.SetComponentData(targetSubPolicyEntity, currentSubPolicy);

                        parentLocation.Add(subPolicy.Key, issueRanges.Count);
                        issueRanges.Add(new DataRange(subPolicy.Key, -1, -1));

                        FileUnpacker.ProcessQueue(subPolicy, issueActions, issueRanges,
                            parentLocation, values, (s, pair) => false);

                        using (var tempDataRange = new NativeArray<DataRange>(issueRanges.ToArray(), Allocator.Temp))
                        {
                            em.AddBuffer<DataRange>(targetSubPolicyEntity).AddRange(tempDataRange);
                        }

                        issueRanges.Clear();

                        using (var tempDataValue = new NativeArray<DataValue>(issueActions.ToArray(), Allocator.Temp))
                        {
                            em.AddBuffer<DataValue>(targetSubPolicyEntity).AddRange(tempDataValue);
                        }

                        issueActions.Clear();
                    }

                    em.SetComponentData(targetPolicyGroupEntity, currentPolicyGroup);
                    em.AddBuffer<EntityWrapper>(targetPolicyGroupEntity).AddRange(subCollector);
                    subCollector.Clear();
                }
            }
        }
    }
}