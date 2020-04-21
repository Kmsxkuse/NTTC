using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public struct Map : IComponentData
    {
        public bool LoadingFinished,
            NamesLoaded,
            ClickLoaded;

        public int2 ColorSize;
    }

    public class LoadMain : MonoBehaviour
    {
        //public static bool CacheLoad;
        private NativeList<int> _borderList;
        private NativeMultiHashMap<int, int> _borderMiddle, _provTerrains;
        private NativeHashMap<int2, bool> _borderStart, _borderRemove;
        private NativeArray<Color> _colorMap, _terrainMap;
        private Map _mapComponent;
        private Entity _mapEntity;
        private Material _mapMaterial;
        private bool _pixelDone, _borderDone;
        private JobHandle _pixelJobHandle, _borderJobHandle, _terrainJobHandle;
        private NativeHashMap<int, int> _stateLookup, _idIndex;
        private NativeArray<int> _terrainCounter;
        private NativeArray<int2> _uniqueBorderPairs;
        private NativeHashMap<Color, int> _uniqueColors, _terrainLookup;

        private void OnDestroy()
        {
            _stateLookup.Dispose();
            _terrainLookup.Dispose();
            _uniqueColors.Dispose();
            _idIndex.Dispose();
        }

        private void Start()
        {
            // TODO: Json inline checksum verification.
            // Very expensive conversion between Paradox files to Json arrays.

            Debug.Log("Start: " + Time.realtimeSinceStartup);
            var start = Time.realtimeSinceStartup;

            /*
            // Loading color maps!
            var colorBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "Map", "provinces.png"));
            var map = new Texture2D(2, 2);
            map.LoadImage(colorBytes);
            _mapComponent.ColorSize = new int2(map.width, map.height);
            _colorMap = new NativeArray<Color>(map.GetPixels(), Allocator.TempJob);

            colorBytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "Map", "terrain.png"));
            map.LoadImage(colorBytes);

            if (map.width != _mapComponent.ColorSize.x || map.height != _mapComponent.ColorSize.y)
                throw new Exception("Dimension mismatch between province and terrain textures!");

            _terrainMap = new NativeArray<Color>(map.GetPixels(), Allocator.TempJob);
            */
            //LoadScreen.SetLoadingScreen(LoadingStages.Terrain);

            var (terrainNames, terrainPalette) = TerrainLoad.Main(false);
            LookupDictionaries.AssignDictionary("TerrainNames", terrainNames);

            _terrainLookup = new NativeHashMap<Color, int>(terrainPalette.Count, Allocator.TempJob);
            foreach (var (palette, index) in terrainPalette)
                _terrainLookup.TryAdd(palette, index);

            //LoadScreen.SetLoadingScreen(LoadingStages.Definitions);

            var (definedNames, idIndexes, foundColors) = DefinitionsLoad.Main(false);
            _uniqueColors = new NativeHashMap<Color, int>(definedNames.Count, Allocator.TempJob);
            _idIndex = new NativeHashMap<int, int>(definedNames.Count, Allocator.TempJob);

            // Populating idIndexes
            for (var i = 0; i < idIndexes.Count; i++)
                _idIndex.TryAdd(idIndexes[i], i);
            UnifiedVariables.IdIndex = _idIndex;
            // Populating unique colors
            for (var i = 0; i < foundColors.Count; i++)
                _uniqueColors.TryAdd(foundColors[i], i);

            //LoadScreen.SetLoadingScreen(LoadingStages.States);

            var (stateProvinces, stateIds) = RegionsLoad.Main(false, _idIndex);

            _stateLookup = new NativeHashMap<int, int>(stateIds.Count, Allocator.TempJob);
            foreach (var stateKvP in stateProvinces)
                _stateLookup.TryAdd(stateKvP.x, stateKvP.y);

            //LoadScreen.SetLoadingScreen(LoadingStages.Pixels);

            /*
            _idMap = new NativeArray<Color32>(_colorMap.Length, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);

            _provTerrains = new NativeMultiHashMap<int, int>(_colorMap.Length, Allocator.TempJob);

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
            _borderStart = new NativeHashMap<int2, bool>(_colorMap.Length * 3, Allocator.TempJob);

            _borderJobHandle = new ProcessBorderGetBorders
            {
                BorderIntermediate = _borderStart.ToConcurrent(),
                ColorMap = _colorMap,
                UniqueColors = _uniqueColors,
                Dimensions = _mapComponent.ColorSize
            }.Schedule(_colorMap.Length, 100);
            */

            //LoadScreen.SetLoadingScreen(LoadingStages.Continents);
            var (continentProvinces, continentNames) = ContinentLoad.Main(false, _idIndex);
            LookupDictionaries.AssignDictionary("ContinentNames", continentNames);

            //LoadScreen.SetLoadingScreen(LoadingStages.CountryNames);
            var (countryNames, countryTags, countryPaths) = CountriesLoad.Names(false);
            LookupDictionaries.AssignDictionary("CountryTags", countryTags);

            //LoadScreen.SetLoadingScreen(LoadingStages.Religions);
            var (religionNames, religionGroupNames) = ReligionsLoad.Main(false);
            LookupDictionaries.AssignDictionary("ReligionNames", religionNames);

            //LoadScreen.SetLoadingScreen(LoadingStages.Cultures);
            var (cultureNames, cultureGroupNames) = CulturesLoad.Main(false);
            LookupDictionaries.AssignDictionary("CultureNames", cultureNames);
            LookupDictionaries.AssignDictionary("CultureGroupNames", cultureGroupNames);

            //LoadScreen.SetLoadingScreen(LoadingStages.Ideologies);
            var (ideologies, ideologyGroups, ideologyNames, ideologyGroupNames) = IdeologiesLoad.Main(false);
            
            /*
            //LoadScreen.SetLoadingScreen(LoadingStages.Governments);
            var (governments, governmentNames) = GovernmentsLoad.Main();

            //LoadScreen.SetLoadingScreen(LoadingStages.Goods);
            var (goods, goodsCategory, goodsNames, goodsCategoryNames) = GoodsLoad.Main();

            //LoadScreen.SetLoadingScreen(LoadingStages.Buildings);
            var (buildings, buildingNames) = BuildingsLoad.Main();

            //LoadScreen.SetLoadingScreen(LoadingStages.Units);
            var (units, unitNames) = UnitsLoad.Main();

            //LoadScreen.SetLoadingScreen(LoadingStages.PopTypes);
            var (popTypes, popTypeNames) = PopTypesLoad.Main();

            //LoadScreen.SetLoadingScreen(LoadingStages.PolicyNames);
            var (issueFileTree, issueValues, subPolicies, policyGroups, subPolicyNames, policyGroupNames)
                = IssuesLoad.PolicyNames();
            _stringBox.PolicyGroupNames = policyGroupNames;
            _stringBox.SubPolicyNames = subPolicyNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Technology);
            var (technologies, schools, folders, incompleteInventions) = TechLoad.Main(ref _stringBox);

            //LoadScreen.SetLoadingScreen(LoadingStages.Crimes);
            var (crimes, crimeNames) = CrimeLoad.Main();
            _stringBox.CrimeNames = crimeNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.EventModifiers);
            var (eventModifiers, eventModifierNames) = EventModifierLoad.Main();
            _stringBox.EventModifierNames = eventModifierNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.NationalValues);, literally identical to Event Modifiers. Merge or generify?
            var (nationalValues, nationalValueNames) = NationalValueLoad.Main();
            _stringBox.NationalValueNames = nationalValueNames;

            //LoadScreen.SetLoadingScreen(LoadingStages.Inventions);
            var inventions = InventionsLoad.Main(technologies, ref _stringBox);

            //LoadScreen.SetLoadingScreen(LoadingStages.Issues);
            IssuesLoad.Main(issueFileTree, issueValues, subPolicies, policyGroups);

            //LoadScreen.SetLoadingScreen(LoadingStages.TechnologyNames);

            InventionsLoad.CompleteInventions(technologies, incompleteInventions);

            //LoadScreen.SetLoadingScreen(LoadingStages.Histories);

            var provinces = ProvinceHistoryLoad.Main(_newDataBox.IdIndex);
            ProvincePopulationLoad.Main(provinces, _newDataBox.IdIndex);

            var (countries, tempRulingParty) = CountryHistoryLoad.Main(technologies, inventions, cultures,
                ideologies, subPolicies, governments);

            //LoadScreen.SetLoadingScreen(LoadingStages.Countries);
            var (countryParties, countryPartyNames) = CountriesLoad.Main(countries, countryPaths);
            _stringBox.CountryPartyNames = countryPartyNames;

            _pixelJobHandle.Complete();
            _borderJobHandle.Complete();

            _uniqueColors.Dispose();
            _stateLookup.Dispose();
            _colorMap.Dispose();
            _terrainMap.Dispose();
            _terrainLookup.Dispose();
            _provTerrains.Dispose();
            _borderStart.Dispose();
            */

            Debug.Log("End: " + Time.realtimeSinceStartup);
            Debug.Log("Duration: " + (Time.realtimeSinceStartup - start));
        }
    }
}