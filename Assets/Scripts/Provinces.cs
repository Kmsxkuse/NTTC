using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct Province : IComponentData
{
    public int Index, TradeGoods, LifeRating;

    public Entity Owner, Controller;
    //public NativeString32 Name;
}

public struct Cores : IBufferElementData
{
    public Entity Nation;
    public static implicit operator Cores(Entity e) => new Cores {Nation = e};
}

public struct Infrastructure : IBufferElementData
{
    public int Level;
}

