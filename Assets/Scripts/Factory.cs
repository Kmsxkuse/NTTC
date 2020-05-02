using Unity.Entities;

public struct Factory : IComponentData
{
    public int MaximumEmployment;
}

public struct Employee : IBufferElementData
{
    public Entity Pop;

    public static implicit operator Employee(Entity e)
    {
        return new Employee {Pop = e};
    }
}

public struct FactoryLink : IBufferElementData
{
    public Entity Factory;

    public static implicit operator FactoryLink(Entity e)
    {
        return new FactoryLink {Factory = e};
    }
}