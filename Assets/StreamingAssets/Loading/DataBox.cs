using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    [InternalBufferCapacity(0)]
    public struct EntityWrapper : IBufferElementData
    {
        public readonly Entity Entity;

        private EntityWrapper(Entity entity)
        {
            Entity = entity;
        }

        public static implicit operator EntityWrapper(Entity e)
        {
            return new EntityWrapper(e);
        }

        public static implicit operator Entity(EntityWrapper e)
        {
            return e.Entity;
        }
    }

    [InternalBufferCapacity(0)] // Automatically sized. Kills performance but whatever.
    public struct DataValue : IBufferElementData
    {
        public readonly int Type;
        public readonly float Value;

        public DataValue(int type, float value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"Type: {(LoadVariables) Type}. Value: {Value}.";
        }
    }

    [InternalBufferCapacity(0)] // Automatically sized. Kills performance but whatever.
    public struct DataRange : IBufferElementData
    {
        public readonly int Type, Start, End;

        public DataRange(int type, int start, int end)
        {
            Type = type;
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            return $"Type: {(LoadVariables) Type}. Range: {Start} - {End}.";
        }
    }

    [InternalBufferCapacity(0)]
    public struct DataGood : IBufferElementData
    {
        public readonly int Type;
        public readonly float Amount;

        public DataGood(int type, float amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    [InternalBufferCapacity(0)]
    public struct DataInt : IBufferElementData
    {
        private readonly int _value;

        private DataInt(int value)
        {
            _value = value;
        }

        public static implicit operator DataInt(int i)
        {
            return new DataInt(i);
        }

        public static implicit operator int(DataInt i)
        {
            return i._value;
        }
    }

    public struct NewDataBox : ISharedComponentData, IEquatable<NewDataBox>
    {
        public NativeHashMap<int, int> IdIndex;
        public NativeArray<Color32> IdMap;

        public bool Equals(NewDataBox other)
        {
            return IdIndex.Equals(other.IdIndex);
        }

        public override bool Equals(object obj)
        {
            return obj is NewDataBox other && Equals(other);
        }

        public override int GetHashCode()
        {
            return IdIndex.GetHashCode();
        }

        public static bool operator ==(NewDataBox left, NewDataBox right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NewDataBox left, NewDataBox right)
        {
            return !left.Equals(right);
        }
    }

    public struct DataBox : ISharedComponentData, IEquatable<DataBox>
    {
        // All global variables. Possibly split to multiple boxes?
        public NativeArray<Color32> IdMap;
        public NativeArray<ProvinceEntity> Provinces;
        public NativeArray<int> BorderEnds, BorderIndices;

        // Population info arrays
        public NativeArray<ProvPopInfo> PopList;
        public NativeMultiHashMap<int, int> PopLookup;

        public bool Equals(DataBox other)
        {
            return IdMap.Equals(other.IdMap) && Provinces.Equals(other.Provinces);
        }

        public override bool Equals(object obj)
        {
            return obj is DataBox other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IdMap.GetHashCode();
                hashCode = (hashCode * 397) ^ Provinces.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(DataBox left, DataBox right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DataBox left, DataBox right)
        {
            return !left.Equals(right);
        }
    }

    public struct ProvinceTable : ISharedComponentData, IEquatable<ProvinceTable>
    {
        // Color array
        public float4[] ProvLookup;

        public bool Equals(ProvinceTable other)
        {
            return Equals(ProvLookup, other.ProvLookup);
        }

        public override bool Equals(object obj)
        {
            return obj is ProvinceTable other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ProvLookup != null ? ProvLookup.GetHashCode() : 0;
        }

        public static bool operator ==(ProvinceTable left, ProvinceTable right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProvinceTable left, ProvinceTable right)
        {
            return !left.Equals(right);
        }
    }

    public struct EventBox : ISharedComponentData, IEquatable<EventBox>
    {
        public NativeArray<int> EventModifiers;
        public NativeArray<int3> EventModifierRanges;
        public NativeArray<float2> EventModifierActions;

        public bool Equals(EventBox other)
        {
            return EventModifiers.Equals(other.EventModifiers);
        }

        public override bool Equals(object obj)
        {
            return obj is EventBox other && Equals(other);
        }

        public override int GetHashCode()
        {
            return EventModifiers.GetHashCode();
        }

        public static bool operator ==(EventBox left, EventBox right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EventBox left, EventBox right)
        {
            return !left.Equals(right);
        }
    }

    public struct PoliticalBox : ISharedComponentData, IEquatable<PoliticalBox>
    {
        public NativeArray<int>
            IdeologyGroups, CountryIndices, IssueGroups, CountryPolicies; // CI: Political parties and such.

        public NativeArray<int3> IdeologyRanges, CountryRanges, IssueRanges, CountryHistoryRanges;
        public NativeArray<float2> IdeologyActions, CountryActions, IssueActions, CountryHistoryActions;

        public NativeArray<int> GovernmentIdeologies;

        public NativeArray<Color32> CountryColors, CountryGovernColors;

        public bool Equals(PoliticalBox other)
        {
            return CountryGovernColors.Equals(other.CountryGovernColors);
        }

        public override bool Equals(object obj)
        {
            return obj is PoliticalBox other && Equals(other);
        }

        public override int GetHashCode()
        {
            return CountryGovernColors.GetHashCode();
        }

        public static bool operator ==(PoliticalBox left, PoliticalBox right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PoliticalBox left, PoliticalBox right)
        {
            return !left.Equals(right);
        }
    }

    public struct TechBox : ISharedComponentData, IEquatable<TechBox>
    {
        public NativeArray<TechInfo> Technologies;
        public NativeArray<int> Inventions;

        public NativeArray<int> TechInventions;

        public NativeArray<int2> FolderRanges,
            SchoolRanges;

        public NativeArray<int3> TechRanges, InventionRanges;

        public NativeArray<float2> SchoolActions,
            TechActions,
            InventionActions;

        public bool Equals(TechBox other)
        {
            return Technologies.Equals(other.Technologies);
        }

        public override bool Equals(object obj)
        {
            return obj is TechBox other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Technologies.GetHashCode();
        }

        public static bool operator ==(TechBox left, TechBox right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TechBox left, TechBox right)
        {
            return !left.Equals(right);
        }
    }

    public struct StringBox : ISharedComponentData, IEquatable<StringBox>
    {
        // Dictionary because states have their unique ID numbers as well.
        public Dictionary<string, int> CountryFlags, GlobalFlags;

        private List<string> _stateNames,
            _continentNames,
            _popTypeNames,
            _goodsNames,
            _countryTags,
            _religionNames,
            _cultureNames,
            _cultureGroupNames,
            _ideologyNames,
            _buildingNames,
            _policyGroupNames,
            _subPolicyNames,
            _folderNames,
            _schoolNames,
            _techNames,
            _inventionNames,
            _unitNames,
            _governmentNames,
            _crimeNames,
            _eventModifierNames,
            _nationalValueNames,
            _terrainNames;

        public List<string> ProvinceNames { get; set; }

        public List<string> StateNames
        {
            get => _stateNames;
            set
            {
                LookupDictionaries.States = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.States.Add(value[i], i);

                _stateNames = value;
            }
        }

        public List<string> ContinentNames
        {
            get => _continentNames;
            set
            {
                LookupDictionaries.Continents = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Continents.Add(value[i], i);

                _continentNames = value;
            }
        }

        public List<string> PopTypeNames
        {
            get => _popTypeNames;
            set
            {
                LookupDictionaries.PopTypes = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.PopTypes.Add(value[i], i);

                _popTypeNames = value;
            }
        }

        public List<string> GoodsNames
        {
            get => _goodsNames;
            set
            {
                LookupDictionaries.Goods = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Goods.Add(value[i], i);

                _goodsNames = value;
            }
        }

        public List<string> GoodsCategory { get; set; }

        public List<string> CountryTags
        {
            get => _countryTags;
            set
            {
                LookupDictionaries.CountryTags = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.CountryTags.Add(value[i], i);

                _countryTags = value;
            }
        }

        public List<string> CountryNames { get; set; }

        public List<string> ReligionNames
        {
            get => _religionNames;
            set
            {
                LookupDictionaries.Religions = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Religions.Add(value[i], i);

                _religionNames = value;
            }
        }

        public List<string> ReligionGroupNames { get; set; }

        public List<string> CultureNames
        {
            get => _cultureNames;
            set
            {
                LookupDictionaries.Cultures = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Cultures.Add(value[i], i);

                _cultureNames = value;
            }
        }

        public List<string> CultureGroupNames
        {
            get => _cultureGroupNames;
            set
            {
                LookupDictionaries.CultureGroups = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.CultureGroups.Add(value[i], i);

                _cultureGroupNames = value;
            }
        }

        public List<string> IdeologyNames
        {
            get => _ideologyNames;
            set
            {
                LookupDictionaries.Ideologies = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Ideologies.Add(value[i], i);

                _ideologyNames = value;
            }
        }

        public List<string> IdeologyGroupNames { get; set; }

        public List<string> BuildingNames
        {
            get => _buildingNames;
            set
            {
                LookupDictionaries.Buildings = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Buildings.Add(value[i], i);

                _buildingNames = value;
            }
        }

        public List<string> PolicyGroupNames
        {
            get => _policyGroupNames;
            set
            {
                LookupDictionaries.PolicyGroups = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.PolicyGroups.Add(value[i], i);

                _policyGroupNames = value;
            }
        }

        public List<string> SubPolicyNames
        {
            get => _subPolicyNames;
            set
            {
                LookupDictionaries.SubPolicies = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.SubPolicies.Add(value[i], i);

                _subPolicyNames = value;
            }
        }

        public List<string> FolderNames
        {
            get => _folderNames;
            set
            {
                LookupDictionaries.FolderResearchBonuses = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.FolderResearchBonuses.Add(value[i] + "_research_bonus", i);

                _folderNames = value;
            }
        }

        public List<string> AreaNames { get; set; }

        public List<string> SchoolNames
        {
            get => _schoolNames;
            set
            {
                LookupDictionaries.Schools = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Schools.Add(value[i], i);

                _schoolNames = value;
            }
        }

        public List<string> TechNames
        {
            get => _techNames;
            set
            {
                LookupDictionaries.Techs = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Techs.Add(value[i], i);

                _techNames = value;
            }
        }

        public List<string> InventionNames
        {
            get => _inventionNames;
            set
            {
                LookupDictionaries.Inventions = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Inventions.Add(value[i], i);

                _inventionNames = value;
            }
        }

        public List<string> UnitNames
        {
            get => _unitNames;
            set
            {
                LookupDictionaries.Units = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Units.Add(value[i], i);

                _unitNames = value;
            }
        }

        public List<string> GovernmentNames
        {
            get => _governmentNames;
            set
            {
                LookupDictionaries.Governments = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Governments.Add(value[i], i);

                _governmentNames = value;
            }
        }

        public List<string> EventModifierNames
        {
            get => _eventModifierNames;
            set
            {
                LookupDictionaries.EventModifiers = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                {
                    // Overwriting duplicates with ones further below
                    LookupDictionaries.EventModifiers.Remove(value[i]);
                    LookupDictionaries.EventModifiers.Add(value[i], i);
                }

                _eventModifierNames = value;
            }
        }

        public List<string> CountryPartyNames { get; set; }

        public List<string> CrimeNames
        {
            get => _crimeNames;
            set
            {
                LookupDictionaries.Crimes = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Crimes.Add(value[i], i);

                _crimeNames = value;
            }
        }

        public List<string> NationalValueNames
        {
            get => _nationalValueNames;
            set
            {
                LookupDictionaries.NationalValues = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.NationalValues.Add(value[i], i);

                _nationalValueNames = value;
            }
        }

        public List<string> TerrainNames
        {
            get => _terrainNames;
            set
            {
                LookupDictionaries.Terrain = new Dictionary<string, int>(value.Count);
                for (var i = 0; i < value.Count; i++)
                    LookupDictionaries.Terrain.Add(value[i], i);

                _terrainNames = value;
            }
        }

        public bool Equals(StringBox other)
        {
            return Equals(StateNames, other.StateNames);
        }

        public override bool Equals(object obj)
        {
            return obj is StringBox other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = StateNames != null ? StateNames.GetHashCode() : 0;
            return hashCode;
        }

        public static bool operator ==(StringBox left, StringBox right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringBox left, StringBox right)
        {
            return !left.Equals(right);
        }
    }

    public struct BlittableBool : IEquatable<BlittableBool>
    {
        private readonly byte _value;

        private BlittableBool(bool value)
        {
            _value = Convert.ToByte(value);
        }

        public static implicit operator bool(BlittableBool blittableBool)
        {
            return blittableBool._value != 0;
        }

        public static implicit operator BlittableBool(bool value)
        {
            return new BlittableBool(value);
        }

        public bool Equals(BlittableBool other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            return obj is BlittableBool blittableBool && Equals(blittableBool);
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public static bool operator ==(BlittableBool left, BlittableBool right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlittableBool left, BlittableBool right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ((bool) this).ToString();
        }
    }
}