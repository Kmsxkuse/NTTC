using Unity.Entities;
using UnityEngine;

public struct Country : IComponentData
{
    public Color Color;
}

public struct OceanCountry : IComponentData
{
    // Empty tag. Unique for Ocean country.
}

public struct UncolonizedCountry : IComponentData
{
    // Empty tag. Unique for Uncolonized country.
}