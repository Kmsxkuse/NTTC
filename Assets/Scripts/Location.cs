using Unity.Entities;

public readonly struct Location : IComponentData
{
    public readonly Entity Province, State;

    public Location(Entity province, Entity state)
    {
        Province = province;
        State = state;
    }
}