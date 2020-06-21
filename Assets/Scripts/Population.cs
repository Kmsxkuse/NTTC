using Unity.Entities;

public struct Population : IBufferElementData
{
    // Dynamic Buffer under Province Entity.
    public int Quantity, Employed;
    public float Wealth;

    public Population(int quantity, int employed, float wealth)
    {
        Quantity = quantity;
        Employed = employed;
        Wealth = wealth;
    }
}

public struct FactoryEmployment : IBufferElementData
{
    // Dynamic Buffer under Factory Entity;
    public Entity Province;
    public int PopIndex, Quantity;

    public FactoryEmployment(Entity province, int popIndex, int quantity)
    {
        Province = province;
        PopIndex = popIndex;
        Quantity = quantity;
    }
}