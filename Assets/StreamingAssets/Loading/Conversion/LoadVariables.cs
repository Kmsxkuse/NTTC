﻿namespace Conversion
{
    public enum LoadVariables
    {
        // Goods
        AvailableFromStart,
        OverseasPenalty,
        Tradeable,
        Colonial,

        // Terrain
        Palette,
        Attrition,
        MinBuildNavalBase,
        MinBuildRailroad,
        MinBuildFort,
        MinBuildRoad,

        // Event Modifiers
        PopMilitancyModifier,
        GlobalPopConsciousnessModifier,
        GlobalPopMilitancyModifier,
        InfluenceModifier,
        ResearchPointsModifier,
        LocalArtisanOutput,
        LocalArtisanInput,
        LocalFactoryOutput,
        LocalFactoryInput,
        ImmigrantAttract,
        DiplomaticPointsModifier,
        AssimilationRate,
        LocalRgoThroughput,
        PopulationGrowth,
        LandAttackModifier,
        NavalOrganisation,
        PoorLifeNeeds,
        MiddleLifeNeeds,
        RichLifeNeeds,
        LocalRepair,
        GoodsDemand,
        ResearchPoints,
        MaxAttrition,

        // Crime
        PopConsciousnessModifier,
        LifeRating,
        LocalRgoOutput,
        LocalFactoryThroughput,
        LocalArtisanThroughput,
        BoostStrongestParty,
        NumberOfVoters,
        LocalRulingPartySupport,
        ImmigrantPush,
        MiddleEverydayNeeds,
        RichEverydayNeeds,

        // Governments
        AppointRulingParty,
        FlagType,

        // Countries
        StartDate, // YYYY MM DD
        EndDate,
        NonStateCultureLiteracy,
        Decision, // No clue
        NonStateConsciousness,
        RulingParty, // skipped for now
        Oob, // skipped

        // Buildings
        CompletionSize,
        MaxLevel,
        Time,
        DefaultEnabled,
        Province,
        FortLevel,
        OnePerState,
        ColonialRange,
        NavalCapacity,
        LocalShipBuild,
        Infrastructure,
        MovementCost,
        ColonialMultiplier, // COLONIAL POINTS CONVERTED TO MATH FUNCTION!
        ColonialBase, // math.round(Ln(BuildingLevel) * ColonialMultiplier + ColonialBase)

        // Ideologies
        Uncivilized,
        Date,
        CanReduceMilitancy,
        PoliticalMovementStrength,
        SocialMovementStrength,

        // Pop Types
        Unemployment,
        IsPrimaryCulture,
        IsAcceptedCulture,
        IsStateCapital,
        IsCapital,
        IsCoastal,
        Terrain,
        HasFactories,
        Worker,
        DebtLaw,
        MilitarySpending,
        EducationSpending,
        CrimeFighting,
        PoliticalReformWant,
        Sprite,
        Color,
        Strata,
        StateCapitalOnly,

        // Events
        Id,
        Title,
        Desc,
        Picture,
        Major,
        FireOnlyOnce,

        // Skipped
        News,
        NewsTitle,
        NewsDescLong,
        NewsDescMedium,
        NewsDescShort,

        IsSlave,
        Days,
        Months,
        Name,
        HasPopCulture,
        Militancy,
        Consciousness,
        ClrCountryFlag,
        SetGlobalFlag,
        ReleaseVassal,
        Neutrality,
        ChanceProperty,
        ProvinceId,
        PoliticalReform,
        Capital,
        Leadership,
        Duration,
        SecedeProvince,
        Who,
        ChangeTagNoCoreSwitch,
        IsOurVassal,
        WarWith,
        TruceWith,
        Target,
        CasusBelliProperty,
        HasPopType,
        ReducePop,
        IdeologyProperty,
        DominantProperty,
        HasProvinceModifier,
        AddAcceptedCulture,
        RemoveAcceptedCulture,
        Issue,
        AllowMultipleInstances,
        Region,
        RemoveProvinceModifier,
        IsCore,
        StateId,
        AddCore,
        IsTriggeredOnly,
        Blockade,
        Owns, // Province ID
        NumOfRevolts,
        Inherit,
        TypeCasusBelli,
        RemoveCore,
        ChangeRegionName,
        HasLeader,
        ChangeTag,
        Personality,
        Background,
        Which,
        Culture, // culture and this
        ClrGlobalFlag, // Global flag
        PartOfSphere, // bool
        BrigadesCompare, // float
        LeaveAlliance, // Tag/this
        PrestigeFactor, // float
        AllianceWith, // Tag/this
        CountryProperty,
        AcceptedCulture, // Culture
        IsVassal, // bool
        UnitsInProvince, // float
        ControlledByRebels, // bool
        CountryUnitsInProvince, // Tag/this
        VassalOf, // Tag/this
        CreateVassal, // Tag/this
        IsSphereLeaderOf, // Tag/this
        CivilizationProgress, // float
        TotalAmountOfDivisions, // Float
        CallAlly, // bool
        IsDisarmed, // bool
        PopType, // pop
        RevoltPercentage, // float
        AddTaxRelativeIncome, // float
        YearsOfResearch, // float
        NationalProvincesOccupied, // float
        LostNational, // float
        ChangeController, // Tag/this
        SubStateOf, // Tag/this
        Neighbour, // Tag/this
        EndWar, // Tag/this
        AnnexTo, // Tag/this
        MovePop, // Province
        StateProvinceId, // Province
        ExistsTag, // Tag/this
        ChangeProvinceName, // string
        Release, // Tag/this
        HasRecentImigration, // float. Yes, this is spelled wrong.
        HasNationalMinority, // bool
        PoorStrataLifeNeeds, // float
        PoorStrataEverydayNeeds, // float
        PoorStrataLuxuryNeeds, // float
        MiddleStrataLifeNeeds, // float
        MiddleStrataEverydayNeeds, // float
        MiddleStrataLuxuryNeeds, // float
        RichStrataLifeNeeds, // float
        RichStrataEverydayNeeds, // float
        RichStrataLuxuryNeeds, // float
        CreateAlliance, // Tag/this
        Controls, // Province
        IsPossibleVassal, // Tag/this
        UnitInBattle, // bool
        SetProvinceFlag, // Flag?
        Month, // float
        IsCanalEnabled, // float
        EnableCanal, // float
        NumOfPorts, // float
        TotalSunkByUs, // float
        IsStateReligion, // bool
        PopMajorityCulture, // culture
        PopMajorityIdeology, // ideology,
        HasPopReligion, // religion
        InvolvedInCrisis, // bool
        ConstructingCbProgress, // float
        ConstructingCbType, // cb type
        ThisCultureUnion, // this
        IsCulturalUnion, // Bool & this
        EndMilitaryAccess, // tag
        MilitaryAccess, // tag
        SocialReform, // unCiv social_policy hardcoded
        EconomicReform, // unCiv economic_policy hardcoded
        MilitaryReform, // unCiv military_policy hardcoded
        Nationalize, // bool
        SocialReformWant, // float
        ClrProvinceFlag, // Flag?
        Religion,
        Assimilate, // bool
        AddCrisisInterest, // bool
        HasFlashPoint, // bool
        CrisisExist, // bool
        IsClaimCrisis, // bool
        IsLiberationCrisis, // bool
        IsMobilised, // bool
        CultureHasUnionTag, // bool
        IsIndependent, // bool
        AddCrime, // crime
        Building, // building
        IssueGroup,
        LoyaltyValue, // float
        AdministrationSpending, // float
        FlashPointTension, // float
        AddCrisisTemperature, // float
        NumOfVassalsNoSubStates, // float
        Revanchism, // float
        PoorStrataMilitancy, // float
        MiddleStrataMilitancy, // float
        RichStrataMilitancy, // float
        EnableIdeology, // ideology
        FromProperty, // so far, only ideology
        To, // Ideology
        RemoveRandomEconomicReforms, // float
        RemoveRandomMilitaryReforms, // float
        PopMilitancy, // float
        LifeNeedsProperty, // float
        EverydayNeedsProperty, // float
        LuxuryNeedsProperty, // float
        SubUnitType, // unit
        SubUnitProperty, // skipped
        IsWater, // bool

        // TODO: convert to building lookups
        Fort, // float
        NavalBase, // float

        // Tech
        Area,
        Cost,
        UnCivMilitary,
        ActivateBuilding,
        SupplyConsumption,
        DigInCap,
        Factor,
        IsGreaterPower,
        Continent,
        WarProperty,
        HasRecentlyLostWar,
        Civilized,
        MilitaryScore,
        NumberOfStates,
        Rank,
        HasUnclaimedCores,
        TechSchool,
        MaxFort,
        DefaultOrganisation,
        Support,
        ActivateUnit,
        CombatWidth,
        ColonialLifeRating,
        Unit, // Who knows?
        Attack,
        Defence,
        Morale,
        MilitaryTactics,
        MobilisationSize,
        TaxEff,
        Literacy,
        Prestige,
        IncreaseResearch, // Research rate
        IsSecondaryPower,
        ColonialMigration,
        EducationEfficiency,
        Tag,
        MaxNationalFocus,
        ColonialNation,
        TotalPops,
        CbCreationSpeed,
        Empty,
        Exists,
        RegularExperienceLevel,
        ReinforceRate,
        AdministrativeEfficiency,
        FactoryInput,
        IndustrialScore,
        TradeGoods,
        Influence,
        FarmRgoEff,
        IsColonial,
        FactoryOutput,
        BigProducer, // Lead producer?
        FactoryThroughput,
        InSphere,
        OwnedBy,
        FactoryCost,
        RgoOutput,
        Produces,
        ProvinceControlDays,
        ControlledBy,
        Value,
        Type, // For unemployment by type
        MaxRailroad,
        SupplyLimit,
        ColonialPoints,
        TotalNumOfPorts,
        TotalAmountOfShips,
        NumOfCities,
        IsOverseas,
        MaxNavalBase,
        Money,
        MaximumSpeed,
        BuildTime,
        ColonialPrestige,
        Invention,
        SupplyRange,
        Port,
        HasBuilding,
        DiplomaticPoints,

        // Issues
        MaxTariff,
        MinTariff,
        MaxTax,

        FactoryOwnerCost,
        ImportCost,
        BuildFactory,
        ExpandFactory,
        OpenFactory,
        DestroyFactory,
        BuildRailway,
        FactoryPriority,
        CanSubsidise,
        PopBuildFactory,
        PopExpandFactory,
        PopOpenFactory,
        DeleteFactoryIfNoInput,
        PopBuildFactoryInvest,
        PopExpandFactoryInvest,
        OpenFactoryInvest,
        AllowForeignInvestment,
        BuildRailwayInvest,
        BuildFactoryInvest,
        ExpandFactoryInvest,
        CanInvestInPopProjects,
        GlobalAssimilationRate,
        MinTax,

        MaxMilitarySpending,
        MinMilitarySpending,
        WarExhaustionEffect,
        IsJingoism,
        CbGenerationSpeedModifier,
        MobilizationImpact,
        OrgRegain,
        ReinforceSpeed,

        MaxSocialSpending,
        MinSocialSpending,

        SlaveryAllowed,
        HasCountryFlag,
        Year,
        GlobalImmigrantAttract,
        NextStepOnly,
        RichVote,
        MiddleVote,
        PoorVote,
        RichOnly,
        PopulationVote,
        Election,
        SameAsRulingParty,
        RulingPartySupport,
        StateVote,
        LargestShare,
        Dhont,
        SainteLaque,
        SuppressionPointsModifier,
        IssueChangeSpeed,
        LiteracyConImpact,
        SocialReformDesire,
        MobilisationEconomyImpact,
        LandUnitStartExperience,
        EducationEfficiencyModifier,
        PrimaryCultureVoting,
        NonAcceptedPopConsciousnessModifier,
        NonAcceptedPopMilitancyModifier,
        AllVoting,
        CultureVoting,
        PoorSavingsModifier,
        TaxEfficiency,
        RgoThroughput,
        PoorEverydayNeeds,
        AdministrativeMultiplier,
        CorePopMilitancyModifier,
        IsCultureGroup,
        PrimaryCulture,
        Administrative,
        MinimumWage,
        ArtisanThroughput,
        FactoryMaintenance,
        ArtisanOutput,
        PensionLevel,
        UnemploymentBenefit,
        GlobalPopulationGrowth,
        BadBoy,

        FarmRgoSize,
        MineRgoSize,
        CivilizationProgressModifier,
        TechnologyCost,
        AdministrativeEfficiencyModifier,
        MaxLoanModifier,
        IsSubState,
        MineRgoEff,
        InWholeCapitalState,
        LimitToWorldGreatestLevel,
        Treasury, // Modifies money in finances
        ActivateTechnology,
        SetCountryFlag,
        BuildFactoryInCapitalState,
        CanBuildFactoryInCapitalState,
        ResearchPointsOnConquer,
        LandOrganisation,
        Ai,
        CountryEventProperty, // Fires event
        LandDefenseModifier,
        TariffEfficiencyModifier,
        LeadershipModifier,
        HasCountryModifier,
        RemoveCountryModifier,
        NavalUnitStartExperience,
        NavalAttackModifier,
        NavalDefenseModifier,
        Plurality,

        // Inventions
        Base,
        Siege,
        Reconnaissance,
        GasDefence,
        GasAttack,

        //News, // Dumped.
        NationalValue,
        RulingPartyIdeology,
        AverageConsciousness,
        WarExhaustion,
        CorePopConsciousnessModifier,
        PermanentPrestige,
        IsIdeologyEnabled,
        LoanInterest,
        SharedPrestige,
        Government,
        PoliticalReformDesire,
        AverageMilitancy,

        // ReSharper disable once IdentifierTypo
        Seperatism,
        EnableCrime,
        Faction,
        SoldierToPopLoss,
        LandAttrition,
        PopGrowth,
        HasGlobalFlag,
        Hull,
        GunPower,
        TorpedoAttack,
        NavalAttrition,

        // Units
        // General
        Icon,
        Active,
        UnitType,

        // Core
        Priority,
        MaxStrength,
        WeightedValue,

        // Building + BuildCost
        CanBuildOverseas,
        MinPortLevel,
        LimitPerPort,

        // Supply + SupplyCost
        SupplyConsumptionScore,

        // Ability
        Discipline,
        Maneuver,
        FireRange,
        Evasion,

        BreakCore, // ------------------------------------------------------------------------

        // Tech
        AiChance,
        ArmyBase,
        NavyBase,
        Modifier,
        CapitalScope,
        AnyGreaterPower,
        AnyNeighborCountry,
        AnyNeighborProvince,
        Overlord,
        SphereOwner,
        CulturalUnion,
        RgoGoodsOutput,
        RgoSize,
        FactoryGoodsOutput,
        UnemploymentByType,

        UncivEconomicModifier,
        UncivMilitaryModifier,

        // Issues
        Rules,
        Allow,
        Not,
        Or,
        And,
        OnExecute,
        Effect,
        AnyPop,
        ScaledMilitancy,
        BuildRailwayInCapital,
        CanBuildRailwayInCapital,
        CanBuildFortInCapital,
        BuildFortInCapital,
        Trigger,
        RandomOwned,
        Limit,
        Owner,
        AnyOwnedProvince,

        // Inventions
        Chance,
        WarCountries,
        RebelOrgGain,
        FactoryGoodsThroughput,

        // Events

        // Third Levels
        // Ranges
        CountryEvent,
        ProvinceEvent,
        Random,
        RandomState,
        Ideology,
        AnyState,
        AnyOwned,
        UpperHouse,
        AddProvinceModifier,
        AddCountryModifier,
        DiplomaticInfluence,
        Relation,
        AttackerGoal,
        DefenderGoal,
        DominantIssue,
        From,
        ScaledConsciousness,
        AllCore,
        War,
        AddCasusBelli,
        DefineGeneral,
        SetVariable,
        ChangeVariable,
        CheckVariable,
        AnyCountry,
        RandomCountry,
        CasusBelli,
        Controller,
        RandomProvince,
        RandomNeighborProvince,
        RandomPop,
        AnySubState,
        SubUnit,
        PartyLoyalty,
        RandomEmptyNeighborProvince,
        CanBuildInProvince,
        MoveIssuePercentage,

        // Strata?
        RichStrata,
        MiddleStrata,
        PoorStrata,

        Immediate,
        MeanTimeToHappen,
        Option,
        RandomList,

        BreakNeedsStart,

        // Pop Types
        LifeNeeds,
        EverydayNeeds,
        LuxuryNeeds,

        BreakNeedsEnd,

        CountryMigrationTarget,
        MigrationTarget,
        PromoteTo,
        Ideologies,
        Rebel,
        Issues,
        Country,
        AnyCore,
        This,
        StateScope,
        Group,
        Location,
        WorkAvailable,

        // Units
        BuildCost,
        SupplyCost,

        // Ideologies
        AddPoliticalReform,
        RemovePoliticalReform,
        AddSocialReform,
        RemoveSocialReform,
        AddMilitaryReform, // For uncivs civilizing
        RemoveMilitaryReform,
        AddEconomicReform, // For uncivs civilizing
        RemoveEconomicReform, // Not used?

        // Buildings
        GoodsCost,

        // Countries
        UnitNames,
        Party,
        ForeignInvestment,

        BreakPoliciesStart,

        PartyIssues,
        PoliticalReforms,
        SocialReforms,

        // For uncivs
        EconomicReforms,
        MilitaryReforms,

        BreakPoliciesEnd,

        BreakRange
    }
}