using Unity.Entities;
using UnityEngine;

public struct Country : IComponentData
{
    public Color Color;
}

public struct OceanTag : IComponentData
{
    // Empty tag. Unique for Ocean country.
}

public struct UncolonizedTag : IComponentData
{
    // Empty tag. Unique for Uncolonized country.
}