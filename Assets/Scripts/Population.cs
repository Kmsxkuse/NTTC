using System;
using Unity.Entities;
using UnityEngine;

public struct PopWrapper : IBufferElementData
{
    public Entity Population;

    public static implicit operator PopWrapper(Entity e)
    {
        return new PopWrapper {Population = e};
    }
}

public struct Population : IComponentData
{
    // Top down connection between workplace and employees.
    public int Quantity, Employed;
    //public float Satisfaction;
}

public struct Ethnicity : IComponentData
{
    public int Culture, Religion;
}

public struct Location : IComponentData
{
    public Entity Province, State;
}

public struct PopEmployment : IBufferElementData
{
    public Entity Factory;
    public int Employed;
}