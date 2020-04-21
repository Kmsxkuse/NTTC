﻿using System;
using System.Collections.Generic;
using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Conversion
{
    public struct Map : IComponentData
    {
        public BlittableBool LoadingFinished,
            NamesLoaded,
            ClickLoaded;

        public int2 ColorSize;
    }

    public enum LoadingStages
    {
        Start,
        Map,
        Definitions,
        Terrain,
        States,
        Pixels,
        Borders,
        Continents,
        CountryNames,
        Religions,
        Cultures,
        Ideologies,
        Goods,
        Governments,
        Buildings,
        Units,
        PolicyNames,
        PopTypes,
        TechnologyNames,
        Technologies,
        Inventions,
        EventModifiers,
        NationalValues,
        Countries,
        Crimes,
        Issues,
        Events,
        Histories,
        UserInterface,
        Complete
    }

    [Serializable]
    public struct CacheInfo
    {
        public string borders,
            buildings,
            continents,
            countries,
            countryColors,
            countryHistories,
            crimes,
            cultures,
            definitions,
            eventModifiers,
            goods,
            governments,
            ideologies,
            inventions,
            inventionNames,
            issues,
            nationalValues,
            policyNames,
            popTypes,
            populations,
            provinces,
            religions,
            states,
            technologies,
            terrains,
            units,
            idMap,
            textLabels,
            fileNameChecksum,
            totalChecksum;

        public bool success;

        public CacheInfo(bool throwaway)
        {
            borders = "Borders.json";
            buildings = "Buildings.json";
            continents = "Continents.json";
            countries = "Countries.json";
            countryColors = "CountryColors.json";
            countryHistories = "CountryHistories.json";
            crimes = "Crimes.json";
            cultures = "Cultures.json";
            definitions = "Definitions.json";
            eventModifiers = "EventModifiers.json";
            goods = "Goods.json";
            governments = "Governments.json";
            ideologies = "Ideologies.json";
            inventionNames = "InventionNames.json";
            inventions = "Invention.json";
            issues = "Issues.json";
            nationalValues = "NationalValues.json";
            policyNames = "PolicyNames.json";
            popTypes = "PopTypes.json";
            populations = "Populations.json";
            provinces = "Provinces.json";
            religions = "Religions.json";
            states = "States.json";
            technologies = "Technologies.json";
            terrains = "Terrains.json";
            units = "Units.json";
            idMap = "IdMap.png";
            textLabels = "TextLabels.json";
            fileNameChecksum = "NULL";
            totalChecksum = "NULL";

            success = false;
        }
    }

    public class MapLoad : MonoBehaviour
    {
        public static bool CacheLoad;
        private NativeList<int> _borderList;
        private NativeMultiHashMap<int, int> _borderMiddle, _provTerrains;
        private NativeHashMap<int2, BlittableBool> _borderStart, _borderRemove;
        private CacheInfo _cacheInfo;
        private NativeArray<Color> _colorMap, _terrainMap;
        private EventBox _eventBox;
        private bool _loading = true, _pixelDone, _borderDone;
        private Map _mapComponent;
        private Entity _mapEntity;
        private Material _mapMaterial;
        private NewDataBox _newDataBox;
        private JobHandle _pixelJobHandle, _borderJobHandle, _terrainJobHandle;
        private PoliticalBox _politicalBox;
        private NativeHashMap<int, int> _stateLookup;
        private StringBox _stringBox;
        private TechBox _techBox;
        private NativeArray<int> _terrainCounter;
        private NativeArray<int2> _uniqueBorderPairs;
        private NativeHashMap<Color, int> _uniqueColors, _terrainLookup;

        private void Awake()
        {
            Profiler.enabled = false;

            /*
            _mapEntity = GetComponent<GameObjectEntity>().Entity;
            _em.AddComponent(_mapEntity, typeof(Map));
            _em.AddSharedComponentData(_mapEntity, new DataBox());
            _em.AddSharedComponentData(_mapEntity, new UnitBox());
            _em.AddSharedComponentData(_mapEntity, new TechBox());
            _em.AddSharedComponentData(_mapEntity, new PoliticalBox());
            _em.AddSharedComponentData(_mapEntity, new EventBox());
            _em.AddSharedComponentData(_mapEntity, new StringBox());
            _em.AddSharedComponentData(_mapEntity, new ProvinceTable());

            // Loading localization and passing to LoadMethods.
            LoadMethods.LocalizedReplacer = LocalizationLoad.Main();

            // Loading global variables
            _dataBox = new DataBox();
            _stringBox = new StringBox();
            _mapComponent = new Map();

            // Checking for cached files.
            if (!Directory.Exists(Path.Combine(Application.streamingAssetsPath, "Cache")))
                Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "Cache"));
            if (!File.Exists(Path.Combine(Application.streamingAssetsPath, "Cache/Cache.json")))
                _cacheInfo = new CacheInfo(true);
            else
                _cacheInfo = JsonUtility.FromJson<CacheInfo>(
                    File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache/Cache.json")));

            Debug.Log("Loading start at " + Time.realtimeSinceStartup + " seconds.");

            //StartCoroutine(_cacheInfo.success ? VerifyCache() : LoadFiles());
            */

            Debug.Log("Start: " + Time.realtimeSinceStartup);

            // Loading color maps!
            var colorBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "Map", "provinces.png"));
            var map = new Texture2D(2, 2);
            map.LoadImage(colorBytes);
            _mapComponent.ColorSize = new int2(map.width, map.height);
            _colorMap = new NativeArray<Color>(map.GetPixels(), Allocator.Persistent);

            colorBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "Map", "terrain.png"));
            map.LoadImage(colorBytes);

            if (map.width != _mapComponent.ColorSize.x || map.height != _mapComponent.ColorSize.y)
                throw new Exception("Dimension mismatch between province and terrain textures!");

            _terrainMap = new NativeArray<Color>(map.GetPixels(), Allocator.Persistent);

            //LoadScreen.SetLoadingScreen(LoadingStages.Terrain);

            // REFACTORED
            var (terrains, terrainNames, terrainPalette) = TerrainLoad.Main();
            _stringBox.TerrainNames = terrainNames;

            _terrainLookup = new NativeHashMap<Color, int>(terrainPalette.Count, Allocator.Persistent);
            foreach (var terrainColor in terrainPalette)
                _terrainLookup.TryAdd(terrainColor.Key, terrainColor.Value);

            //LoadScreen.SetLoadingScreen(LoadingStages.Definitions);

            // UNCHANGED
            var definitions = DefinitionsLoad.ReadDefinitions();
            _stringBox.ProvinceNames = definitions.definedNames;
            _uniqueColors = new NativeHashMap<Color, int>(_stringBox.ProvinceNames.Count, Allocator.Persistent);
            _newDataBox.IdIndex = new NativeHashMap<int, int>(_stringBox.ProvinceNames.Count, Allocator.Persistent);

            // Populating idIndexes
            for (var i = 0; i < definitions.idIndexes.Count; i++)
                _newDataBox.IdIndex.TryAdd(definitions.idIndexes[i], i);
            UnifiedVariables.IdIndex = _newDataBox.IdIndex;
            // Populating unique colors
            for (var i = 0; i < definitions.FoundColors.Count; i++)
                _uniqueColors.TryAdd(definitions.FoundColors[i], i);

            //LoadScreen.SetLoadingScreen(LoadingStages.States);

            // REFACTORED
            var (stateProvinces, stateNames) = RegionsLoad.Main(_newDataBox.IdIndex);
            _stringBox.StateNames = stateNames;

            _stateLookup = new NativeHashMap<int, int>(_stringBox.ProvinceNames.Count, Allocator.Persistent);
            foreach (var stateKvP in stateProvinces)
                _stateLookup.TryAdd(stateKvP.x, stateKvP.y);

            //LoadScreen.SetLoadingScreen(LoadingStages.Pixels);

            _newDataBox.IdMap = new NativeArray<Color32>(_colorMap.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _provTerrains = new NativeMultiHashMap<int, int>(_colorMap.Length, Allocator.Persistent);

            _pixelJobHandle = new ProcessPixel
            {
                ColorMap = _colorMap,
                UniqueColors = _uniqueColors,
                StateLookup = _stateLookup,
                IdMap = _newDataBox.IdMap,

                TerrainMap = _terrainMap,
                NumOfTerrains = terrainNames.Count,
                TerrainLookup = _terrainLookup,
                ProvTerrains = _provTerrains.ToConcurrent()
            }.Schedule(_colorMap.Length, 100);

            // Border processing, used in path finding!
            _borderStart = new NativeHashMap<int2, BlittableBool>(_colorMap.Length * 3, Allocator.Persistent);

            _borderJobHandle = new ProcessBorderGetBorders
            {
                BorderIntermediate = _borderStart.ToConcurrent(),
                ColorMap = _colorMap,
                UniqueColors = _uniqueColors,
                Dimensions = _mapComponent.ColorSize
            }.Schedule(_colorMap.Length, 100);

            //LoadScreen.SetLoadingScreen(LoadingStages.Continents);

            // REFACTORED
            var (continents, continentProvinces, continentNames) = ContinentLoad.Main(_newDataBox.IdIndex);
            _stringBox.ContinentNames = continentNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.CountryNames);

            // UNCHANGED
            var (countryNames, countryTags, countryPaths) = CountriesLoad.Names();
            _stringBox.CountryTags = countryTags;
            _stringBox.CountryNames = countryNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Religions);

            // REFACTORED
            var (religions, religionGroups, religionNames, religionGroupNames) = ReligionsLoad.Main();
            _stringBox.ReligionNames = religionNames;
            _stringBox.ReligionGroupNames = religionGroupNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Cultures);

            // REFACTORED
            var (cultures, cultureGroups, cultureNames, cultureGroupNames) = CulturesLoad.Main();
            _stringBox.CultureNames = cultureNames;
            _stringBox.CultureGroupNames = cultureGroupNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Ideologies);

            // REFACTORED
            var (ideologies, ideologyGroups, ideologyNames, ideologyGroupNames) = IdeologiesLoad.Main(ref _stringBox);
            _stringBox.IdeologyNames = ideologyNames;
            _stringBox.IdeologyGroupNames = ideologyGroupNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Governments);

            // REFACTORED
            var (governments, governmentNames) = GovernmentsLoad.Main();
            _stringBox.GovernmentNames = governmentNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Goods);

            // REFACTORED
            var (goods, goodsCategory, goodsNames, goodsCategoryNames) = GoodsLoad.Main();
            _stringBox.GoodsCategory = goodsCategoryNames;
            _stringBox.GoodsNames = goodsNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Buildings);

            // REFACTORED
            var (buildings, buildingNames) = BuildingsLoad.Main();
            _stringBox.BuildingNames = buildingNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Units);

            // REFACTORED
            var (units, unitNames) = UnitsLoad.Main();
            _stringBox.UnitNames = unitNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.PopTypes);

            // REFACTORED, STILL INCOMPLETE (because I'm lazy)
            var (popTypes, popTypeNames) = PopTypesLoad.Main();
            _stringBox.PopTypeNames = popTypeNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.PolicyNames);

            // REFACTORED
            var (issueFileTree, issueValues, subPolicies, policyGroups, subPolicyNames, policyGroupNames)
                = IssuesLoad.PolicyNames();
            _stringBox.PolicyGroupNames = policyGroupNames;
            _stringBox.SubPolicyNames = subPolicyNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Technology);

            // REFACTORED
            var (technologies, schools, folders, incompleteInventions) = TechLoad.Main(ref _stringBox);

            //LoadScreen.SetLoadingScreen(LoadingStages.Crimes);

            // REFACTORED
            var (crimes, crimeNames) = CrimeLoad.Main();
            _stringBox.CrimeNames = crimeNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.EventModifiers);

            // REFACTORED
            var (eventModifiers, eventModifierNames) = EventModifierLoad.Main();
            _stringBox.EventModifierNames = eventModifierNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.NationalValues);

            // REFACTORED, literally identical to Event Modifiers. Merge or generify?
            var (nationalValues, nationalValueNames) = NationalValueLoad.Main();
            _stringBox.NationalValueNames = nationalValueNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Inventions);

            // REFACTORED
            var inventions = InventionsLoad.Main(technologies, ref _stringBox);

            //LoadScreen.SetLoadingScreen(LoadingStages.Issues);

            // REFACTORED
            IssuesLoad.Main(issueFileTree, issueValues, subPolicies, policyGroups);

            //LoadScreen.SetLoadingScreen(LoadingStages.TechnologyNames);

            InventionsLoad.CompleteInventions(technologies, incompleteInventions);

            //LoadScreen.SetLoadingScreen(LoadingStages.Histories);

            var provinces = ProvinceHistoryLoad.Main(_newDataBox.IdIndex);
            ProvincePopulationLoad.Main(provinces, _newDataBox.IdIndex);

            var (countries, tempRulingParty) = CountryHistoryLoad.Main(technologies, inventions, cultures,
                ideologies, subPolicies, governments);

            //LoadScreen.SetLoadingScreen(LoadingStages.Countries);

            // REFACTORED
            var (countryParties, countryPartyNames) = CountriesLoad.Main(countries, countryPaths);
            _stringBox.CountryPartyNames = countryPartyNames;

            _pixelJobHandle.Complete();
            _borderJobHandle.Complete();

            Debug.Log("End: " + Time.realtimeSinceStartup);

            _uniqueColors.Dispose();
            _stateLookup.Dispose();
            _colorMap.Dispose();
            _terrainMap.Dispose();
            _terrainLookup.Dispose();
            _provTerrains.Dispose();
            _borderStart.Dispose();

            throw new Exception("REFACTORING!");
        }

        private void OnDestroy()
        {
            _newDataBox.IdIndex.Dispose();
            _newDataBox.IdMap.Dispose();
        }

        /*
        private IEnumerator LoadFiles()
        {
            _cacheInfo.success = false;

            yield return new WaitForEndOfFrame();

            LoadScreen.SetLoadingScreen(LoadingStages.Start);

            LoadScreen.SetLoadingScreen(LoadingStages.Map);
            yield return null;

            // Loading color maps!
            var colorBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "Map/provinces.png"));
            var map = new Texture2D(2, 2);
            map.LoadImage(colorBytes);
            _mapComponent.ColorSize = new int2(map.width, map.height);
            _colorMap = new NativeArray<Color>(map.GetPixels(), Allocator.Persistent);

            colorBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "Map/terrain.png"));
            map.LoadImage(colorBytes);

            if (map.width != _mapComponent.ColorSize.x || map.height != _mapComponent.ColorSize.y)
                throw new Exception("Dimension mismatch between province and terrain textures!");

            _terrainMap = new NativeArray<Color>(map.GetPixels(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Terrain);
            yield return null;

            // Manually determined color codes since unity cant use Bitmaps
            // Normally, I would read the bitmap in System.Drawing.Bitmap, pull the ColorPalette,
            //     then match them to the indexes found in V2's Terrain.txt. Color = { [Index] }
            // Unity cant do do that, so GIMP manually match.
            var terrains = TerrainLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.terrains),
                JsonUtility.ToJson(terrains));

            _stringBox.TerrainNames = terrains.terrainNames.ToArray();
            _dataBox.Terrains = new NativeArray<TerrainInfo>(terrains.terrains.ToArray(), Allocator.Persistent);
            _dataBox.TerrainActions = new NativeArray<float2>(terrains.terrainActions.ToArray(), Allocator.Persistent);

            _terrainLookup = new NativeHashMap<Color, int>(terrains.TerrainPalette.Count, Allocator.Persistent);
            foreach (var terrainColor in terrains.TerrainPalette)
                _terrainLookup.TryAdd(terrainColor.Key, terrainColor.Value);

            LoadScreen.SetLoadingScreen(LoadingStages.Definitions);
            yield return null;

            // Loading Definitions COMPLETED
            var definitions = DefinitionsLoad.ReadDefinitions();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.definitions),
                JsonUtility.ToJson(definitions));

            _stringBox.ProvinceNames = definitions.definedNames.ToArray();
            _uniqueColors = new NativeHashMap<Color, int>(_stringBox.ProvinceNames.Length, Allocator.Persistent);
            _dataBox.IdIndex = new NativeHashMap<int, int>(_stringBox.ProvinceNames.Length, Allocator.Persistent);

            // Populating idIndexes
            for (var i = 0; i < definitions.idIndexes.Count; i++)
                _dataBox.IdIndex.TryAdd(definitions.idIndexes[i], i);
            // Populating unique colors
            for (var i = 0; i < definitions.FoundColors.Count; i++)
                _uniqueColors.TryAdd(definitions.FoundColors[i], i);

            UnifiedVariables.IdIndex = _dataBox.IdIndex;

            LoadScreen.SetLoadingScreen(LoadingStages.States);
            yield return null;

            // Loading States COMPLETED
            var states = RegionsLoad.Main(_dataBox.IdIndex);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.states),
                JsonUtility.ToJson(states));

            _stringBox.StateNames = states.stateNames.ToArray();
            _dataBox.States = new NativeArray<int2>(states.states.ToArray(), Allocator.Persistent);
            _dataBox.StateProvinces = new NativeArray<int>(states.stateProvinces.ToArray(), Allocator.Persistent);

            _stateLookup = new NativeHashMap<int, int>(_stringBox.ProvinceNames.Length, Allocator.Persistent);
            for (var i = 0; i < states.states.Count; i++)
            {
                var targetState = states.states[i];
                for (var j = targetState.x; j < targetState.y; j++)
                    _stateLookup.TryAdd(states.stateProvinces[j], i);
            }

            LoadScreen.SetLoadingScreen(LoadingStages.Pixels);
            yield return null;

            _dataBox.IdMap = new NativeArray<Color32>(_colorMap.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _provTerrains = new NativeMultiHashMap<int, int>(_colorMap.Length, Allocator.Persistent);

            _pixelJobHandle = new ProcessPixel
            {
                ColorMap = _colorMap,
                UniqueColors = _uniqueColors,
                StateLookup = _stateLookup,
                IdMap = _dataBox.IdMap,

                TerrainMap = _terrainMap,
                NumOfTerrains = terrains.terrains.Count,
                TerrainLookup = _terrainLookup,
                ProvTerrains = _provTerrains.ToConcurrent()
            }.Schedule(_colorMap.Length, 100);

            // Border processing, used in path finding!
            _dataBox.BorderEnds = new NativeArray<int>(_uniqueColors.Length, Allocator.Persistent);
            _borderStart = new NativeHashMap<int2, BlittableBool>(_colorMap.Length * 3, Allocator.Persistent);

            var firstBorderJobHandle = new ProcessBorderGetBorders
            {
                BorderIntermediate = _borderStart.ToConcurrent(),
                ColorMap = _colorMap,
                UniqueColors = _uniqueColors,
                Dimensions = _mapComponent.ColorSize
            }.Schedule(_colorMap.Length, 100);

            LoadScreen.SetLoadingScreen(LoadingStages.Continents);
            yield return null;

            // Loading Continents COMPLETED
            var continents = ContinentLoad.Main(_dataBox.IdIndex);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.continents),
                JsonUtility.ToJson(continents));

            _stringBox.ContinentNames = continents.continentNames.ToArray();
            _dataBox.Continents = new NativeArray<ContinentInfo>(continents.continents.ToArray(), Allocator.Persistent);
            _dataBox.ContinentProvinces =
                new NativeArray<int>(continents.continentProvinces.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.CountryColors);
            yield return null;

            // Loading countries
            var countryColors = CountriesLoad.Colors();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.countryColors),
                JsonUtility.ToJson(countryColors));

            _stringBox.CountryTags = countryColors.countryTags.ToArray();
            _stringBox.CountryNames = countryColors.countries.ToArray();
            LoadScreen.SetLoadingScreen(LoadingStages.Religions);
            yield return null;

            // Loading Religions COMPLETED
            var religions = ReligionsLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.religions),
                JsonUtility.ToJson(religions));

            _dataBox.Religions = new NativeArray<ReligionInfo>(religions.religionArray.ToArray(), Allocator.Persistent);
            _dataBox.ReligionGroups = new NativeArray<int>(religions.groupRanges.ToArray(), Allocator.Persistent);
            _stringBox.ReligionNames = religions.religionNames.ToArray();
            _stringBox.ReligionGroupNames = religions.groupNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Cultures);
            yield return null;

            // Loading Cultures COMPLETED
            var cultures = CulturesLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.cultures),
                JsonUtility.ToJson(cultures));

            _dataBox.Cultures = new NativeArray<CultureInfo>(cultures.cultures.ToArray(), Allocator.Persistent);
            _dataBox.CultureGroups = new NativeArray<int>(cultures.groupRanges.ToArray(), Allocator.Persistent);
            _stringBox.CultureNames = cultures.cultureNames.ToArray();
            _stringBox.CultureGroupNames = cultures.cultureGroupNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Ideologies);
            yield return null;

            // Loading Ideologies COMPLETED
            var ideologies = IdeologiesLoad.Main(_stringBox);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.ideologies),
                JsonUtility.ToJson(ideologies));

            _politicalBox = new PoliticalBox
            {
                Ideologies = new NativeArray<IdeologyInfo>(ideologies.ideologies.ToArray(), Allocator.Persistent),
                IdeologyGroups = new NativeArray<int>(ideologies.ideologyGroups.ToArray(), Allocator.Persistent),
                IdeologyRanges = new NativeArray<int3>(ideologies.ideologyRanges.ToArray(), Allocator.Persistent),
                IdeologyActions = new NativeArray<float2>(ideologies.ideologyActions.ToArray(), Allocator.Persistent)
            };
            _stringBox.IdeologyNames = ideologies.ideologyNames.ToArray();
            _stringBox.IdeologyGroupNames = ideologies.ideologyGroupNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Governments);
            yield return null;

            // Loading Governments COMPLETED
            var governments = GovernmentsLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.governments),
                JsonUtility.ToJson(governments));

            _politicalBox.Governments =
                new NativeArray<GovernmentInfo>(governments.governments.ToArray(), Allocator.Persistent);
            _politicalBox.GovernmentIdeologies =
                new NativeArray<int>(governments.governmentIdeologies.ToArray(), Allocator.Persistent);
            _stringBox.GovernmentNames = governments.governmentNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Goods);
            yield return null;

            // Loading Trade Goods COMPLETED
            var goods = GoodsLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.goods),
                JsonUtility.ToJson(goods));

            _dataBox.Goods = new NativeArray<GoodsInfo>(goods.goods.ToArray(), Allocator.Persistent);
            _stringBox.GoodsCategory = goods.goodsCategory.ToArray();
            _stringBox.GoodsNames = goods.goodsName.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Buildings);
            yield return null;

            // Loading Buildings COMPLETED
            var buildings = BuildingsLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.buildings),
                JsonUtility.ToJson(buildings));

            _stringBox.BuildingNames = buildings.buildingNames.ToArray();
            _dataBox.Buildings = new NativeArray<BuildingInfo>(buildings.buildings.ToArray(), Allocator.Persistent);
            _dataBox.BuildingActions =
                new NativeArray<float2>(buildings.buildingActions.ToArray(), Allocator.Persistent);
            _dataBox.BuildingGoods = new NativeArray<float2>(buildings.buildingGoods.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Units);
            yield return null;

            // Loading Units COMPLETED
            var units = UnitsLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.units),
                JsonUtility.ToJson(units));

            _unitBox = new UnitBox
            {
                Units = new NativeArray<UnitInfo>(units.units.ToArray(), Allocator.Persistent),

                Ability = new NativeArray<float2>(units.ability.ToArray(), Allocator.Persistent),
                General = new NativeArray<float2>(units.general.ToArray(), Allocator.Persistent),
                Core = new NativeArray<float2>(units.core.ToArray(), Allocator.Persistent),
                Build = new NativeArray<float2>(units.build.ToArray(), Allocator.Persistent),
                Supply = new NativeArray<float2>(units.supply.ToArray(), Allocator.Persistent),
                BuildCost = new NativeArray<float2>(units.buildCost.ToArray(), Allocator.Persistent),
                SupplyCost = new NativeArray<float2>(units.supplyCost.ToArray(), Allocator.Persistent)
            };
            _stringBox.UnitNames = units.unitNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.PopTypes);
            yield return null;

            // Loading Population Types. INCOMPLETE FUNCTIONAL
            var popTypes = PopTypesLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.popTypes),
                JsonUtility.ToJson(popTypes));

            _dataBox.PopTypes = new NativeArray<PopTypeInfo>(popTypes.popTypes.ToArray(), Allocator.Persistent);
            _dataBox.PopRebels = new NativeArray<float2>(popTypes.popRebels.ToArray(), Allocator.Persistent);
            _dataBox.NeedsList = new NativeArray<float2>(popTypes.needsList.ToArray(), Allocator.Persistent);
            _stringBox.PopTypeNames = popTypes.popTypeNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.PolicyNames);
            yield return null;

            // Partial policy lookup. COMPETED
            var policyNames = IssuesLoad.PolicyNames();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.policyNames),
                JsonUtility.ToJson(policyNames));

            _politicalBox.IssueGroups = new NativeArray<int>(policyNames.groupRanges.ToArray(), Allocator.Persistent);
            _stringBox.PolicyGroupNames = policyNames.policyGroupNames.ToArray();
            _stringBox.SubPolicyNames = policyNames.subPolicyNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.TechnologyNames);
            yield return null;

            // Loading Techs. COMPLETED
            // Unified dictionary assignment inside tech load.
            var technologies = TechLoad.Main(ref _stringBox);
            _stringBox.TechNames = technologies.technologies.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Crimes);
            yield return null;

            //UnifiedVariables.AssignDictionaries(_stringBox); Might be needed someday.
            var crimes = CrimeLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.crimes),
                JsonUtility.ToJson(crimes));

            _dataBox.Crimes = new NativeArray<int>(crimes.Crimes.ToArray(), Allocator.Persistent);
            _dataBox.CrimeRanges = new NativeArray<int3>(crimes.CrimeRanges.ToArray(), Allocator.Persistent);
            _dataBox.CrimeActions = new NativeArray<float2>(crimes.CrimeActions.ToArray(), Allocator.Persistent);
            _stringBox.CrimeNames = crimes.CrimeNames;

            LoadScreen.SetLoadingScreen(LoadingStages.EventModifiers);
            yield return null;

            var eventModifiers = EventModifierLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.eventModifiers),
                JsonUtility.ToJson(eventModifiers));

            _eventBox = new EventBox
            {
                EventModifiers = new NativeArray<int>(eventModifiers.eventModifiers.ToArray(), Allocator.Persistent),
                EventModifierRanges =
                    new NativeArray<int3>(eventModifiers.eventModifierRanges.ToArray(), Allocator.Persistent),
                EventModifierActions =
                    new NativeArray<float2>(eventModifiers.eventModifierActions.ToArray(), Allocator.Persistent)
            };
            _stringBox.EventModifierNames = eventModifiers.eventModifierNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.NationalValues);
            yield return null;

            var nationalValues = NationalValueLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.nationalValues),
                JsonUtility.ToJson(nationalValues));

            _dataBox.NationalValues =
                new NativeArray<int>(nationalValues.nationalValues.ToArray(), Allocator.Persistent);
            _dataBox.NationalValueRanges =
                new NativeArray<int3>(nationalValues.nationalValueRanges.ToArray(), Allocator.Persistent);
            _dataBox.NationalValueActions =
                new NativeArray<float2>(nationalValues.nationalValueActions.ToArray(), Allocator.Persistent);
            _stringBox.NationalValueNames = nationalValues.nationalValueNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.InventionNames);
            yield return null;

            // Partial inventions loading. COMPLETED
            var inventionNames = InventionsLoad.InventionNames(technologies.technologies.Count);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.inventionNames),
                JsonUtility.ToJson(inventionNames));

            _stringBox.InventionNames = inventionNames.inventions.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Countries);
            yield return null;

            // Completing Countries COMPLETED
            var countries = CountriesLoad.Main(countryColors.CountryPaths);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.countries),
                JsonUtility.ToJson(countries));

            _politicalBox.CountryRanges =
                new NativeArray<int3>(countries.countryRanges.ToArray(), Allocator.Persistent);
            _politicalBox.CountryActions =
                new NativeArray<float2>(countries.countryActions.ToArray(), Allocator.Persistent);
            _politicalBox.CountryIndices =
                new NativeArray<int>(countries.countryIndices.ToArray(), Allocator.Persistent);
            _politicalBox.CountryColors =
                new NativeArray<Color32>(countries.countryColors.ToArray(), Allocator.Persistent);
            _politicalBox.CountryGovernColors =
                new NativeArray<Color32>(countries.countryGovernColors.ToArray(), Allocator.Persistent);
            _stringBox.CountryPartyNames = countries.countryPartyNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Technologies);
            yield return null;

            // Finish parsing technology (applying inventions) here. COMPLETED
            TechLoad.CompleteTechInventions(ref technologies.techActions, technologies.FinishInventions);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.technologies),
                JsonUtility.ToJson(technologies));

            _techBox = new TechBox
            {
                TechInventions = new NativeArray<int>(inventionNames.techBlocks.ToArray(), Allocator.Persistent),

                Technologies = new NativeArray<TechInfo>(technologies.techInfos.ToArray(), Allocator.Persistent),

                FolderRanges = new NativeArray<int2>(technologies.folderRanges.ToArray(), Allocator.Persistent),
                SchoolRanges = new NativeArray<int2>(technologies.schoolRanges.ToArray(), Allocator.Persistent),

                TechRanges = new NativeArray<int3>(technologies.techRanges.ToArray(), Allocator.Persistent),

                SchoolActions = new NativeArray<float2>(technologies.schoolActions.ToArray(), Allocator.Persistent),
                TechActions = new NativeArray<float2>(technologies.techActions.ToArray(), Allocator.Persistent)
            };

            LoadScreen.SetLoadingScreen(LoadingStages.Issues);
            yield return null;

            // Finishing issues parsing. INCOMPLETE. Needs event_modifiers.txt parsing.
            var issues = IssuesLoad.Main(policyNames.FileTree, policyNames.Values);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.issues),
                JsonUtility.ToJson(issues));

            _politicalBox.IssueRanges = new NativeArray<int3>(issues.issueRanges.ToArray(), Allocator.Persistent);
            _politicalBox.IssueActions = new NativeArray<float2>(issues.issueActions.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Inventions);
            yield return null;

            //Debug.Log("Issues/Policies loaded in " + Time.realtimeSinceStartup);

            // Finishing inventions parsing
            var inventions = InventionsLoad.Main(inventionNames.FileTreeIndexCorrector, inventionNames.FileTree,
                inventionNames.Values);

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.inventions),
                JsonUtility.ToJson(inventions));

            _techBox.Inventions = new NativeArray<int>(inventions.inventIndices.ToArray(), Allocator.Persistent);
            _techBox.InventionRanges = new NativeArray<int3>(inventions.inventRanges.ToArray(), Allocator.Persistent);
            _techBox.InventionActions =
                new NativeArray<float2>(inventions.inventActions.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Histories);
            yield return null;

            // Populating provinces. REWRITE!
            var provinces = ProvinceHistoryLoad.LoadProvinceHistory(_dataBox.IdIndex, _stringBox.ProvinceNames.Length);
            _dataBox.Provinces = new NativeArray<ProvinceInfo>(provinces.ToArray(), Allocator.Persistent);

            var population = ProvinceHistoryLoad.LoadPopulation();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.populations),
                JsonUtility.ToJson(population));

            _dataBox.PopList = new NativeArray<ProvPopInfo>(population.popList.ToArray(), Allocator.Persistent);
            _dataBox.PopLookup = new NativeMultiHashMap<int, int>(1, Allocator.Persistent);
            foreach (var popPair in population.popLookup)
                _dataBox.PopLookup.Add(popPair.x, popPair.y);

            var countryHistories = CountryHistoryLoad.Main();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.countryHistories),
                JsonUtility.ToJson(countryHistories));

            _politicalBox.CountryHistories =
                new NativeArray<CountryInfo>(countryHistories.countryHistories.ToArray(), Allocator.Persistent);
            _politicalBox.CountryHistoryRanges = new NativeArray<int3>(countryHistories.countryHistoryRanges.ToArray(),
                Allocator.Persistent);
            _politicalBox.CountryHistoryActions =
                new NativeArray<float2>(countryHistories.countryHistoryActions.ToArray(), Allocator.Persistent);
            _politicalBox.CountryPolicies =
                new NativeArray<int>(countryHistories.countryPolicies.ToArray(), Allocator.Persistent);
            _politicalBox.CountryUpperHouses =
                new NativeArray<float>(countryHistories.countryUpperHouses.ToArray(), Allocator.Persistent);
            _politicalBox.CountryInventions = new NativeMultiHashMap<int, int>(1, Allocator.Persistent);
            _politicalBox.CountryTechnologies = new NativeMultiHashMap<int, int>(1, Allocator.Persistent);

            foreach (var invention in countryHistories.countryInventions)
                _politicalBox.CountryInventions.Add(invention.x, invention.y);
            foreach (var technology in countryHistories.countryTechnologies)
                _politicalBox.CountryTechnologies.Add(technology.x, technology.y);

            LoadScreen.SetLoadingScreen(LoadingStages.Events);
            yield return null;

            //Debug.Log("Provinces loaded in " + Time.realtimeSinceStartup);

            // Loading events INCOMPLETE NOT FUNCTIONAL
            try
            {
                //EventsLoad.Main();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            LoadScreen.SetLoadingScreen(LoadingStages.Borders);
            yield return null;

            firstBorderJobHandle.Complete();

            // Getting custom borders
            var adjacencies = AdjacenciesLoad.Main();

            _borderMiddle = new NativeMultiHashMap<int, int>(
                _borderStart.Length + adjacencies.validConnections.Count, Allocator.Persistent);
            _borderRemove = new NativeHashMap<int2, BlittableBool>(adjacencies.impassables.Count, Allocator.Persistent);

            foreach (var connection in adjacencies.validConnections)
                _borderMiddle.Add(connection.x, connection.y);
            foreach (var impassable in adjacencies.impassables)
                _borderRemove.TryAdd(impassable, true);

            _uniqueBorderPairs = _borderStart.GetKeyArray(Allocator.Persistent);
            _borderList = new NativeList<int>(Allocator.Persistent);

            var secondBorderJobHandle = new ProcessBorderSortRanges
            {
                UniqueBorderPairs = _uniqueBorderPairs,
                SortedBorders = _borderMiddle.ToConcurrent()
            }.Schedule(_borderStart.Length, 10);

            _borderJobHandle = new ProcessBorderOutput
            {
                SortedBorders = _borderMiddle,
                MaxProvinces = _dataBox.Provinces.Length,
                BorderRemove = _borderRemove,
                BorderIndices = _borderList,
                BorderEnds = _dataBox.BorderEnds
            }.Schedule(secondBorderJobHandle);

            _loading = false;

            LoadScreen.SetLoadingScreen(LoadingStages.UserInterface);
            yield return null;
        }

        private IEnumerator VerifyCache()
        {
            // Removing cache file if existing for check sum generation
            File.Delete(Path.Combine(Application.streamingAssetsPath, "Cache/Cache.json"));
            yield return null;

            // Checking if the cache files ever changed since last time cache generation was ran.
            StartCoroutine(FileNameCheckSum().Equals(_cacheInfo.fileNameChecksum, StringComparison.OrdinalIgnoreCase) &&
                           Sha512CheckSum().Equals(_cacheInfo.totalChecksum, StringComparison.OrdinalIgnoreCase)
                ? LoadCache()
                : LoadFiles());
        }

        private IEnumerator LoadCache()
        {
            CacheLoad = true;

            yield return new WaitForEndOfFrame();

            LoadScreen.SetLoadingScreen(LoadingStages.Start);

            // Unused.
            LoadScreen.SetLoadingScreen(LoadingStages.Pixels);
            LoadScreen.SetLoadingScreen(LoadingStages.TechnologyNames);

            LoadScreen.SetLoadingScreen(LoadingStages.Map);
            yield return null;

            var colorBytes =
                File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.idMap));
            var idMap = new Texture2D(2, 2);
            idMap.LoadImage(colorBytes);
            _dataBox.IdMap = new NativeArray<Color32>(idMap.GetPixels32(), Allocator.Persistent);
            _mapComponent.ColorSize = new int2(idMap.width, idMap.height);

            LoadScreen.SetLoadingScreen(LoadingStages.Definitions);
            yield return null;

            var definitions = JsonUtility.FromJson<DefinitionsLoad.DefinitionOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.definitions)));

            _stringBox.ProvinceNames = definitions.definedNames.ToArray();
            _dataBox.IdIndex = new NativeHashMap<int, int>(_stringBox.ProvinceNames.Length, Allocator.Persistent);

            for (var i = 0; i < definitions.idIndexes.Count; i++)
                _dataBox.IdIndex.TryAdd(definitions.idIndexes[i], i);

            LoadScreen.SetLoadingScreen(LoadingStages.Terrain);
            yield return null;

            var terrains = JsonUtility.FromJson<TerrainLoad.TerrainOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.terrains)));

            _stringBox.TerrainNames = terrains.terrainNames.ToArray();
            _dataBox.Terrains = new NativeArray<TerrainInfo>(terrains.terrains.ToArray(), Allocator.Persistent);
            _dataBox.TerrainActions = new NativeArray<float2>(terrains.terrainActions.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.States);
            yield return null;

            // Loading States COMPLETED
            var states = JsonUtility.FromJson<RegionsLoad.StatesOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.states)));

            _stringBox.StateNames = states.stateNames.ToArray();
            _dataBox.States = new NativeArray<int2>(states.states.ToArray(), Allocator.Persistent);
            _dataBox.StateProvinces = new NativeArray<int>(states.stateProvinces.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Continents);
            yield return null;

            // Loading Continents COMPLETED
            var continents = JsonUtility.FromJson<ContinentLoad.ContinentOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.continents)));

            _stringBox.ContinentNames = continents.continentNames.ToArray();
            _dataBox.Continents = new NativeArray<ContinentInfo>(continents.continents.ToArray(), Allocator.Persistent);
            _dataBox.ContinentProvinces =
                new NativeArray<int>(continents.continentProvinces.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.CountryColors);
            yield return null;

            // Loading countries
            var countryColors = JsonUtility.FromJson<CountriesLoad.CountryColorOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.countryColors)));

            _stringBox.CountryTags = countryColors.countryTags.ToArray();
            _stringBox.CountryNames = countryColors.countries.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Religions);
            yield return null;

            // Loading Religions COMPLETED
            var religions = JsonUtility.FromJson<ReligionsLoad.ReligionOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.religions)));

            _dataBox.Religions = new NativeArray<ReligionInfo>(religions.religionArray.ToArray(), Allocator.Persistent);
            _dataBox.ReligionGroups = new NativeArray<int>(religions.groupRanges.ToArray(), Allocator.Persistent);
            _stringBox.ReligionNames = religions.religionNames.ToArray();
            _stringBox.ReligionGroupNames = religions.groupNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Cultures);
            yield return null;

            // Loading Cultures COMPLETED
            var cultures = JsonUtility.FromJson<CulturesLoad.CulturesOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.cultures)));

            _dataBox.Cultures = new NativeArray<CultureInfo>(cultures.cultures.ToArray(), Allocator.Persistent);
            _dataBox.CultureGroups = new NativeArray<int>(cultures.groupRanges.ToArray(), Allocator.Persistent);
            _stringBox.CultureNames = cultures.cultureNames.ToArray();
            _stringBox.CultureGroupNames = cultures.cultureGroupNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Ideologies);
            yield return null;

            // Loading Ideologies COMPLETED
            var ideologies = JsonUtility.FromJson<IdeologiesLoad.IdeologiesOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.ideologies)));

            _politicalBox = new PoliticalBox
            {
                Ideologies = new NativeArray<IdeologyInfo>(ideologies.ideologies.ToArray(), Allocator.Persistent),
                IdeologyGroups = new NativeArray<int>(ideologies.ideologyGroups.ToArray(), Allocator.Persistent),
                IdeologyRanges = new NativeArray<int3>(ideologies.ideologyRanges.ToArray(), Allocator.Persistent),
                IdeologyActions = new NativeArray<float2>(ideologies.ideologyActions.ToArray(), Allocator.Persistent)
            };
            _stringBox.IdeologyNames = ideologies.ideologyNames.ToArray();
            _stringBox.IdeologyGroupNames = ideologies.ideologyGroupNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Governments);
            yield return null;

            // Loading Governments COMPLETED
            var governments = JsonUtility.FromJson<GovernmentsLoad.GovernmentOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.governments)));

            _politicalBox.Governments =
                new NativeArray<GovernmentInfo>(governments.governments.ToArray(), Allocator.Persistent);
            _politicalBox.GovernmentIdeologies =
                new NativeArray<int>(governments.governmentIdeologies.ToArray(), Allocator.Persistent);
            _stringBox.GovernmentNames = governments.governmentNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Goods);
            yield return null;

            // Loading Trade Goods COMPLETED
            var goods = JsonUtility.FromJson<GoodsLoad.GoodsOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.goods)));

            _dataBox.Goods = new NativeArray<GoodsInfo>(goods.goods.ToArray(), Allocator.Persistent);
            _stringBox.GoodsCategory = goods.goodsCategory.ToArray();
            _stringBox.GoodsNames = goods.goodsName.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Buildings);
            yield return null;

            // Loading Buildings COMPLETED
            var buildings = JsonUtility.FromJson<BuildingsLoad.BuildingsOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.buildings)));

            _stringBox.BuildingNames = buildings.buildingNames.ToArray();
            _dataBox.Buildings = new NativeArray<BuildingInfo>(buildings.buildings.ToArray(), Allocator.Persistent);
            _dataBox.BuildingActions =
                new NativeArray<float2>(buildings.buildingActions.ToArray(), Allocator.Persistent);
            _dataBox.BuildingGoods = new NativeArray<float2>(buildings.buildingGoods.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Units);
            yield return null;

            // Loading Units COMPLETED
            var units = JsonUtility.FromJson<UnitsLoad.UnitsOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.units)));

            _unitBox = new UnitBox
            {
                Units = new NativeArray<UnitInfo>(units.units.ToArray(), Allocator.Persistent),

                Ability = new NativeArray<float2>(units.ability.ToArray(), Allocator.Persistent),
                General = new NativeArray<float2>(units.general.ToArray(), Allocator.Persistent),
                Core = new NativeArray<float2>(units.core.ToArray(), Allocator.Persistent),
                Build = new NativeArray<float2>(units.build.ToArray(), Allocator.Persistent),
                Supply = new NativeArray<float2>(units.supply.ToArray(), Allocator.Persistent),
                BuildCost = new NativeArray<float2>(units.buildCost.ToArray(), Allocator.Persistent),
                SupplyCost = new NativeArray<float2>(units.supplyCost.ToArray(), Allocator.Persistent)
            };
            _stringBox.UnitNames = units.unitNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.PopTypes);
            yield return null;

            // Loading Population Types. INCOMPLETE FUNCTIONAL
            var popTypes = JsonUtility.FromJson<PopTypesLoad.PopOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.popTypes)));

            _dataBox.PopTypes = new NativeArray<PopTypeInfo>(popTypes.popTypes.ToArray(), Allocator.Persistent);
            _dataBox.PopRebels = new NativeArray<float2>(popTypes.popRebels.ToArray(), Allocator.Persistent);
            _dataBox.NeedsList = new NativeArray<float2>(popTypes.needsList.ToArray(), Allocator.Persistent);
            _stringBox.PopTypeNames = popTypes.popTypeNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.PolicyNames);
            yield return null;

            // Partial policy lookup. COMPETED
            var policyNames = JsonUtility.FromJson<IssuesLoad.PolicyNameOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.policyNames)));

            _politicalBox.IssueGroups = new NativeArray<int>(policyNames.groupRanges.ToArray(), Allocator.Persistent);
            _stringBox.PolicyGroupNames = policyNames.policyGroupNames.ToArray();
            _stringBox.SubPolicyNames = policyNames.subPolicyNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Crimes);
            yield return null;

            //UnifiedVariables.AssignDictionaries(_stringBox); Might be needed someday.
            var crimes = JsonUtility.FromJson<CrimeLoad.CrimeOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.crimes)));

            _dataBox.Crimes = new NativeArray<int>(crimes.Crimes.ToArray(), Allocator.Persistent);
            _dataBox.CrimeRanges = new NativeArray<int3>(crimes.CrimeRanges.ToArray(), Allocator.Persistent);
            _dataBox.CrimeActions = new NativeArray<float2>(crimes.CrimeActions.ToArray(), Allocator.Persistent);
            _stringBox.CrimeNames = crimes.CrimeNames;

            LoadScreen.SetLoadingScreen(LoadingStages.EventModifiers);
            yield return null;

            var eventModifiers = JsonUtility.FromJson<EventModifierLoad.EventModifierOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.eventModifiers)));

            _eventBox = new EventBox
            {
                EventModifiers = new NativeArray<int>(eventModifiers.eventModifiers.ToArray(), Allocator.Persistent),
                EventModifierRanges =
                    new NativeArray<int3>(eventModifiers.eventModifierRanges.ToArray(), Allocator.Persistent),
                EventModifierActions =
                    new NativeArray<float2>(eventModifiers.eventModifierActions.ToArray(), Allocator.Persistent)
            };
            _stringBox.EventModifierNames = eventModifiers.eventModifierNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.NationalValues);
            yield return null;

            var nationalValues = JsonUtility.FromJson<NationalValueLoad.NationalValueOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.nationalValues)));

            _dataBox.NationalValues =
                new NativeArray<int>(nationalValues.nationalValues.ToArray(), Allocator.Persistent);
            _dataBox.NationalValueRanges =
                new NativeArray<int3>(nationalValues.nationalValueRanges.ToArray(), Allocator.Persistent);
            _dataBox.NationalValueActions =
                new NativeArray<float2>(nationalValues.nationalValueActions.ToArray(), Allocator.Persistent);
            _stringBox.NationalValueNames = nationalValues.nationalValueNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.InventionNames);
            yield return null;

            // Partial inventions loading. COMPLETED
            var inventionNames = JsonUtility.FromJson<InventionsLoad.InventionNamesOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.inventionNames)));

            _stringBox.InventionNames = inventionNames.inventions.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Countries);
            yield return null;

            // Completing Countries COMPLETED
            var countries = JsonUtility.FromJson<CountriesLoad.CountryOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.countries)));

            _politicalBox.CountryRanges =
                new NativeArray<int3>(countries.countryRanges.ToArray(), Allocator.Persistent);
            _politicalBox.CountryActions =
                new NativeArray<float2>(countries.countryActions.ToArray(), Allocator.Persistent);
            _politicalBox.CountryIndices =
                new NativeArray<int>(countries.countryIndices.ToArray(), Allocator.Persistent);
            _politicalBox.CountryColors =
                new NativeArray<Color32>(countries.countryColors.ToArray(), Allocator.Persistent);
            _politicalBox.CountryGovernColors =
                new NativeArray<Color32>(countries.countryGovernColors.ToArray(), Allocator.Persistent);
            _stringBox.CountryPartyNames = countries.countryPartyNames.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Technologies);
            yield return null;

            var technologies = JsonUtility.FromJson<TechLoad.TechOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.technologies)));

            _techBox = new TechBox
            {
                TechInventions = new NativeArray<int>(inventionNames.techBlocks.ToArray(), Allocator.Persistent),

                Technologies = new NativeArray<TechInfo>(technologies.techInfos.ToArray(), Allocator.Persistent),

                FolderRanges = new NativeArray<int2>(technologies.folderRanges.ToArray(), Allocator.Persistent),
                SchoolRanges = new NativeArray<int2>(technologies.schoolRanges.ToArray(), Allocator.Persistent),

                TechRanges = new NativeArray<int3>(technologies.techRanges.ToArray(), Allocator.Persistent),

                SchoolActions = new NativeArray<float2>(technologies.schoolActions.ToArray(), Allocator.Persistent),
                TechActions = new NativeArray<float2>(technologies.techActions.ToArray(), Allocator.Persistent)
            };
            _stringBox.TechNames = technologies.technologies.ToArray();
            _stringBox.SchoolNames = technologies.schools.ToArray();
            _stringBox.FolderNames = technologies.folders.ToArray();
            _stringBox.AreaNames = technologies.areas.ToArray();

            LoadScreen.SetLoadingScreen(LoadingStages.Issues);
            yield return null;

            // Finishing issues parsing. INCOMPLETE. Needs event_modifiers.txt parsing.
            var issues = JsonUtility.FromJson<IssuesLoad.IssuesOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.issues)));

            _politicalBox.IssueRanges = new NativeArray<int3>(issues.issueRanges.ToArray(), Allocator.Persistent);
            _politicalBox.IssueActions = new NativeArray<float2>(issues.issueActions.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Inventions);
            yield return null;

            // Finishing inventions parsing
            var inventions = JsonUtility.FromJson<InventionsLoad.InventionsOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.inventions)));

            _techBox.Inventions = new NativeArray<int>(inventions.inventIndices.ToArray(), Allocator.Persistent);
            _techBox.InventionRanges = new NativeArray<int3>(inventions.inventRanges.ToArray(), Allocator.Persistent);
            _techBox.InventionActions =
                new NativeArray<float2>(inventions.inventActions.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Histories);
            yield return null;

            var provinces = JsonHelper.FromJson<ProvinceInfo>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.provinces)));

            _dataBox.Provinces = new NativeArray<ProvinceInfo>(provinces.ToArray(), Allocator.Persistent);

            var population = JsonUtility.FromJson<ProvinceHistoryLoad.PopulationOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.populations)));

            _dataBox.PopList = new NativeArray<ProvPopInfo>(population.popList.ToArray(), Allocator.Persistent);
            _dataBox.PopLookup = new NativeMultiHashMap<int, int>(1, Allocator.Persistent);
            foreach (var popPair in population.popLookup)
                _dataBox.PopLookup.Add(popPair.x, popPair.y);

            var countryHistories = JsonUtility.FromJson<CountryHistoryOutput>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.countryHistories)));

            _politicalBox.CountryHistories =
                new NativeArray<CountryInfo>(countryHistories.countryHistories.ToArray(), Allocator.Persistent);
            _politicalBox.CountryHistoryRanges = new NativeArray<int3>(countryHistories.countryHistoryRanges.ToArray(),
                Allocator.Persistent);
            _politicalBox.CountryHistoryActions =
                new NativeArray<float2>(countryHistories.countryHistoryActions.ToArray(), Allocator.Persistent);
            _politicalBox.CountryPolicies =
                new NativeArray<int>(countryHistories.countryPolicies.ToArray(), Allocator.Persistent);
            _politicalBox.CountryUpperHouses =
                new NativeArray<float>(countryHistories.countryUpperHouses.ToArray(), Allocator.Persistent);
            _politicalBox.CountryInventions = new NativeMultiHashMap<int, int>(1, Allocator.Persistent);
            _politicalBox.CountryTechnologies = new NativeMultiHashMap<int, int>(1, Allocator.Persistent);

            foreach (var invention in countryHistories.countryInventions)
                _politicalBox.CountryInventions.Add(invention.x, invention.y);
            foreach (var technology in countryHistories.countryTechnologies)
                _politicalBox.CountryTechnologies.Add(technology.x, technology.y);

            LoadScreen.SetLoadingScreen(LoadingStages.Borders);
            yield return null;

            var borders = JsonUtility.FromJson<BorderCache>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.borders)));

            _dataBox.BorderEnds = new NativeArray<int>(borders.borderEnds.ToArray(), Allocator.Persistent);
            _dataBox.BorderIndices = new NativeArray<int>(borders.borderIndices.ToArray(), Allocator.Persistent);

            LoadScreen.SetLoadingScreen(LoadingStages.Events);
            yield return null;

            // skipped

            _loading = false;
            LoadScreen.SetLoadingScreen(LoadingStages.UserInterface);
            yield return null;
        }

        private void Update()
        {
            if (_mapComponent.LoadingFinished || _loading)
                return;

            while (!_cacheInfo.success && FinishLoadingFiles())
                return;

            var provLookup = new float4[_dataBox.Provinces.Length];
            for (var counter = 0; counter < _dataBox.Provinces.Length; counter++)
            {
                var prov = _dataBox.Provinces[counter];
                var color = (Color) _politicalBox.CountryColors[prov.owner];
                provLookup[counter] = new float4(color.r, color.g, color.b, color.a);
            }

            // Creating map entity
            _em.SetSharedComponentData(_mapEntity, _dataBox);
            _em.SetSharedComponentData(_mapEntity, _unitBox);
            _em.SetSharedComponentData(_mapEntity, _techBox);
            _em.SetSharedComponentData(_mapEntity, _politicalBox);
            _em.SetSharedComponentData(_mapEntity, _eventBox);
            _em.SetSharedComponentData(_mapEntity, _stringBox);
            _em.SetSharedComponentData(_mapEntity, new ProvinceTable {ProvLookup = provLookup});

            // Initializing screen switcher
            TopBarEventSystem.SwitchWindow(0);

            UnifiedVariables.DisposeDictionaries(ref _stringBox);
            LoadMethods.DisposeLocalization();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache/Cache.json"),
                JsonUtility.ToJson(_cacheInfo));

            _mapComponent.LoadingFinished = true;
            _em.SetComponentData(_mapEntity, _mapComponent);

            StartCoroutine(CompletionChecker());
        }

        private bool FinishLoadingFiles()
        {
            if (!_pixelDone && _pixelJobHandle.IsCompleted)
            {
                _pixelDone = true;
                _pixelJobHandle.Complete();

                _colorMap.Dispose();
                _uniqueColors.Dispose();
                _stateLookup.Dispose();

                _terrainMap.Dispose();
                _terrainLookup.Dispose();
                _terrainCounter = new NativeArray<int>(_dataBox.Provinces.Length * _dataBox.Terrains.Length,
                    Allocator.Persistent);

                _terrainJobHandle = new CountTerrains
                {
                    ProvinceTerrain = _provTerrains,
                    TerrainCounter = _terrainCounter
                }.Schedule(_dataBox.Provinces.Length, 10);

                _terrainJobHandle = new AssignTerrains
                {
                    Provinces = _dataBox.Provinces,
                    NumOfTerrains = _dataBox.Terrains.Length,
                    TerrainCounter = _terrainCounter
                }.Schedule(_dataBox.Provinces.Length, 10, _terrainJobHandle);

                // Caching idMap
                var idMap = new Texture2D(_mapComponent.ColorSize.x, _mapComponent.ColorSize.y, TextureFormat.RGBA32,
                    false)
                {
                    filterMode = FilterMode.Point
                };
                idMap.SetPixels32(_dataBox.IdMap.ToArray());
                idMap.Apply();
                File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.idMap),
                    idMap.EncodeToPNG());

                return true;
            }

            if (!_borderDone && _borderJobHandle.IsCompleted)
            {
                _borderDone = true;
                _borderJobHandle.Complete();

                _borderStart.Dispose();
                _borderMiddle.Dispose();
                _borderRemove.Dispose();
                _uniqueBorderPairs.Dispose();

                _dataBox.BorderIndices = _borderList.ToArray(Allocator.Persistent);
                _borderList.Dispose();

                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.borders),
                    JsonUtility.ToJson(new BorderCache
                    {
                        borderEnds = _dataBox.BorderEnds.ToList(),
                        borderIndices = _dataBox.BorderIndices.ToList()
                    }));

                return true;
            }

            if (!_pixelDone || !_borderDone || !_terrainJobHandle.IsCompleted)
                return true;

            _terrainJobHandle.Complete();
            _provTerrains.Dispose();
            _terrainCounter.Dispose();

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.provinces),
                JsonHelper.ToJson(_dataBox.Provinces.ToArray()));

            return false;
        }

        private IEnumerator CompletionChecker()
        {
            var errorCounter = 0;
            while (!_mapComponent.NamesLoaded || !_mapComponent.ClickLoaded)
            {
                _mapComponent = _em.GetComponentData<Map>(_mapEntity);

                if (errorCounter++ > 200)
                    throw new Exception("Loading stalled!");

                yield return null;
            }

            yield return null;

            _dataBox.IdMap.Dispose();
            _dataBox.IdIndex.Dispose(); // Disposes the one in Unified Variables as well.

            if (!_cacheInfo.success)
            {
                File.Delete(Path.Combine(Application.streamingAssetsPath, "Cache/Cache.json"));

                // Generate new SHA512 check sums for cache
                _cacheInfo.fileNameChecksum = FileNameCheckSum();
                _cacheInfo.totalChecksum = Sha512CheckSum();
            }

            _cacheInfo.success = true;

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache/Cache.json"),
                JsonUtility.ToJson(_cacheInfo));

            Debug.Log("Loading complete in:" + Time.realtimeSinceStartup + " seconds.");
            LoadScreen.SetLoadingScreen(LoadingStages.Complete);
        }

        private static string FileNameCheckSum()
        {
            var cacheFiles = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "Cache"), "*.*")
                .Where(s => s.EndsWith(".json") || s.EndsWith(".png")).OrderBy(str => str);

            var currentLowerHash = new List<byte>();

            using (var sha512 = SHA512.Create())
            {
                foreach (var fInfo in cacheFiles)
                    currentLowerHash.AddRange(sha512.ComputeHash(Encoding.Default.GetBytes(fInfo)));
            }

            return ConvertHashToString(currentLowerHash);
        }

        private static string Sha512CheckSum()
        {
            var cacheFiles = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "Cache"), "*.*")
                .Where(s => s.EndsWith(".json") || s.EndsWith(".png")).OrderBy(str => str);

            var currentLowerHash = new List<byte>();

            // Getting the sha hashes for each file
            using (var sha512 = SHA512.Create())
            {
                foreach (var fInfo in cacheFiles)
                {
                    var fileStream = new FileStream(fInfo, FileMode.Open, FileAccess.Read)
                    {
                        Position = 0
                    };
                    currentLowerHash.AddRange(sha512.ComputeHash(fileStream));
                    fileStream.Close();
                }
            }

            return ConvertHashToString(currentLowerHash);
        }

        private static string ConvertHashToString(in List<byte> currentLowerHash)
        {
            byte[] totalHash;
            using (var sha512 = SHA512.Create())
                // Getting total hash
            {
                totalHash = sha512.ComputeHash(currentLowerHash.ToArray());
            }

            var sBuilder = new StringBuilder();
            foreach (var character in totalHash)
                sBuilder.Append(character.ToString("X"));

            return sBuilder.ToString();
        }

        private void OnDestroy()
        {
            //_dataBox.IdMap.Dispose(); Disposed in Camera bootstrap on 3rd frame.
            //_dataBox.IdIndex.Dispose(); Disposed in UnifiedVariables
            _dataBox.Provinces.Dispose();
            _dataBox.BorderEnds.Dispose();
            _dataBox.BorderIndices.Dispose();
            _dataBox.States.Dispose();
            _dataBox.StateProvinces.Dispose();
            _dataBox.Goods.Dispose();
            _dataBox.PopTypes.Dispose();
            _dataBox.PopRebels.Dispose();
            _dataBox.NeedsList.Dispose();
            _dataBox.PopList.Dispose();
            _dataBox.PopLookup.Dispose();
            _dataBox.Religions.Dispose();
            _dataBox.ReligionGroups.Dispose();
            _dataBox.Cultures.Dispose();
            _dataBox.CultureGroups.Dispose();
            _dataBox.Continents.Dispose();
            _dataBox.ContinentProvinces.Dispose();
            _dataBox.Buildings.Dispose();
            _dataBox.BuildingActions.Dispose();
            _dataBox.BuildingGoods.Dispose();
            _dataBox.Crimes.Dispose();
            _dataBox.CrimeActions.Dispose();
            _dataBox.CrimeRanges.Dispose();
            _dataBox.NationalValues.Dispose();
            _dataBox.NationalValueActions.Dispose();
            _dataBox.NationalValueRanges.Dispose();
            _dataBox.Terrains.Dispose();
            _dataBox.TerrainActions.Dispose();

            _eventBox.EventModifiers.Dispose();
            _eventBox.EventModifierActions.Dispose();
            _eventBox.EventModifierRanges.Dispose();

            _politicalBox.Ideologies.Dispose();
            _politicalBox.IdeologyGroups.Dispose();
            _politicalBox.IdeologyRanges.Dispose();
            _politicalBox.IdeologyActions.Dispose();
            _politicalBox.Governments.Dispose();
            _politicalBox.GovernmentIdeologies.Dispose();
            _politicalBox.CountryRanges.Dispose();
            _politicalBox.CountryActions.Dispose();
            _politicalBox.CountryIndices.Dispose();
            _politicalBox.CountryColors.Dispose();
            _politicalBox.CountryGovernColors.Dispose();
            _politicalBox.IssueGroups.Dispose();
            _politicalBox.IssueRanges.Dispose();
            _politicalBox.IssueActions.Dispose();
            _politicalBox.CountryHistories.Dispose();
            _politicalBox.CountryHistoryRanges.Dispose();
            _politicalBox.CountryHistoryActions.Dispose();
            _politicalBox.CountryTechnologies.Dispose();
            _politicalBox.CountryInventions.Dispose();
            _politicalBox.CountryPolicies.Dispose();
            _politicalBox.CountryUpperHouses.Dispose();

            _unitBox.Units.Dispose();
            _unitBox.Ability.Dispose();
            _unitBox.Build.Dispose();
            _unitBox.Core.Dispose();
            _unitBox.General.Dispose();
            _unitBox.Supply.Dispose();
            _unitBox.BuildCost.Dispose();
            _unitBox.SupplyCost.Dispose();

            _techBox.Technologies.Dispose();
            _techBox.TechInventions.Dispose();
            _techBox.Inventions.Dispose();
            _techBox.FolderRanges.Dispose();
            _techBox.SchoolRanges.Dispose();
            _techBox.TechRanges.Dispose();
            _techBox.InventionRanges.Dispose();
            _techBox.SchoolActions.Dispose();
            _techBox.TechActions.Dispose();
            _techBox.InventionActions.Dispose();

            Debug.Log("Everything disposed");
        }
        */

        [BurstCompile]
        private struct AssignTerrains : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> TerrainCounter;
            [ReadOnly] public int NumOfTerrains;

            [NativeDisableParallelForRestriction] public NativeArray<ProvinceEntity> Provinces;

            public void Execute(int index)
            {
                if (Provinces[index].Terrain != -1)
                    return;

                var pluralityTerrain = -1;
                var highestCounted = -1;

                for (var i = 0; i < NumOfTerrains; i++)
                {
                    var currentCount = TerrainCounter[index * NumOfTerrains + i];
                    if (currentCount < highestCounted)
                        continue;

                    highestCounted = currentCount;
                    pluralityTerrain = i;
                }

                var target = Provinces[index];
                target.Terrain = pluralityTerrain;
                Provinces[index] = target;
            }
        }

        [BurstCompile]
        private struct CountTerrains : IJobParallelFor
        {
            [ReadOnly] public NativeMultiHashMap<int, int> ProvinceTerrain;
            [NativeDisableParallelForRestriction] public NativeArray<int> TerrainCounter;

            public void Execute(int index)
            {
                if (!ProvinceTerrain.TryGetFirstValue(index, out var terrainIndex, out var iterator))
                    throw new Exception("Province does not have terrain. " + index);

                do
                {
                    TerrainCounter[terrainIndex]++;
                } while (ProvinceTerrain.TryGetNextValue(out terrainIndex, ref iterator));
            }
        }

        [BurstCompile]
        private struct ProcessPixel : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color> ColorMap;
            [ReadOnly] public NativeHashMap<Color, int> UniqueColors;
            [ReadOnly] public NativeHashMap<int, int> StateLookup;
            [WriteOnly] public NativeArray<Color32> IdMap;

            [ReadOnly] public NativeArray<Color> TerrainMap;
            [ReadOnly] public int NumOfTerrains;
            [ReadOnly] public NativeHashMap<Color, int> TerrainLookup;
            [WriteOnly] public NativeMultiHashMap<int, int>.Concurrent ProvTerrains;

            public void Execute(int index)
            {
                var currentIndex = UniqueColors[ColorMap[index]];
                if (!StateLookup.TryGetValue(currentIndex, out var stateIndex))
                    stateIndex = StateLookup.Length; // Oceans
                // R and G: Current ID.
                // B and A: Current State.
                IdMap[index] = new Color32((byte) (currentIndex >> 0), (byte) (currentIndex >> 8),
                    (byte) (stateIndex >> 0), (byte) (stateIndex >> 8));

                // Terrain processing
                if (!TerrainLookup.TryGetValue(TerrainMap[index], out var terrainIndex))
                    throw new Exception("Color not found in terrain lookup: " + (Color32) TerrainMap[index]);

                ProvTerrains.Add(currentIndex, currentIndex * NumOfTerrains + terrainIndex);
            }
        }

        [BurstCompile]
        private struct ProcessBorderGetBorders : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color> ColorMap;
            [ReadOnly] public NativeHashMap<Color, int> UniqueColors;
            [ReadOnly] public int2 Dimensions;
            [WriteOnly] public NativeHashMap<int2, BlittableBool>.Concurrent BorderIntermediate;

            public void Execute(int index)
            {
                // Mirrored in shader code, except that is for province colors!
                var currentIndex = UniqueColors[ColorMap[index]];
                var east = currentIndex;
                var north = currentIndex;

                if (index < Dimensions.x * Dimensions.y - 1)
                    east = UniqueColors[ColorMap[index + 1]];
                if (index / Dimensions.x < Dimensions.y - 1)
                    north = UniqueColors[ColorMap[index + Dimensions.x]];

                if (east != currentIndex)
                {
                    BorderIntermediate.TryAdd(new int2(currentIndex, east), true);
                    BorderIntermediate.TryAdd(new int2(east, currentIndex), true);
                }

                if (north == currentIndex)
                    return;

                BorderIntermediate.TryAdd(new int2(currentIndex, north), true);
                BorderIntermediate.TryAdd(new int2(north, currentIndex), true);
            }
        }

        [BurstCompile]
        private struct ProcessBorderSortRanges : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int2> UniqueBorderPairs;
            [WriteOnly] public NativeMultiHashMap<int, int>.Concurrent SortedBorders;

            public void Execute(int index)
            {
                // Very simple, prepares uniques for listing.
                SortedBorders.Add(UniqueBorderPairs[index].x, UniqueBorderPairs[index].y);
            }
        }

        [BurstCompile]
        private struct ProcessBorderOutput : IJob
        {
            [ReadOnly] public int MaxProvinces;
            [ReadOnly] public NativeHashMap<int2, BlittableBool> BorderRemove;
            [ReadOnly] public NativeMultiHashMap<int, int> SortedBorders;
            [WriteOnly] public NativeArray<int> BorderEnds;
            public NativeList<int> BorderIndices;

            public void Execute()
            {
                for (var i = 0; i < MaxProvinces; i++)
                {
                    if (!SortedBorders.TryGetFirstValue(i, out var bordering, out var iterator))
                    {
                        BorderEnds[i] = -1;
                        continue;
                    }

                    do
                    {
                        if (!BorderRemove.TryGetValue(new int2(i, bordering), out _))
                            BorderIndices.Add(bordering);
                    } while (SortedBorders.TryGetNextValue(out bordering, ref iterator));

                    BorderEnds[i] = BorderIndices.Length;
                }
            }
        }

        [Serializable]
        private struct BorderCache
        {
            public List<int> borderIndices, borderEnds;
        }
    }
}