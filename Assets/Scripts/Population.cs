using Unity.Entities;

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
    public int Employment, Quantity;
    public float Satisfaction;
}

public struct Ethnicity : IComponentData
{
    public int Culture, Religion;
}