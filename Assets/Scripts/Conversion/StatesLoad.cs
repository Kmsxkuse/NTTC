using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public class StatesLoad : MonoBehaviour
    {
        public static (IEnumerable<string>, BlobAssetReference<StateToProv>, BlobAssetReference<ProvToState>)
            Main(IReadOnlyDictionary<int, int> idIndex, NativeHashMap<int, int> stateLookup,
                IReadOnlyDictionary<int, Entity> provEntityLookup)
        {
            var slicedText = File.ReadLines(Path.Combine(Application.streamingAssetsPath, "map", "region.txt"),
                Encoding.GetEncoding(1252));

            var stateIdNames = new List<string>();
            var stateLookupArray = new List<List<int>>();
            
            var defInv = new NativeArray<Inventory>(LoadChain.GoodNum, Allocator.Temp);

            foreach (var rawLine in slicedText)
            {
                if (CommentDetector(rawLine, out var line))
                    continue;

                var stateProvinces = new List<int>();

                var choppedLine = line.Split(new[] {'{', '}'}, StringSplitOptions.RemoveEmptyEntries);
                // 0. StateId = | 1. Prov 1 Prov 2 Prov 3... | 2. #StateName

                var innerChopped = choppedLine[1].Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var colorNum in innerChopped)
                {
                    if (!int.TryParse(colorNum, out var lookupNum))
                        throw new Exception("Invalid province ID. Must be int number.");

                    var provId = idIndex[lookupNum];
                    stateLookup.Add(provId, stateIdNames.Count);
                    stateProvinces.Add(provId);
                }

                stateLookupArray.Add(stateProvinces);

                stateIdNames.Add(Regex.Match(choppedLine[0], @"^.+?(?=\=)").Value.Trim());
            }

            // Creating state to prov blob lookup nested array
            BlobAssetReference<StateToProv> stateToProvReference;
            using (var stateToProv = new BlobBuilder(Allocator.Temp))
            {
                ref var lookupStruct = ref stateToProv.ConstructRoot<StateToProv>();

                var stateArray = stateToProv.Allocate(ref lookupStruct.Lookup, stateLookupArray.Count);
                for (var state = 0; state < stateLookupArray.Count; state++)
                {
                    var provArray = stateToProv.Allocate(ref stateArray[state], stateLookupArray[state].Count);
                    for (var i = 0; i < stateLookupArray[state].Count; i++)
                        provArray[i] = provEntityLookup[stateLookupArray[state][i]];
                }

                stateToProvReference = stateToProv.CreateBlobAssetReference<StateToProv>(Allocator.Persistent);
            }

            // Creating state entities
            var provToStateArray = new Entity[idIndex.Count];
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            for (var stateIndex = 0; stateIndex < stateIdNames.Count; stateIndex++)
            {
                var stateEntity = em.CreateEntity(typeof(State), typeof(FactoryWrapper), typeof(Inventory));
                em.SetComponentData(stateEntity, new State(stateIndex, stateToProvReference));
                em.GetBuffer<Inventory>(stateEntity).AddRange(defInv);
                em.SetName(stateEntity, "State: " + stateIdNames[stateIndex]); //DEBUG
                
                foreach (var provId in stateLookupArray[stateIndex])
                    provToStateArray[provId] = stateEntity;
            }

            defInv.Dispose();

            // Creating prov to state blob lookup nested array
            BlobAssetReference<ProvToState> provToStateReference;
            using (var provToState = new BlobBuilder(Allocator.Temp))
            {
                ref var lookupStruct = ref provToState.ConstructRoot<ProvToState>();

                var provArray = provToState.Allocate(ref lookupStruct.Lookup, idIndex.Count);
                SetAllocation(ref provArray, provToStateArray);

                provToStateReference = provToState.CreateBlobAssetReference<ProvToState>(Allocator.Persistent);

                void SetAllocation<T>(ref BlobBuilderArray<T> builderArray, IReadOnlyList<T> managedArray) where T : struct
                {
                    for (var i = 0; i < managedArray.Count; i++)
                        builderArray[i] = managedArray[i];
                }
            }

            // Updating province entities with prov to state blob reference.
            foreach (var provEntity in provEntityLookup.Values)
            {
                var provinceData = em.GetComponentData<Province>(provEntity);
                provinceData.ProvToState = provToStateReference;
                em.SetComponentData(provEntity, provinceData);
            }

            return (stateIdNames, stateToProvReference, provToStateReference);

            bool CommentDetector(string line, out string sliced)
            {
                // Comment Detector. Will also lowercase everything. Throwing away comments.
                sliced = line.ToLowerInvariant().Split(new[] {"#"}, StringSplitOptions.None)[0].Trim();
                return sliced.Length == 0;
            }
        }
    }
}