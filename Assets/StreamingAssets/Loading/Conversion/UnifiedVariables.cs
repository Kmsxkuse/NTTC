﻿using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public enum MagicUnifiedNumbers
    {
        SkipMagicNumbers = -200,
        ContinueMagicNumbers = -100,
        Government = 10000, // Used for special national colors upon government change.
        Technology = 20000,
        Unit = 30000,
        TechSchool = 40000,
        Ideology = 50000,
        Goods = 60000,
        SubPolicy = 70000,
        Invention = 90000,
        PolicyGroup = 100000,
        IdeologyGroup = 110000,
        Province = 120000, // MAX 10000 PROVINCES! (Why would you need more?)
        Building = 130000,
        Countries = 140000,
        Probabilities = 150000,
        Religion = 160000,
        Placeholder = 170000,
        ResearchBonus = 180000, // [Folder name]_research_bonus
        PopType = 190000,
        PopSubPolicy = 200000, // Pop issue awareness
        State = 210000,
        Terrain = 220000
    }

    public enum CommandWords
    {
        This = -10000,
        From = -10001,
        Culture = -10002, // Blame ITAFlavor. Change_tag = culture.
        Owner = -10003
    }

    public struct LookupDictionaries
    {
        // Mirrors StringBox
        public static Dictionary<string, int>
            States = new Dictionary<string, int>(),
            Continents = new Dictionary<string, int>(),
            PopTypes = new Dictionary<string, int>(),
            Goods = new Dictionary<string, int>(),
            CountryTags = new Dictionary<string, int>(),
            Religions = new Dictionary<string, int>(),
            Cultures = new Dictionary<string, int>(),
            CultureGroups = new Dictionary<string, int>(),
            Ideologies = new Dictionary<string, int>(),
            Buildings = new Dictionary<string, int>(),
            PolicyGroups = new Dictionary<string, int>(),
            SubPolicies = new Dictionary<string, int>(),
            FolderResearchBonuses = new Dictionary<string, int>(),
            Schools = new Dictionary<string, int>(),
            Techs = new Dictionary<string, int>(),
            Inventions = new Dictionary<string, int>(),
            Units = new Dictionary<string, int>(),
            Governments = new Dictionary<string, int>(),
            Crimes = new Dictionary<string, int>(),
            EventModifiers = new Dictionary<string, int>(),
            NationalValues = new Dictionary<string, int>(),
            Terrain = new Dictionary<string, int>();

        public static void AssignDictionary(string type, IReadOnlyList<string> names)
        {
            // Finding the name of the variable passed into 'names' would be too painful of a reflection to do.
            // I guess I could pass in (nameof(names), names) in LoadMain but ehhhhhhh.
            switch (type)
            {
                case "TerrainNames":
                    AddData(ref Terrain);
                    return;
                case "ContinentNames":
                    AddData(ref Continents);
                    return;
                case "StateNames":
                    AddData(ref States);
                    return;
                case "PopTypeNames":
                    AddData(ref PopTypes);
                    return;
                case "GoodNames":
                    AddData(ref Goods);
                    return;
                case "CountryTags":
                    AddData(ref CountryTags);
                    return;
                case "ReligionNames":
                    AddData(ref Religions);
                    return;
                case "CultureNames":
                    AddData(ref Cultures);
                    return;
                case "CultureGroupNames":
                    AddData(ref CultureGroups);
                    return;
                default:
                    Debug.Log($"Unknown dictionary type {type}.");
                    return;
            }

            void AddData(ref Dictionary<string, int> targetDictionary)
            {
                for (var cursor = 0; cursor < names.Count; cursor++)
                    targetDictionary.Add(names[cursor], cursor);
            }
        }

        public static void ClearDictionaries()
        {
            States.Clear();
            Continents.Clear();
            PopTypes.Clear();
            Goods.Clear();
            CountryTags.Clear();
            Religions.Clear();
            Cultures.Clear();
            CultureGroups.Clear();
            Ideologies.Clear();
            Buildings.Clear();
            PolicyGroups.Clear();
            SubPolicies.Clear();
            Schools.Clear();
            Techs.Clear();
            Inventions.Clear();
            Units.Clear();
            Governments.Clear();
            Crimes.Clear();
            EventModifiers.Clear();
            NationalValues.Clear();
            Terrain.Clear();

            States = null;
            Continents = null;
            PopTypes = null;
            Goods = null;
            CountryTags = null;
            Religions = null;
            Cultures = null;
            CultureGroups = null;
            Ideologies = null;
            Buildings = null;
            PolicyGroups = null;
            SubPolicies = null;
            Schools = null;
            Techs = null;
            Inventions = null;
            Units = null;
            Governments = null;
            Crimes = null;
            EventModifiers = null;
            NationalValues = null;
            Terrain = null;
        }
    }

    public static class UnifiedVariables
    {
        //private static readonly Dictionary<string, int> CountryFlags = new Dictionary<string, int>();

        //private static readonly Dictionary<string, int> GlobalFlags = new Dictionary<string, int>();

        //public static NativeHashMap<int, int> IdIndex;

        /*
        public static void DisposeDictionaries(ref StringBox stringBox)
        {
            LookupDictionaries.ClearDictionaries();

            stringBox.CountryFlags = CountryFlags;
            stringBox.GlobalFlags = GlobalFlags;
        }
        */

        public static int MagicNumbers(int parent, IList<string> strPair, Func<int, string, int> magicOverrides)
        {
            var strVariable = strPair[0].Trim();

            var magicOverwritten = magicOverrides(parent, strVariable);
            if (magicOverwritten >= 0 ||
                magicOverwritten < 0 && magicOverwritten != (int) MagicUnifiedNumbers.ContinueMagicNumbers)
                return magicOverwritten;

            if (LookupDictionaries.Techs.TryGetValue(strVariable, out var techIndex))
                return (int) MagicUnifiedNumbers.Technology + techIndex;

            if (LookupDictionaries.Units.TryGetValue(strVariable, out var unitIndex))
            {
                // God damn artillery being both a good and unit.
                if (!float.TryParse(strPair[1], out _))
                    return (int) MagicUnifiedNumbers.Unit + unitIndex;

                if (LookupDictionaries.Goods.TryGetValue(strVariable, out var goodUnitIndex))
                    return (int) MagicUnifiedNumbers.Goods + goodUnitIndex;
            }

            if (LookupDictionaries.PopTypes.TryGetValue(strVariable, out var popIndex))
                return (int) MagicUnifiedNumbers.PopType + popIndex;

            if (LookupDictionaries.Schools.TryGetValue(strVariable, out var schoolIndex))
                return (int) MagicUnifiedNumbers.TechSchool + schoolIndex;

            if (LookupDictionaries.Ideologies.TryGetValue(strVariable, out var ideologyIndex))
                return (int) MagicUnifiedNumbers.Ideology + ideologyIndex;

            if (LookupDictionaries.Goods.TryGetValue(strVariable, out var goodsIndex))
                return (int) MagicUnifiedNumbers.Goods + goodsIndex;

            if (LookupDictionaries.PolicyGroups.TryGetValue(strVariable, out var policyGroupIndex))
                return (int) MagicUnifiedNumbers.PolicyGroup + policyGroupIndex;

            if (LookupDictionaries.SubPolicies.TryGetValue(strVariable, out var subPolicyIndex))
                return (int) MagicUnifiedNumbers.SubPolicy + subPolicyIndex;

            if (LookupDictionaries.Religions.TryGetValue(strVariable, out var religionIndex))
                return (int) MagicUnifiedNumbers.Religion + religionIndex;

            if (LookupDictionaries.Governments.TryGetValue(strVariable, out var governmentIndex))
                return (int) MagicUnifiedNumbers.Government + governmentIndex;

            if (LookupDictionaries.States.TryGetValue(strVariable, out var stateIndex))
                return (int) MagicUnifiedNumbers.State + stateIndex;

            if (LookupDictionaries.FolderResearchBonuses.TryGetValue(strVariable, out var researchBonusIndex))
                return (int) MagicUnifiedNumbers.ResearchBonus + researchBonusIndex;

            //if (int.TryParse(strVariable, out var provId) && IdIndex.TryGetValue(provId, out var provIndex))
                //return (int) MagicUnifiedNumbers.Province + provIndex;

            if (strVariable.Length == 3 &&
                LookupDictionaries.CountryTags.TryGetValue(strVariable, out var countryIndex))
                return (int) MagicUnifiedNumbers.Countries + countryIndex;

            var checker = strVariable.Replace("_", "");
            if (Enum.TryParse(checker, true, out LoadVariables enumResult))
                switch (enumResult)
                {
                    case LoadVariables.Value:
                        switch (parent)
                        {
                            case (int) LoadVariables.Ideology:
                                return (int) LoadVariables.IdeologyProperty;
                            case (int) LoadVariables.DominantIssue:
                                return (int) LoadVariables.DominantProperty;
                            case (int) LoadVariables.SubUnit:
                                // Not used
                                return (int) LoadVariables.SubUnitProperty;
                            default:
                                return (int) LoadVariables.Value;
                        }

                    case LoadVariables.Type:
                        switch (parent)
                        {
                            case (int) LoadVariables.SubUnit:
                                return (int) LoadVariables.SubUnitType;
                            default:
                                return (int) LoadVariables.Type;
                        }

                    default:
                        return (int) enumResult;
                }

            Debug.Log($"Unknown value: {checker}.");
            return (int) MagicUnifiedNumbers.SkipMagicNumbers;
        }

        /*
        public static void ConvertToType<TOutput>(ref TOutput input, (int Key, object Value) target,
            Func<string, (int, object), (bool, float)> switchOverrides) where TOutput : IFirstLevelData<TOutput>
        {
            string targetStr;
            try
            {
                targetStr = target.Key < (int) LoadVariables.BreakCore
                    ? (string) target.Value
                    : string.Empty;
            }
            catch (Exception _)
            {
                Debug.Log((LoadVariables) target.Key + " " + target.Value);
                throw;
            }

            var (toggle, custom) = (false, 0f);

            if (switchOverrides != null)
                (toggle, custom) = switchOverrides(targetStr, target);

            if (toggle)
            {
                input.Assignment(target.Key, custom);
                return;
            }

            // Command word detection. Should never be used in a check type.
            if (target.Key < (int) LoadVariables.BreakCore
                && Enum.TryParse(targetStr, true, out CommandWords commandIndex))
            {
                input.Assignment(target.Key, (int) commandIndex);
                return;
            }

            switch ((LoadVariables) target.Key)
            {
                // Terrain
                case LoadVariables.Attrition:
                case LoadVariables.MinBuildNavalBase:
                case LoadVariables.MinBuildRailroad:
                case LoadVariables.MinBuildFort:
                case LoadVariables.MinBuildRoad:
                // Countries
                case LoadVariables.NonStateCultureLiteracy:
                case LoadVariables.NonStateConsciousness:
                // Event Modifiers
                case LoadVariables.PoorLifeNeeds:
                case LoadVariables.MiddleLifeNeeds:
                case LoadVariables.RichLifeNeeds:
                case LoadVariables.LocalRepair:
                case LoadVariables.GoodsDemand:
                case LoadVariables.ResearchPoints:
                case LoadVariables.AssimilationRate:
                case LoadVariables.LocalRgoThroughput:
                case LoadVariables.PopulationGrowth:
                case LoadVariables.LandAttackModifier:
                case LoadVariables.NavalOrganisation:
                case LoadVariables.MovementCost:
                case LoadVariables.DiplomaticPointsModifier:
                case LoadVariables.LocalArtisanOutput:
                case LoadVariables.LocalArtisanInput:
                case LoadVariables.LocalFactoryOutput:
                case LoadVariables.LocalFactoryInput:
                case LoadVariables.ImmigrantAttract:
                case LoadVariables.PopMilitancyModifier:
                case LoadVariables.GlobalPopConsciousnessModifier:
                case LoadVariables.GlobalPopMilitancyModifier:
                case LoadVariables.InfluenceModifier:
                case LoadVariables.ResearchPointsModifier:
                case LoadVariables.LocalShipBuild:
                case LoadVariables.MaxAttrition:
                // Crime
                case LoadVariables.Icon:
                case LoadVariables.PopConsciousnessModifier:
                case LoadVariables.LifeRating:
                case LoadVariables.LocalRgoOutput:
                case LoadVariables.LocalFactoryThroughput:
                case LoadVariables.LocalArtisanThroughput:
                case LoadVariables.BoostStrongestParty:
                case LoadVariables.NumberOfVoters:
                case LoadVariables.LocalRulingPartySupport:
                case LoadVariables.ImmigrantPush:
                case LoadVariables.MiddleEverydayNeeds:
                case LoadVariables.RichEverydayNeeds:
                // Inventions
                case LoadVariables.Hull:
                case LoadVariables.GunPower:
                case LoadVariables.TorpedoAttack:
                case LoadVariables.SoldierToPopLoss:
                case LoadVariables.LandAttrition:
                case LoadVariables.PopGrowth:
                case LoadVariables.NavalAttrition:
                case LoadVariables.Seperatism:
                case LoadVariables.Base:
                case LoadVariables.Siege:
                case LoadVariables.Reconnaissance:
                case LoadVariables.WarExhaustion:
                case LoadVariables.CorePopConsciousnessModifier:
                case LoadVariables.PermanentPrestige:
                case LoadVariables.LoanInterest:
                case LoadVariables.SharedPrestige:
                case LoadVariables.PoliticalReformDesire:
                case LoadVariables.AverageMilitancy:
                // Ideologies
                case LoadVariables.PoliticalMovementStrength:
                case LoadVariables.SocialMovementStrength:
                // Events
                case LoadVariables.ReducePop:
                case LoadVariables.ChanceProperty:
                case LoadVariables.Leadership:
                case LoadVariables.ProvinceId:
                case LoadVariables.Duration:
                case LoadVariables.Id:
                case LoadVariables.Consciousness:
                case LoadVariables.Militancy:
                case LoadVariables.AverageConsciousness:
                case LoadVariables.Days:
                case LoadVariables.Months:
                case LoadVariables.Blockade:
                case LoadVariables.NumOfRevolts:
                case LoadVariables.BrigadesCompare:
                case LoadVariables.PrestigeFactor:
                case LoadVariables.UnitsInProvince:
                case LoadVariables.CivilizationProgress:
                case LoadVariables.TotalAmountOfDivisions:
                case LoadVariables.RevoltPercentage:
                case LoadVariables.AddTaxRelativeIncome:
                case LoadVariables.YearsOfResearch:
                case LoadVariables.NationalProvincesOccupied:
                case LoadVariables.LostNational:
                case LoadVariables.HasRecentImigration: // Yes, this is spelled wrong.
                case LoadVariables.PoorStrataLifeNeeds:
                case LoadVariables.PoorStrataEverydayNeeds:
                case LoadVariables.PoorStrataLuxuryNeeds:
                case LoadVariables.MiddleStrataLifeNeeds:
                case LoadVariables.MiddleStrataEverydayNeeds:
                case LoadVariables.MiddleStrataLuxuryNeeds:
                case LoadVariables.RichStrataLifeNeeds:
                case LoadVariables.RichStrataEverydayNeeds:
                case LoadVariables.RichStrataLuxuryNeeds:
                case LoadVariables.Month:
                case LoadVariables.Fort:
                case LoadVariables.IsCanalEnabled:
                case LoadVariables.EnableCanal:
                case LoadVariables.NumOfPorts:
                case LoadVariables.TotalSunkByUs:
                case LoadVariables.ConstructingCbProgress:
                case LoadVariables.SocialReformWant:
                case LoadVariables.PoliticalReformWant:
                case LoadVariables.LoyaltyValue:
                case LoadVariables.AdministrationSpending:
                case LoadVariables.FlashPointTension:
                case LoadVariables.AddCrisisTemperature:
                case LoadVariables.NumOfVassalsNoSubStates:
                case LoadVariables.Revanchism:
                case LoadVariables.PoorStrataMilitancy:
                case LoadVariables.MiddleStrataMilitancy:
                case LoadVariables.RichStrataMilitancy:
                case LoadVariables.NavalBase:
                case LoadVariables.RemoveRandomEconomicReforms:
                case LoadVariables.RemoveRandomMilitaryReforms:
                case LoadVariables.PopMilitancy:
                case LoadVariables.LifeNeedsProperty:
                case LoadVariables.EverydayNeedsProperty:
                case LoadVariables.LuxuryNeedsProperty:
                case LoadVariables.Unemployment:
                case LoadVariables.EducationSpending:
                case LoadVariables.MilitarySpending:
                case LoadVariables.CrimeFighting:
                // Issues
                case LoadVariables.Plurality:
                case LoadVariables.NavalUnitStartExperience:
                case LoadVariables.NavalAttackModifier:
                case LoadVariables.NavalDefenseModifier:
                case LoadVariables.LandOrganisation:
                case LoadVariables.LandDefenseModifier:
                case LoadVariables.TariffEfficiencyModifier:
                case LoadVariables.LeadershipModifier:
                case LoadVariables.CountryEventProperty: // Fires Event
                case LoadVariables.ResearchPointsOnConquer:
                case LoadVariables.FarmRgoEff:
                case LoadVariables.MineRgoEff:
                case LoadVariables.Treasury:
                case LoadVariables.AdministrativeEfficiencyModifier:
                case LoadVariables.MaxLoanModifier:
                case LoadVariables.MinimumWage:
                case LoadVariables.ArtisanThroughput:
                case LoadVariables.FactoryMaintenance:
                case LoadVariables.ArtisanOutput:
                case LoadVariables.PensionLevel:
                case LoadVariables.UnemploymentBenefit:
                case LoadVariables.GlobalPopulationGrowth:
                case LoadVariables.BadBoy:
                case LoadVariables.FarmRgoSize:
                case LoadVariables.MineRgoSize:
                case LoadVariables.CivilizationProgressModifier:
                case LoadVariables.TechnologyCost:
                case LoadVariables.NonAcceptedPopConsciousnessModifier:
                case LoadVariables.NonAcceptedPopMilitancyModifier:
                case LoadVariables.PoorSavingsModifier:
                case LoadVariables.TaxEfficiency:
                case LoadVariables.RgoThroughput:
                case LoadVariables.PoorEverydayNeeds:
                case LoadVariables.AdministrativeMultiplier:
                case LoadVariables.CorePopMilitancyModifier:
                case LoadVariables.MobilisationSize:
                case LoadVariables.MobilisationEconomyImpact:
                case LoadVariables.LandUnitStartExperience:
                case LoadVariables.EducationEfficiencyModifier:
                case LoadVariables.SocialReformDesire:
                case LoadVariables.LiteracyConImpact:
                case LoadVariables.IssueChangeSpeed:
                case LoadVariables.SuppressionPointsModifier:
                case LoadVariables.RulingPartySupport:
                case LoadVariables.RichVote: // Vote multipliers.
                case LoadVariables.MiddleVote:
                case LoadVariables.PoorVote:
                case LoadVariables.MinSocialSpending:
                case LoadVariables.MaxSocialSpending:
                case LoadVariables.ReinforceSpeed:
                case LoadVariables.OrgRegain:
                case LoadVariables.MobilizationImpact:
                case LoadVariables.CbGenerationSpeedModifier:
                case LoadVariables.WarExhaustionEffect:
                case LoadVariables.MinMilitarySpending:
                case LoadVariables.MaxMilitarySpending:
                case LoadVariables.MinTax:
                case LoadVariables.GlobalAssimilationRate:
                case LoadVariables.MaxTariff:
                case LoadVariables.MinTariff:
                case LoadVariables.MaxTax:
                case LoadVariables.FactoryOwnerCost:
                case LoadVariables.FactoryOutput:
                case LoadVariables.ImportCost:
                case LoadVariables.GlobalImmigrantAttract:
                // Tech
                case LoadVariables.Year:
                case LoadVariables.Cost:
                case LoadVariables.DiplomaticPoints:
                case LoadVariables.ColonialPoints:
                case LoadVariables.TotalNumOfPorts:
                case LoadVariables.TotalAmountOfShips:
                case LoadVariables.NumOfCities:
                case LoadVariables.MaxNavalBase:
                case LoadVariables.Money:
                case LoadVariables.MaximumSpeed:
                case LoadVariables.BuildTime:
                case LoadVariables.ColonialPrestige:
                case LoadVariables.SupplyRange:
                case LoadVariables.FactoryThroughput:
                case LoadVariables.FactoryCost:
                case LoadVariables.RgoOutput:
                case LoadVariables.ProvinceControlDays:
                case LoadVariables.Value: // For unemployment by type
                case LoadVariables.MaxRailroad:
                case LoadVariables.SupplyLimit:
                case LoadVariables.AdministrativeEfficiency:
                case LoadVariables.FactoryInput:
                case LoadVariables.IndustrialScore:
                case LoadVariables.Influence:
                case LoadVariables.Literacy:
                case LoadVariables.Prestige:
                case LoadVariables.IncreaseResearch: // Research rate
                case LoadVariables.ColonialMigration:
                case LoadVariables.EducationEfficiency:
                case LoadVariables.MaxNationalFocus:
                case LoadVariables.TotalPops:
                case LoadVariables.CbCreationSpeed:
                case LoadVariables.RegularExperienceLevel:
                case LoadVariables.ReinforceRate:
                case LoadVariables.TaxEff:
                case LoadVariables.MilitaryTactics:
                case LoadVariables.Morale:
                case LoadVariables.Defence:
                case LoadVariables.Attack:
                case LoadVariables.Unit:
                case LoadVariables.Support:
                case LoadVariables.NumberOfStates:
                case LoadVariables.Rank:
                case LoadVariables.ColonialLifeRating:
                case LoadVariables.DefaultOrganisation:
                case LoadVariables.CombatWidth:
                case LoadVariables.Factor:
                case LoadVariables.MaxFort:
                case LoadVariables.DigInCap:
                case LoadVariables.SupplyConsumption:
                    if (!float.TryParse(targetStr, out var floatValue))
                        throw new Exception("Float parsing failed! " + targetStr + " of type: " +
                                            (LoadVariables) target.Key);

                    input.Assignment(target.Key, math.round(floatValue * 1000f) / 1000f);
                    break;
                // Crime
                case LoadVariables.Active:
                // Elections
                case LoadVariables.AppointRulingParty:
                // Inventions
                case LoadVariables.GasDefence:
                case LoadVariables.GasAttack:
                // Events
                case LoadVariables.IsSlave:
                case LoadVariables.Major:
                case LoadVariables.FireOnlyOnce:
                case LoadVariables.Neutrality:
                case LoadVariables.AllowMultipleInstances:
                case LoadVariables.IsTriggeredOnly:
                case LoadVariables.PartOfSphere:
                case LoadVariables.IsVassal:
                case LoadVariables.ControlledByRebels:
                case LoadVariables.CallAlly:
                case LoadVariables.IsDisarmed:
                case LoadVariables.HasNationalMinority:
                case LoadVariables.UnitInBattle:
                case LoadVariables.InvolvedInCrisis:
                case LoadVariables.IsStateReligion:
                case LoadVariables.ThisCultureUnion:
                case LoadVariables.IsCulturalUnion:
                case LoadVariables.IsPrimaryCulture:
                case LoadVariables.IsAcceptedCulture:
                case LoadVariables.Nationalize:
                case LoadVariables.Assimilate:
                case LoadVariables.AddCrisisInterest:
                case LoadVariables.HasFlashPoint:
                case LoadVariables.CrisisExist:
                case LoadVariables.IsClaimCrisis:
                case LoadVariables.IsLiberationCrisis:
                case LoadVariables.IsMobilised:
                case LoadVariables.CultureHasUnionTag:
                case LoadVariables.IsIndependent:
                case LoadVariables.IsCoastal:
                case LoadVariables.IsCapital:
                case LoadVariables.IsStateCapital:
                case LoadVariables.IsWater:
                // Issues
                case LoadVariables.Port:
                case LoadVariables.Ai:
                case LoadVariables.LimitToWorldGreatestLevel: // No clue what this does
                case LoadVariables.InWholeCapitalState:
                case LoadVariables.IsSubState:
                case LoadVariables.Administrative:
                case LoadVariables.AllVoting:
                case LoadVariables.CultureVoting:
                case LoadVariables.PrimaryCultureVoting:
                case LoadVariables.LargestShare: // Voting system
                case LoadVariables.Dhont:
                case LoadVariables.SainteLaque:
                case LoadVariables.Election:
                case LoadVariables.PopulationVote: // Upper party representation
                case LoadVariables.StateVote:
                case LoadVariables.RichOnly:
                case LoadVariables.SameAsRulingParty:
                case LoadVariables.NextStepOnly:
                case LoadVariables.IsJingoism:
                case LoadVariables.SlaveryAllowed:
                case LoadVariables.BuildFactory:
                case LoadVariables.ExpandFactory:
                case LoadVariables.OpenFactory:
                case LoadVariables.DestroyFactory:
                case LoadVariables.BuildRailway:
                case LoadVariables.FactoryPriority:
                case LoadVariables.CanSubsidise:
                case LoadVariables.PopBuildFactory:
                case LoadVariables.PopExpandFactory:
                case LoadVariables.PopOpenFactory:
                case LoadVariables.DeleteFactoryIfNoInput:
                case LoadVariables.PopBuildFactoryInvest:
                case LoadVariables.PopExpandFactoryInvest:
                case LoadVariables.OpenFactoryInvest:
                case LoadVariables.AllowForeignInvestment:
                case LoadVariables.BuildRailwayInvest:
                case LoadVariables.BuildFactoryInvest:
                case LoadVariables.ExpandFactoryInvest:
                case LoadVariables.CanInvestInPopProjects:
                // Tech
                case LoadVariables.IsOverseas:
                case LoadVariables.IsColonial:
                case LoadVariables.ColonialNation:
                case LoadVariables.Empty:
                case LoadVariables.IsSecondaryPower:
                case LoadVariables.HasUnclaimedCores:
                case LoadVariables.Civilized:
                case LoadVariables.HasRecentlyLostWar:
                case LoadVariables.WarProperty:
                case LoadVariables.IsGreaterPower:
                case LoadVariables.UnCivMilitary:
                    input.Assignment(target.Key, LoadMethods.YesNoConverter(targetStr) ? 1 : 0);
                    break;
                case LoadVariables.SubUnitProperty:
                case LoadVariables.News:
                case LoadVariables.NewsTitle:
                case LoadVariables.NewsDescLong:
                case LoadVariables.NewsDescMedium:
                case LoadVariables.NewsDescShort:
                    // Skipped
                    break;
                case LoadVariables.Which: // Flag?
                    // TODO: Variable parsing? No clue.
                    break;
                case LoadVariables.Terrain:
                    if (!LookupDictionaries.Terrain.TryGetValue(targetStr, out var terrainIndex))
                        throw new Exception("Unknown terrain. " + targetStr);

                    input.Assignment(target.Key, terrainIndex);
                    break;
                case LoadVariables.SetProvinceFlag:
                case LoadVariables.ClrProvinceFlag:
                    // TODO: Province flags? Maybe????
                    break;
                case LoadVariables.Strata:
                    if (!Enum.TryParse(targetStr, true, out PopTypeEntity.Standing standingIndex))
                        throw new Exception("Unknown strata. " + targetStr);

                    input.Assignment(target.Key, (int) standingIndex);
                    break;
                case LoadVariables.Personality: // Personality
                case LoadVariables.Background: // Personality
                    // TODO: General personality parsing.
                    break;
                case LoadVariables.Religion:
                case LoadVariables.HasPopReligion:
                    if (!LookupDictionaries.Religions.TryGetValue(targetStr, out var religionIndex))
                        throw new Exception("Unknown religion. " + targetStr);

                    input.Assignment(target.Key, religionIndex);
                    break;
                case LoadVariables.Controls:
                case LoadVariables.MovePop:
                case LoadVariables.StateProvinceId:
                case LoadVariables.Capital:
                case LoadVariables.Owns:
                case LoadVariables.StateId: // Province numbers. Why? No clue.
                    if (!int.TryParse(targetStr, out var provId) || !IdIndex.TryGetValue(provId, out var provIndex))
                        throw new Exception("Province Id unknown. " + targetStr);

                    input.Assignment(target.Key, provIndex);
                    break;
                case LoadVariables.Region:
                    if (!LookupDictionaries.States.TryGetValue(targetStr, out var stateIndex))
                        throw new Exception("Unknown state. " + targetStr);

                    input.Assignment(target.Key, stateIndex);
                    break;
                case LoadVariables.Date:
                case LoadVariables.StartDate:
                case LoadVariables.EndDate:
                    if (!DateTime.TryParse(targetStr, out var dateResult))
                        throw new Exception("Unknown date. " + targetStr);
                    int.TryParse("9" + dateResult.ToString("yyyyMMdd"), out var dateInt);

                    input.Assignment(target.Key, dateInt);
                    break;
                case LoadVariables.AddCrime:
                case LoadVariables.EnableCrime:
                    if (!LookupDictionaries.Crimes.TryGetValue(targetStr, out var crimeIndex))
                        throw new Exception("Unknown crime. " + targetStr);

                    input.Assignment(target.Key, crimeIndex);
                    break;
                case LoadVariables.Faction:
                    // TODO: rebel faction parsing.
                    break;
                case LoadVariables.Government:
                    if (!LookupDictionaries.Governments.TryGetValue(targetStr, out var governmentIndex))
                        throw new Exception("Unknown government. " + targetStr);

                    input.Assignment(target.Key, governmentIndex);
                    break;
                case LoadVariables.NationalValue:
                    if (!LookupDictionaries.NationalValues.TryGetValue(targetStr, out var nationalValueIndex))
                        throw new Exception("Unknown national value. " + targetStr);

                    input.Assignment(target.Key, nationalValueIndex);
                    break;
                case LoadVariables.HasProvinceModifier:
                case LoadVariables.RemoveProvinceModifier:
                case LoadVariables.RemoveCountryModifier:
                case LoadVariables.HasCountryModifier:
                    if (!LookupDictionaries.EventModifiers.TryGetValue(targetStr, out var eventModifierIndex))
                        throw new Exception("Unknown event modifier. " + targetStr);

                    input.Assignment(target.Key, eventModifierIndex);
                    break;
                case LoadVariables.ClrGlobalFlag:
                case LoadVariables.SetGlobalFlag:
                case LoadVariables.HasGlobalFlag:
                    if (!GlobalFlags.TryGetValue(targetStr, out var globalFlagIndex))
                    {
                        globalFlagIndex = GlobalFlags.Count;
                        GlobalFlags.Add(targetStr, GlobalFlags.Count);
                    }

                    input.Assignment(target.Key, globalFlagIndex);
                    break;
                case LoadVariables.ClrCountryFlag:
                case LoadVariables.SetCountryFlag:
                case LoadVariables.HasCountryFlag:
                    if (!CountryFlags.TryGetValue(targetStr, out var countryFlagIndex))
                    {
                        countryFlagIndex = CountryFlags.Count;
                        CountryFlags.Add(targetStr, CountryFlags.Count);
                    }

                    input.Assignment(target.Key, countryFlagIndex);
                    break;
                case LoadVariables.Worker:
                case LoadVariables.PopType:
                case LoadVariables.HasPopType:
                case LoadVariables.Type: // For use in unemployment by type
                    if (!LookupDictionaries.PopTypes.TryGetValue(targetStr, out var popIndex))
                        throw new Exception("Unknown pop type. " + targetStr);

                    input.Assignment(target.Key, popIndex);
                    break;
                case LoadVariables.Invention:
                    if (!LookupDictionaries.Inventions.TryGetValue(targetStr, out var inventIndex))
                        throw new Exception("Unknown invention. " + targetStr);

                    input.Assignment(target.Key, inventIndex);
                    break;
                case LoadVariables.Building:
                case LoadVariables.BuildFactoryInCapitalState:
                case LoadVariables.CanBuildFactoryInCapitalState:
                case LoadVariables.HasBuilding:
                case LoadVariables.ActivateBuilding:
                    // Generic factory building
                    if (targetStr.Equals("factory"))
                    {
                        input.Assignment(target.Key, -1);
                        break;
                    }

                    if (!LookupDictionaries.Buildings.TryGetValue(targetStr, out var buildingIndex))
                        throw new Exception("Unknown building. " + targetStr);

                    input.Assignment(target.Key, buildingIndex);
                    break;
                case LoadVariables.Continent:
                    if (!LookupDictionaries.Continents.TryGetValue(targetStr, out var continentIndex))
                        throw new Exception("Unknown continent. " + targetStr);

                    input.Assignment(target.Key, continentIndex);
                    break;
                case LoadVariables.TechSchool:
                    if (!LookupDictionaries.Schools.TryGetValue(targetStr, out var techSchool))
                        throw new Exception("Unknown tech school. " + targetStr);

                    input.Assignment(target.Key, techSchool);
                    break;
                case LoadVariables.SubUnitType:
                case LoadVariables.ActivateUnit:
                    if (!LookupDictionaries.Units.TryGetValue(targetStr, out var unitIndex))
                        throw new Exception("Unknown unit. " + targetStr);

                    input.Assignment(target.Key, unitIndex);
                    break;
                case LoadVariables.BigProducer: // Lead producer?
                case LoadVariables.Produces:
                case LoadVariables.TradeGoods:
                    if (!LookupDictionaries.Goods.TryGetValue(targetStr, out var goodIndex))
                        throw new Exception("Unknown good. " + targetStr);

                    input.Assignment(target.Key, goodIndex);
                    break;
                case LoadVariables.Exists:
                    // Exists can be both bool and country tag. Oh joy.
                    // ExistsTag for tags, Exist for bool
                    if (targetStr.Length == 3 && !targetStr.Equals("yes"))
                    {
                        if (!LookupDictionaries.CountryTags.TryGetValue(targetStr, out var existsTagIndex))
                            throw new Exception("Unknown exists tag. " + targetStr);

                        input.Assignment((int) LoadVariables.ExistsTag, existsTagIndex);
                    }
                    else
                    {
                        input.Assignment((int) LoadVariables.Exists,
                            LoadMethods.YesNoConverter(targetStr) ? 1 : 0);
                    }

                    break;
                case LoadVariables.EndMilitaryAccess:
                case LoadVariables.MilitaryAccess:
                case LoadVariables.IsPossibleVassal:
                case LoadVariables.CreateAlliance:
                case LoadVariables.Release:
                case LoadVariables.ChangeController:
                case LoadVariables.SubStateOf:
                case LoadVariables.Neighbour:
                case LoadVariables.EndWar:
                case LoadVariables.AnnexTo:
                case LoadVariables.CountryUnitsInProvince:
                case LoadVariables.VassalOf:
                case LoadVariables.CreateVassal:
                case LoadVariables.IsSphereLeaderOf:
                case LoadVariables.CountryProperty:
                case LoadVariables.AllianceWith:
                case LoadVariables.LeaveAlliance:
                case LoadVariables.ChangeTag:
                case LoadVariables.RemoveCore:
                case LoadVariables.Inherit:
                case LoadVariables.InSphere:
                case LoadVariables.OwnedBy:
                case LoadVariables.ControlledBy:
                case LoadVariables.MilitaryScore:
                case LoadVariables.AddCore:
                case LoadVariables.IsCore:
                case LoadVariables.SecedeProvince:
                case LoadVariables.ChangeTagNoCoreSwitch:
                case LoadVariables.IsOurVassal:
                case LoadVariables.ReleaseVassal:
                case LoadVariables.TruceWith:
                case LoadVariables.WarWith:
                case LoadVariables.Target:
                case LoadVariables.Who:
                case LoadVariables.Tag:
                    if (!LookupDictionaries.CountryTags.TryGetValue(targetStr, out var tagIndex))
                        throw new Exception("Unknown country tag. " + targetStr);

                    input.Assignment(target.Key, tagIndex);
                    break;
                case LoadVariables.PopMajorityCulture:
                case LoadVariables.AcceptedCulture:
                case LoadVariables.Culture:
                case LoadVariables.RemoveAcceptedCulture:
                case LoadVariables.AddAcceptedCulture:
                case LoadVariables.HasPopCulture:
                case LoadVariables.PrimaryCulture:
                    if (!LookupDictionaries.Cultures.TryGetValue(targetStr, out var cultureIndex))
                        throw new Exception("Unknown culture. " + targetStr);

                    input.Assignment(target.Key, cultureIndex);
                    break;
                case LoadVariables.IsCultureGroup:
                    if (!LookupDictionaries.CultureGroups.TryGetValue(targetStr, out var cultureGroupIndex))
                        throw new Exception("Unknown culture group. " + targetStr);

                    input.Assignment(target.Key, cultureGroupIndex);
                    break;
                case LoadVariables.EnableIdeology:
                case LoadVariables.PopMajorityIdeology:
                case LoadVariables.IsIdeologyEnabled:
                case LoadVariables.RulingPartyIdeology:
                case LoadVariables.IdeologyProperty:
                    if (!LookupDictionaries.Ideologies.TryGetValue(targetStr, out var ideologyIndex))
                        throw new Exception("Unknown ideology. " + targetStr);

                    input.Assignment(target.Key, ideologyIndex);
                    break;
                case LoadVariables.ActivateTechnology:
                    if (!LookupDictionaries.Techs.TryGetValue(targetStr, out var techIndex))
                        throw new Exception("Unknown technology. " + targetStr);

                    input.Assignment(target.Key, techIndex);
                    break;
                case LoadVariables.ConstructingCbType:
                case LoadVariables.TypeCasusBelli:
                case LoadVariables.CasusBelliProperty:
                    // TODO: Proper CB parsing.
                    break;
                case LoadVariables.FromProperty:
                case LoadVariables.To:
                case LoadVariables.DominantProperty:
                case LoadVariables.PoliticalReform:
                case LoadVariables.SocialReform:
                case LoadVariables.EconomicReform:
                case LoadVariables.MilitaryReform:
                case LoadVariables.Issue:
                    if (!LookupDictionaries.SubPolicies.TryGetValue(targetStr, out var policyIndex))
                        throw new Exception("Unknown policy. " + targetStr);

                    input.Assignment(target.Key, policyIndex);
                    break;
                case LoadVariables.IssueGroup:
                    if (!LookupDictionaries.PolicyGroups.TryGetValue(targetStr, out var policyGroupIndex))
                        throw new Exception("Unknown policy group. " + targetStr);

                    input.Assignment(target.Key, policyGroupIndex);
                    break;
                case LoadVariables.Palette:
                case LoadVariables.Color:
                    throw new Exception("Colors can not be parsed by unified variables and must be overwritten.");
                default:
                    switch ((MagicUnifiedNumbers) (target.Key / 10000 * 10000))
                    {
                        case MagicUnifiedNumbers.SkipMagicNumbers: // Unknowns. Reserved for development!
                            throw new Exception("Unknown value type. " + target.Key);
                        case MagicUnifiedNumbers.TechSchool:
                            throw new Exception("Test Unified School");
                        case MagicUnifiedNumbers.ResearchBonus: // Used in events. E.g: naval_tech_research_bonus
                        case MagicUnifiedNumbers.Technology:
                        case MagicUnifiedNumbers.Ideology: // Percentage in upper house. Used in inventions.
                        case MagicUnifiedNumbers.Goods:
                            if (!float.TryParse((string) target.Value, out var magicFloat))
                                throw new Exception("Unknown magic float. " + (string) target.Value);

                            input.Assignment(target.Key, magicFloat);
                            break;
                        case MagicUnifiedNumbers.Invention:
                            // Used in inventions and country history load
                            throw new Exception("Test Unified Inventions");
                        case MagicUnifiedNumbers.PolicyGroup:
                            // Policy checkers inside other policies, madness I tell you.
                            // Example: War_Policy under draft laws.
                            if (!LookupDictionaries.SubPolicies.TryGetValue((string) target.Value,
                                out var subPolicyIndex))
                                throw new Exception("Unknown sub policy. " + (string) target.Value);

                            input.Assignment(target.Key, subPolicyIndex);
                            break;
                        case MagicUnifiedNumbers.PopSubPolicy: // percentage of population who believe in sub policy
                            if (!float.TryParse((string) target.Value, out var subPolicyPopFloat))
                                throw new Exception("Population sub policy float parsing failed! " + targetStr);

                            input.Assignment(target.Key, subPolicyPopFloat);
                            break;
                        case MagicUnifiedNumbers.IdeologyGroup:
                            // Unused outside Ideology.txt to mark groups
                            throw new Exception("Test Unified Ideology Group");
                        case MagicUnifiedNumbers.Government:
                            throw new Exception(
                                "Government: Colors can not be parsed by unified variables and must be overwritten.");
                        //case MagicUnifiedNumbers.PopType:
                        //case MagicUnifiedNumbers.SubPolicy: // Used in Issues.Main()
                        //case MagicUnifiedNumbers.Unit:
                        //case MagicUnifiedNumbers.Countries:
                        //case MagicUnifiedNumbers.State:
                        //case MagicUnifiedNumbers.Terrain:
                        default:
                            if (target.Key < (int) LoadVariables.BreakCore) Debug.Log("Unknown: " + (LoadVariables) target.Key);

                            //nextQueue.Enqueue(target);
                            break;
                    }

                    break;
            }
        }
                    */
    }
}