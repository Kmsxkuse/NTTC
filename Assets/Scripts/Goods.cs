using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct Goods : IComponentData
{
    public Color Color;
    public float Cost;
    public NativeString32 Name;
}
