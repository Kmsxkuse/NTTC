using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct States : ISharedComponentData
{
    public NativeString32 Name;
}
