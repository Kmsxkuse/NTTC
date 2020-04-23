using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct PopWrapper : IBufferElementData
{
    public Entity Population;
    public static implicit operator PopWrapper(Entity e) => new PopWrapper {Population = e};
}

public struct Population : IComponentData
{
    // Top down connection between workplace and employees.
    public int Employment, Religion, Culture, Quantity;
    public float Wealth, Satisfaction;
    public Entity MarketIdentity;
}
