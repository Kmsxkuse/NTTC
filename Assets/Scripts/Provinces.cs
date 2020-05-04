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

    public Entity Owner, Controller, TradeGood;

    public BlobAssetReference<ProvToState> ProvToState;
    //public NativeString32 Name;
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

/* Not used so far.
public struct ProvinceNames
{
    // Index is province ASSIGNED ID.
    public BlobArray<BlobString> Names;
}
*/