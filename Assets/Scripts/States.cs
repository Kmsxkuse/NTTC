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
    public Entity Owner;
    public BlobAssetReference<StateToProv> StateToProv;

    public State(int index, BlobAssetReference<StateToProv> stateToProv)
    {
        Index = index;
        Owner = Entity.Null; // Determined on first tick of game play loop.
        StateToProv = stateToProv;
    }
}

public struct PartialOwnership : IComponentData
{
    // For multiple countries owning one state.
    // Empty tag. Possible dynamic buffer of countries which own this state attached?
}

public struct Inhabited : IComponentData
{
    // Tag for regions that have at least one owned province.
}