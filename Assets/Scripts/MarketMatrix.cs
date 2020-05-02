using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;

public struct MarketMatrix
{
    // Blob asset. Readonly past initialization.
    public BlobArray<float> Deltas, Demand;
    public BlobArray<int> Priority; // Used for pop. Lower is more important.
}

public struct Inventory : IBufferElementData
{
    public float Value;
}

public struct Identity : IComponentData
{
    // Factory market agent.
    public BlobAssetReference<MarketMatrix> MarketIdentity;

    public static implicit operator Identity(BlobAssetReference<MarketMatrix> b)
    {
        return new Identity {MarketIdentity = b};
    }
}

public struct MarketJson
{
    public float[] Deltas, Demand;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] [CanBeNull]
    public int[] Priority;
}

public static class MarketConvert
{
    public static BlobAssetReference<MarketMatrix> Main(MarketJson marketJson)
    {
        // Conversion between MarketJson and MarketMatrix

        var arrayLengths = marketJson.Deltas.Length;

        if (arrayLengths != marketJson.Demand.Length)
            throw new Exception("Invalid market matrix. Delta array length not corresponding to Demand length.");

        var priorityExist = marketJson.Priority != null;
        if (priorityExist && arrayLengths != marketJson.Priority.Length)
            throw new Exception("Invalid market matrix. Priority array length not " +
                                "corresponding to Deltas and Demand length.");

        using (var marketIdentity = new BlobBuilder(Allocator.Temp))
        {
            ref var marketMatrix = ref marketIdentity.ConstructRoot<MarketMatrix>();

            // Can not merge. Blame Unity.
            var demand = marketIdentity.Allocate(ref marketMatrix.Demand, arrayLengths);
            SetAllocation(ref demand, marketJson.Demand);

            var deltas = marketIdentity.Allocate(ref marketMatrix.Deltas, arrayLengths);
            SetAllocation(ref deltas, marketJson.Deltas);

            var priority = marketIdentity.Allocate(ref marketMatrix.Priority, arrayLengths);
            SetAllocation(ref priority, priorityExist ? marketJson.Priority : new int[arrayLengths]);

            return marketIdentity.CreateBlobAssetReference<MarketMatrix>(Allocator.Persistent);

            void SetAllocation<T>(ref BlobBuilderArray<T> builderArray, IReadOnlyList<T> managedArray) where T : struct
            {
                for (var i = 0; i < managedArray.Count; i++)
                    builderArray[i] = managedArray[i];
            }
        }
    }
}