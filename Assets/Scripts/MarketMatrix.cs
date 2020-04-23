using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct MarketMatrix : IBufferElementData
{
    // Done in a per good format
    public float Demand, Delta;
    public int Priority; // Used for population multiple levels of satisfaction. Lowest is highest priority.
}

public struct Inventory : IBufferElementData
{
    // Attached to agents on a per good basis.
    public float Value;
}

public struct MarketIdentity : IComponentData
{
    // Tag for Market Matrices
}