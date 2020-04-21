using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct CountryCollection : IComponentData
    {
    }

    public struct CountryPopulationComponent : IComponentData
    {
        // Index located in countriesLoad.CountryEntity.
        // Country = Player/AI
        public Entity Religion, NationalValue, PrimaryCulture;
        public float Consciousness, NonStateConsciousness;
    }

    public struct CountryPoliticsComponent : IComponentData
    {
        public int Capital, Prestige;
        public Entity RulingParty, Government;
    }

    public struct CountryTechnologyComponent : IComponentData
    {
        public Entity CurrentResearch;
        public float Plurality, Literacy, NonStateCultureLiteracy;
        public bool Civilized;
    }

    // And a lot more.

    
    public struct CountryUpperHouse : IBufferElementData, IEquatable<CountryUpperHouse>
    {
        public readonly Entity Party;
        public float Percentage;

        public CountryUpperHouse(Entity party, float percentage)
        {
            Party = party;
            Percentage = percentage;
        }

        public bool Equals(CountryUpperHouse other)
        {
            return Party == other.Party;
        }

        public override bool Equals(object obj)
        {
            return obj is CountryUpperHouse other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Party.GetHashCode();
        }

        public static bool operator ==(CountryUpperHouse left, CountryUpperHouse right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CountryUpperHouse left, CountryUpperHouse right)
        {
            return !left.Equals(right);
        }
    }

    
    public struct CountryPolicies : IBufferElementData
    {
        public Entity SubPolicy;

        private CountryPolicies(Entity subPolicy)
        {
            SubPolicy = subPolicy;
        }

        public static implicit operator CountryPolicies(EntityWrapper e)
        {
            return new CountryPolicies(e);
        }

        public static implicit operator CountryPolicies(Entity e)
        {
            return new CountryPolicies(e);
        }

        public static implicit operator Entity(CountryPolicies p)
        {
            return p.SubPolicy;
        }
    }

     // Basically an entity wrapper
    public struct CountryTechnologies : IBufferElementData, IEquatable<CountryTechnologies>
    {
        public readonly Entity Technology;

        private CountryTechnologies(Entity technology)
        {
            Technology = technology;
        }

        public static implicit operator CountryTechnologies(EntityWrapper e)
        {
            return new CountryTechnologies(e);
        }

        public static implicit operator Entity(CountryTechnologies t)
        {
            return t.Technology;
        }

        public bool Equals(CountryTechnologies other)
        {
            return Technology == other.Technology;
        }

        public override bool Equals(object obj)
        {
            return obj is CountryTechnologies other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Technology.GetHashCode();
        }

        public static bool operator ==(CountryTechnologies left, CountryTechnologies right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CountryTechnologies left, CountryTechnologies right)
        {
            return !left.Equals(right);
        }
    }

    
    public struct CountryInventions : IBufferElementData, IEquatable<CountryInventions>
    {
        public readonly Entity Invention;
        public bool Active; // Inactive inventions have a possibility of becoming active.

        public CountryInventions(Entity invention, bool active)
        {
            Invention = invention;
            Active = active;
        }

        public bool Equals(CountryInventions other)
        {
            return Invention == other.Invention;
        }

        public override bool Equals(object obj)
        {
            return obj is CountryInventions other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Invention.GetHashCode();
        }

        public static bool operator ==(CountryInventions left, CountryInventions right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CountryInventions left, CountryInventions right)
        {
            return !left.Equals(right);
        }
    }

    
    public struct CountryFlags : IBufferElementData, IEquatable<CountryFlags>
    {
        public readonly int Flag;

        public CountryFlags(int flag)
        {
            Flag = flag;
        }

        public bool Equals(CountryFlags other)
        {
            return Flag == other.Flag;
        }

        public override bool Equals(object obj)
        {
            return obj is CountryFlags other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Flag;
        }

        public static bool operator ==(CountryFlags left, CountryFlags right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CountryFlags left, CountryFlags right)
        {
            return !left.Equals(right);
        }
    }

    
    public struct CountryCultures : IBufferElementData, IEquatable<CountryCultures>
    {
        public readonly Entity Culture;

        private CountryCultures(Entity culture)
        {
            Culture = culture;
        }

        public static implicit operator CountryCultures(EntityWrapper e)
        {
            return new CountryCultures(e);
        }

        public static implicit operator Entity(CountryCultures c)
        {
            return c.Culture;
        }

        public bool Equals(CountryCultures other)
        {
            return Culture == other.Culture;
        }

        public override bool Equals(object obj)
        {
            return obj is CountryCultures other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Culture.GetHashCode();
        }

        public static bool operator ==(CountryCultures left, CountryCultures right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CountryCultures left, CountryCultures right)
        {
            return !left.Equals(right);
        }
    }

    public static class CountryHistoryLoad
    {
        public static (Entity, string[]) Main(Entity technologies, Entity inventions,
            Entity cultures, Entity ideologies, Entity subPolicies, Entity governments,
            Entity nationalValues)
        {
            var allCountries = new NativeArray<EntityWrapper>(LookupDictionaries.CountryTags.Count, Allocator.Temp);
            var em = World.Active.EntityManager;
            var countryArchetype = em.CreateArchetype(typeof(CountryPopulationComponent),
                typeof(CountryPoliticsComponent), typeof(CountryTechnologyComponent));
            var rulingParties = new string[LookupDictionaries.CountryTags.Count];

            var countryPolicies =
                new NativeArray<CountryPolicies>(LookupDictionaries.PolicyGroups.Count, Allocator.Temp);
            using (var nationalValueList = em.GetBuffer<EntityWrapper>(nationalValues).ToNativeArray(Allocator.Temp))
            using (var governmentList = em.GetBuffer<EntityWrapper>(governments).ToNativeArray(Allocator.Temp))
            using (var subPolicyList = em.GetBuffer<EntityWrapper>(subPolicies).ToNativeArray(Allocator.Temp))
            using (var ideologyList = em.GetBuffer<EntityWrapper>(ideologies).ToNativeArray(Allocator.Temp))
            using (var inventionList = em.GetBuffer<EntityWrapper>(inventions).ToNativeArray(Allocator.Temp))
            using (var cultureList = em.GetBuffer<EntityWrapper>(cultures).ToNativeArray(Allocator.Temp))
            using (var technologyList = em.GetBuffer<EntityWrapper>(technologies).ToNativeArray(Allocator.Temp))
            using (var countryUpperHouse = new NativeList<CountryUpperHouse>(Allocator.Temp))
            using (var countryInventions = new NativeList<CountryInventions>(Allocator.Temp))
            using (var countryTechnologies = new NativeList<CountryTechnologies>(Allocator.Temp))
            using (var countryCultures = new NativeList<CountryCultures>(Allocator.Temp))
            {
                foreach (var countryFile in Directory.EnumerateFiles(
                    Path.Combine(Application.streamingAssetsPath, "History", "countries"), "*.txt"))
                {
                    var fileTree = new List<KeyValuePair<int, object>>();
                    var values = new List<string>();

                    FileUnpacker.ParseFile(countryFile, fileTree, values, CountryHistoryMagicOverride);

                    var countryTag = Regex.Match(Path.GetFileNameWithoutExtension(countryFile) ?? "", @"^.+?(?=\-)")
                        .Value
                        .Trim().ToLower();
                    if (!LookupDictionaries.CountryTags.TryGetValue(countryTag, out var countryIndex))
                        continue;
                    
                    var countryEntity = em.CreateEntity(countryArchetype);

                    var currentCountry = new CountryEntity {Index = countryIndex};
                    var currentPopulation = new CountryPopulationComponent();
                    var currentPolitics = new CountryPoliticsComponent();
                    var currentTechnology = new CountryTechnologyComponent();
                    // Resetting polices
                    for (var index = 0; index < countryPolicies.Length; index++)
                        countryPolicies[index] = Entity.Null;

                    foreach (var target in fileTree)
                    {
                        var targetStr = target.Key < (int) LoadVariables.BreakCore
                            ? values[(int) target.Value]
                            : string.Empty;

                        switch ((LoadVariables) target.Key)
                        {
                            case LoadVariables.Capital:
                                currentPolitics.Capital = int.Parse(targetStr);
                                break;
                            case LoadVariables.RulingParty:
                                rulingParties[countryIndex] = targetStr;
                                break;
                            case LoadVariables.UpperHouse:
                                foreach (var ideology in (List<KeyValuePair<int, object>>) target.Value)
                                {
                                    if (!float.TryParse(values[(int) ideology.Value], out var ideologyPercentage))
                                        throw new Exception("Country ideology parsing failed! " +
                                                            values[(int) ideology.Value]);

                                    if (ideologyPercentage > 0.01)
                                        countryUpperHouse.Add(
                                            new CountryUpperHouse(
                                                ideologyList[ideology.Key - (int) MagicUnifiedNumbers.Ideology],
                                                ideologyPercentage));
                                }

                                break;
                            case LoadVariables.Government:
                                if (!LookupDictionaries.Governments.TryGetValue(targetStr, out var governmentIndex))
                                    throw new Exception("Unknown government. " + targetStr);

                                currentPolitics.Government = governmentList[governmentIndex];
                                break;
                            case LoadVariables.Prestige:
                                currentPolitics.Prestige = int.Parse(targetStr);
                                break;
                            case LoadVariables.Civilized:
                                currentTechnology.Civilized = LoadMethods.YesNoConverter(targetStr);
                                break;
                            case LoadVariables.NonStateCultureLiteracy:
                                currentTechnology.NonStateCultureLiteracy = int.Parse(targetStr);
                                break;
                            case LoadVariables.Literacy:
                                currentTechnology.Literacy = int.Parse(targetStr);
                                break;
                            case LoadVariables.Plurality:
                                currentTechnology.Plurality = int.Parse(targetStr);
                                break;
                            case LoadVariables.Consciousness:
                                currentPopulation.Consciousness = int.Parse(targetStr);
                                break;
                            case LoadVariables.NonStateConsciousness:
                                currentPopulation.NonStateConsciousness = int.Parse(targetStr);
                                break;
                            case LoadVariables.PrimaryCulture:
                                if (!LookupDictionaries.Cultures.TryGetValue(targetStr, out var primaryCultureIndex))
                                    throw new Exception("Unknown primary culture. " + targetStr);

                                currentPopulation.PrimaryCulture = cultureList[primaryCultureIndex];
                                break;
                            case LoadVariables.NationalValue:
                                if (!LookupDictionaries.NationalValues.TryGetValue(targetStr, out var natValIndex))
                                    throw new Exception("Unknown national value. " + targetStr);

                                currentPopulation.NationalValue = nationalValueList[natValIndex];
                                break;
                            case LoadVariables.Culture:
                                if (!LookupDictionaries.Cultures.TryGetValue(targetStr, out var cultureIndex))
                                    throw new Exception("Unknown culture. " + targetStr);

                                countryCultures.Add(cultureList[cultureIndex]);
                                break;
                            case LoadVariables.ForeignInvestment:
                            case LoadVariables.Oob:
                            case LoadVariables.Decision:
                                // Skipping
                                break;
                            default:
                                switch ((MagicUnifiedNumbers) (target.Key / 10000 * 10000))
                                {
                                    case MagicUnifiedNumbers.Placeholder:
                                        break;
                                    case MagicUnifiedNumbers.Invention:
                                        countryInventions.Add(
                                            new CountryInventions(
                                                inventionList[target.Key - (int) MagicUnifiedNumbers.Invention],
                                                LoadMethods.YesNoConverter(values[(int) target.Value])));
                                        break;
                                    case MagicUnifiedNumbers.Technology:
                                        countryTechnologies.Add(technologyList[target.Key - (int) MagicUnifiedNumbers.Technology]);
                                        break;
                                    case MagicUnifiedNumbers.PolicyGroup:
                                        if (!LookupDictionaries.SubPolicies.TryGetValue(values[(int) target.Value],
                                            out var subPolicyIndex))
                                            throw new Exception("Unknown policy group. " + values[(int) target.Value]);

                                        countryPolicies[countryIndex * LookupDictionaries.PolicyGroups.Count
                                                        + (target.Key - (int) MagicUnifiedNumbers.PolicyGroup)]
                                            = subPolicyList[subPolicyIndex];
                                        break;
                                }    

                                Debug.LogWarning("Uncaught load variable on country history load " 
                                                 + (LoadVariables) target.Key);
                                break;
                        }
                    }

                    em.SetComponentData(countryEntity, currentCountry);
                    em.SetComponentData(countryEntity, currentPopulation);
                    em.SetComponentData(countryEntity, currentPolitics);
                    em.SetComponentData(countryEntity, currentTechnology);
                    
                    em.AddBuffer<CountryUpperHouse>(countryEntity).AddRange(countryUpperHouse);
                    countryUpperHouse.Clear();
                    em.AddBuffer<CountryTechnologies>(countryEntity).AddRange(countryTechnologies);
                    countryTechnologies.Clear();
                    em.AddBuffer<CountryInventions>(countryEntity).AddRange(countryInventions);
                    countryInventions.Clear();
                    em.AddBuffer<CountryCultures>(countryEntity).AddRange(countryCultures);
                    countryCultures.Clear();
                    em.AddBuffer<CountryPolicies>(countryEntity).AddRange(countryPolicies);

                    allCountries[countryIndex] = countryEntity;
                }
            }
            // Not in using statement as the values are mutable.
            countryPolicies.Dispose();

            var countryCollector = FileUnpacker.GetCollector<CountryCollection>(allCountries);
            allCountries.Dispose();

            return (countryCollector, rulingParties);

            int CountryHistoryMagicOverride(int parent, string target)
            {
                // Skipping other start dates
                return Regex.IsMatch(target, @"\d+")
                    ? (int) MagicUnifiedNumbers.Placeholder
                    : (int) MagicUnifiedNumbers.ContinueMagicNumbers;
            }
        }
    }
}