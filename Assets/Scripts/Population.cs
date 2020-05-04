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
    public int Quantity;
    //public float Satisfaction;
}

public struct Employer : IComponentData
{
    public Entity Factory;

    public static implicit operator Employer(Entity e)
    {
        return new Employer {Factory = e};
    }
}

public struct Ethnicity : IComponentData
{
    public int Culture, Religion;
}

public struct Location : IComponentData
{
    public Entity Province, State;
}