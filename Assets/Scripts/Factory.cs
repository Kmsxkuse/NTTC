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

public struct FactoryWrapper : IBufferElementData
{
    public Entity Factory;

    public static implicit operator FactoryWrapper(Entity e)
    {
        return new FactoryWrapper {Factory = e};
    }
    public static implicit operator Entity(FactoryWrapper factoryWrapper)
    {
        return factoryWrapper.Factory;
    }
}

public struct RgoGood : IComponentData
{
    // Tag for factories that are RGO good producers.
}