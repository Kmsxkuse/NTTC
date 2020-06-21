using Unity.Entities;

public struct Factory : IComponentData
{
    public int MaximumEmployment, TotalEmployed;

    public Factory(int maximumEmployment, int totalEmployed)
    {
        MaximumEmployment = maximumEmployment;
        TotalEmployed = totalEmployed;
    }
}

public struct FactoryWrapper : IBufferElementData
{
    public Entity Factory;

    public FactoryWrapper(Entity factory)
    {
        Factory = factory;
    }
}

public struct RgoGood : IComponentData
{
    // Tag for factories that are RGO good producers.
}