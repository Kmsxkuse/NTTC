using Unity.Entities;

public struct ProvToState
{
    // Index is province ASSIGNED ID.
    // Entities are states.
    // Created im RegionsLoad.
    public BlobArray<Entity> Lookup;
}

public struct Province : IComponentData
{
    public int Index, LifeRating;
    public Entity Owner, Controller;

    public BlobAssetReference<ProvToState> ProvToState;
    //public NativeString32 Name;
}

public struct ProvinceRgo : IComponentData
{
    public Entity TradeGood;

    public ProvinceRgo(Entity tradeGood)
    {
        TradeGood = tradeGood;
    }
}

public struct Cores : IBufferElementData
{
    public Entity Nation;

    public static implicit operator Cores(Entity e)
    {
        return new Cores {Nation = e};
    }
}

public struct Infrastructure : IBufferElementData
{
    public int Level;
}

public struct OceanProvince : IComponentData
{
    // Tag for oceans.
}

public struct UncolonizedProvince : IComponentData
{
    // Tag for uncolonized.
}

/* Not used so far.
public struct ProvinceNames
{
    // Index is province ASSIGNED ID.
    public BlobArray<BlobString> Names;
}
*/