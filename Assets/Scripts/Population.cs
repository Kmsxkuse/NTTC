using System;
using Unity.Entities;
using UnityEngine;

public struct Population : IComponentData
{
    // Bottom up connection. Linked by PopEmployment buffer.
    public int Quantity, Employed;
    //public float Satisfaction;
}

public struct Ethnicity : IComponentData
{
    public int Culture, Religion;
}

public readonly struct Location : IComponentData
{
    public readonly Entity Province, State;
    
    public Location(Entity province, Entity state)
    {
        Province = province;
        State = state;
    }
}

public struct PopEmployment : IBufferElementData
{
    public Entity Factory;
    public int Employed;
}