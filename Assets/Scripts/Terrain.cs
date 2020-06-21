using UnityEngine;

public struct Water
{
    public readonly string Name;
    public readonly Color32 Color;
    public float MovementModifier;

    public Water(string name, Color32 color, float movementModifier)
    {
        Name = name;
        Color = color;
        MovementModifier = movementModifier;
    }
}