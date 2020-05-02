using Unity.Entities;

public struct StateToProv
{
    // Entities are provinces.
    // Created in RegionsLoad.
    public BlobArray<BlobArray<Entity>> Lookup;
}

public struct State : IComponentData
{
    // Created in RegionsLoad.
    public int Index;
    public BlobAssetReference<StateToProv> StateToProv;

    public State(int index, BlobAssetReference<StateToProv> stateToProv)
    {
        Index = index;
        StateToProv = stateToProv;
    }
}

public struct PartialOwnership : IComponentData
{
    // For multiple countries owning one state.
    // Empty tag. Possible dynamic buffer of countries which own this state attached?
}