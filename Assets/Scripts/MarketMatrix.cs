using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

// ReSharper disable ConvertToUsingDeclaration

public struct MarketMatrix
{
    // Blob asset. Readonly past initialization.
    public BlobArray<float> Deltas;
    public BlobArray<int> Priority; // Used for pop. Lower is more important.
}

public struct Inventory : IBufferElementData
{
    public float Value;

    public Inventory(float value)
    {
        Value = value;
    }
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

public readonly struct BidKey : IEquatable<BidKey>
{
    private readonly Entity _keyEntityLocation; // Not to be confused with location component.
    private readonly int _good;
    private readonly Transaction _type;

    public BidKey(Entity keyEntityLocation, int good, float quantity)
    {
        // Bid Creation
        _keyEntityLocation = keyEntityLocation;
        _good = good;
        _type = quantity < 0 ? Transaction.Buy : Transaction.Sell;
    }

    public BidKey(Entity keyEntityLocation, int good, Transaction type)
    {
        // Bid Search
        _keyEntityLocation = keyEntityLocation;
        _good = good;
        _type = type;
    }

    public bool Equals(BidKey other)
    {
        return _keyEntityLocation.Equals(other._keyEntityLocation) && _good == other._good && _type == other._type;
    }

    public override bool Equals(object obj)
    {
        return obj is BidKey other && Equals(other);
    }

    // Hash code taken from Vector2Int
    public override int GetHashCode()
    {
        // Level is not included as multiple levels are not intended to be together.
        return _keyEntityLocation.GetHashCode() ^ (_good << 2) * (int) _type;
    }

    public enum Transaction
    {
        // Int values very important in determining direction of price change post country market.
        Buy = 1,
        Sell = -1
    }
}

public struct BidOffers
{
    public Entity Source;
    public float Quantity; // Negative is buying. Positive is selling.
}

public struct BidRecord
{
    public int Good;
    public float Quantity;
}

public struct MarketJson
{
    // Demand is now automatically calculated to pop employment * -delta with floor cap being 0;
    public float[] Deltas;

    public int MaximumEmployment;

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] [CanBeNull]
    public int[] Priority;
}

public static class MarketConvert
{
    public static BlobAssetReference<MarketMatrix> Main(MarketJson marketJson)
    {
        // Conversion between MarketJson and MarketMatrix

        var arrayLengths = marketJson.Deltas.Length;

        var priorityExist = marketJson.Priority != null;
        if (priorityExist && arrayLengths != marketJson.Priority.Length)
            throw new Exception("Invalid market matrix. Priority array length not " +
                                "corresponding to Deltas and Demand length.");

        using (var marketIdentity = new BlobBuilder(Allocator.Temp))
        {
            ref var marketMatrix = ref marketIdentity.ConstructRoot<MarketMatrix>();

            // Can not merge. Blame Unity.
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