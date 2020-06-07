using Unity.Entities;

public struct Factory : IComponentData
{
    public int MaximumEmployment, TotalEmployed;
}

public struct FactoryWrapper : IBufferElementData
{
    private Entity _factory;

    public static implicit operator FactoryWrapper(Entity e)
    {
        return new FactoryWrapper {_factory = e};
    }
    public static implicit operator Entity(FactoryWrapper factoryWrapper)
    {
        return factoryWrapper._factory;
    }
}

public struct RgoGood : IComponentData
{
    // Tag for factories that are RGO good producers.
}