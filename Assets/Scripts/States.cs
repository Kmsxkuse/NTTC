using System;
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

public struct StateWrapper : IBufferElementData
{
    private Entity _state;

    public static implicit operator StateWrapper(Entity e)
    {
        return new StateWrapper {_state = e};
    }

    public static implicit operator Entity(StateWrapper stateWrapper)
    {
        return stateWrapper._state;
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

public readonly struct StateGoodTraded : IEquatable<StateGoodTraded>
{
    // Key used for transferring number of goods traded in state to country for price calculations.

    private readonly Entity _country;
    private readonly int _goodIndex;

    public StateGoodTraded(Entity country, int goodIndex)
    {
        _country = country;
        _goodIndex = goodIndex;
    }

    public bool Equals(StateGoodTraded other)
    {
        return _country.Equals(other._country) && _goodIndex == other._goodIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is StateGoodTraded other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (_country.GetHashCode() * 397) ^ _goodIndex;
        }
    }

    public static bool operator ==(StateGoodTraded left, StateGoodTraded right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StateGoodTraded left, StateGoodTraded right)
    {
        return !left.Equals(right);
    }
}