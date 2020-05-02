using Unity.Entities;

public struct Wallet : IComponentData
{
    public float Wealth;
}

public struct Loans : IBufferElementData
{
    public float Amount, Interest;
}