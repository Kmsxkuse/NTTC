using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public class LoadProvNames : MonoBehaviour
    {
        private CacheInfo _cacheInfo;
        private NativeArray<float2> _centerPoints, _furthestPoint, _centerNational;
        private DataBox _dataBox;
        private EntityManager _em;
        private JobHandle _initialJobHandle, _anglesJobHandle, _contiguousJobHandle;
        private NativeArray<float> _longestDistance, _provinceAngles, _longestNational, _nationalAngles;
        private Map _mapComponent;
        private Entity _mapEntity;
        private bool _namesLoaded, _nationalLoaded;
        private NativeArray<int> _numPoints;
        private NativeMultiHashMap<int, float2> _pixelsFound;
        private PoliticalBox _politicalBox;
        private StringBox _stringBox;

        [SerializeField] private GameObject textPrefab, nationalHeader;

        private void Start()
        {
            (_mapEntity, _em) = LoadMethods.MapEntity();
        }

        private void Update()
        {
            if (_mapComponent.LoadingFinished)
                return;
            // Continuously calling for loaded data until loaded!
            _mapComponent = _em.GetComponentData<Map>(_mapEntity);

            if (!_mapComponent.LoadingFinished)
                return;

            _dataBox = _em.GetSharedComponentData<DataBox>(_mapEntity);
            _stringBox = _em.GetSharedComponentData<StringBox>(_mapEntity);
            _politicalBox = _em.GetSharedComponentData<PoliticalBox>(_mapEntity);

            _cacheInfo = JsonUtility.FromJson<CacheInfo>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache/Cache.json")));

            StartCoroutine(WaitForFinished());

            if (MapLoad.CacheLoad)
                CachedCenters();
            else
                GenerateCenters();

            enabled = false;
        }

        private IEnumerator WaitForFinished()
        {
            while (!_namesLoaded || !_nationalLoaded)
                yield return null;

            // Outputting cached centers

            if (!MapLoad.CacheLoad)
            {
                var centerProvCache = new CenterCache
                {
                    centerPoints = _centerPoints.ToList(),
                    longestDistance = _longestDistance.ToList(),
                    provinceAngles = _provinceAngles.ToList(),

                    centerNationals = _centerNational.ToList(),
                    longestNational = _longestNational.ToList(),
                    nationalAngles = _nationalAngles.ToList()
                };

                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.textLabels),
                    JsonUtility.ToJson(centerProvCache));
            }

            _mapComponent = _em.GetComponentData<Map>(_mapEntity);
            _mapComponent.NamesLoaded = true;
            _em.SetComponentData(_mapEntity, _mapComponent);

            yield return null;

            _centerPoints.Dispose();
            _provinceAngles.Dispose();
            _longestDistance.Dispose();

            _centerNational.Dispose();
            _longestNational.Dispose();
            _nationalAngles.Dispose();
        }

        private void CachedCenters()
        {
            var centerCache = JsonUtility.FromJson<CenterCache>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Cache", _cacheInfo.textLabels)));

            _centerPoints = new NativeArray<float2>(centerCache.centerPoints.ToArray(), Allocator.TempJob);
            _longestDistance = new NativeArray<float>(centerCache.longestDistance.ToArray(), Allocator.TempJob);
            _provinceAngles = new NativeArray<float>(centerCache.provinceAngles.ToArray(), Allocator.TempJob);

            _centerNational = new NativeArray<float2>(centerCache.centerNationals.ToArray(), Allocator.TempJob);
            _longestNational = new NativeArray<float>(centerCache.longestNational.ToArray(), Allocator.TempJob);
            _nationalAngles = new NativeArray<float>(centerCache.nationalAngles.ToArray(), Allocator.TempJob);

            InstantiateNames(transform, _centerPoints, _provinceAngles,
                _stringBox.ProvinceNames, _longestDistance, false, out _namesLoaded);

            InstantiateNames(nationalHeader.transform, _centerNational, _nationalAngles,
                _stringBox.CountryNames, _longestNational, true, out _nationalLoaded);
        }

        private void GenerateCenters()
        {
            // Gathering every 10 pixel to find centroid. Index is Province.
            _centerPoints = new NativeArray<float2>(_dataBox.Provinces.Length, Allocator.TempJob);
            _longestDistance = new NativeArray<float>(_dataBox.Provinces.Length, Allocator.TempJob);
            _furthestPoint = new NativeArray<float2>(_dataBox.Provinces.Length, Allocator.TempJob);
            _numPoints = new NativeArray<int>(_dataBox.Provinces.Length, Allocator.TempJob);
            _pixelsFound = new NativeMultiHashMap<int, float2>(_dataBox.IdMap.Length, Allocator.TempJob);

            _initialJobHandle = new PointsJob
            {
                ColorWidth = _mapComponent.ColorSize.x,
                IdMap = _dataBox.IdMap,
                NumPoints = _numPoints,
                CenterPoints = _centerPoints,
                PixelsFound = _pixelsFound.ToConcurrent()
            }.Schedule(_dataBox.IdMap.Length, 100);

            _initialJobHandle = new LongestJob
            {
                CenterPoints = _centerPoints,
                FurthestPoint = _furthestPoint,
                LongestDistance = _longestDistance,
                PointsFound = _pixelsFound
            }.Schedule(_dataBox.Provinces.Length, 10, _initialJobHandle);

            StartCoroutine(GenerateNational());
            StartCoroutine(CalculateNames());
        }

        private IEnumerator CalculateNames()
        {
            _provinceAngles = new NativeArray<float>(_dataBox.Provinces.Length, Allocator.TempJob);

            _anglesJobHandle = new AnglesJob
            {
                CenterPoints = _centerPoints,
                FurthestPoint = _furthestPoint,
                Angles = _provinceAngles
            }.Schedule(_centerPoints.Length, 10, _initialJobHandle);

            while (!_anglesJobHandle.IsCompleted)
                yield return null;

            _anglesJobHandle.Complete();

            // Removing used Natives
            _pixelsFound.Dispose();
            _furthestPoint.Dispose();

            InstantiateNames(transform, _centerPoints, _provinceAngles,
                _stringBox.ProvinceNames, _longestDistance, false, out _namesLoaded);
        }

        private void InstantiateNames(Transform targetHeader, NativeArray<float2> centers,
            NativeArray<float> angles, IReadOnlyList<string> strSource, NativeArray<float> longest, bool countryScale,
            out bool toggle)
        {
            // Setting header to proper location at bottom left corner of texture.
            // Default scaling is 1 / 100 of the actual texture size. Hardcoded I know. Dividing by 2 because origin is at center of texture.
            targetHeader.localPosition = new Vector3(-_mapComponent.ColorSize.x / 100f / 2f,
                -_mapComponent.ColorSize.y / 100f / 2f, -9);

            for (var i = 0; i < centers.Length; i++)
            {
                var newText = Instantiate(textPrefab, targetHeader);
                var textTransform = newText.GetComponent<RectTransform>();
                textTransform.localPosition = new Vector3(centers[i].x / 100,
                    centers[i].y / 100);
                textTransform.rotation = Quaternion.AngleAxis(angles[i], Vector3.forward);

                var tmp = newText.GetComponent<TextMeshPro>();
                tmp.text = LoadMethods.NameCleaning(strSource[i]);
                textTransform.sizeDelta = new Vector2(tmp.preferredWidth, 2.02f);

                // Default scale: 0.0075. Hardcoded because it's the only one that looks nice.
                var scale = longest[i] / tmp.preferredWidth;
                tmp.name = scale.ToString(CultureInfo.InvariantCulture);

                if (!countryScale && scale > 50 || tmp.preferredWidth < 0.01)
                {
                    Destroy(newText);
                    continue;
                }

                // Trial and error
                if (countryScale)
                    scale *= scale < 10 // Countries
                        ? 0.01f
                        : scale < 20 // Countries
                            ? 0.0075f
                            : scale < 30 // Countries
                                ? 0.006f
                                : 0.004f;
                else
                    scale *= scale < 3 // Provinces
                        ? 0.0078f
                        : scale < 5
                            ? 0.0062f
                            : 0.0035f;

                textTransform.localScale = new Vector3(scale, scale, 1);
            }

            toggle = true;
        }

        private IEnumerator GenerateNational()
        {
            var nationalCapitals = new NativeArray<int>(
                _politicalBox.CountryHistories.Select(country => country.capital).ToArray(), Allocator.TempJob);
            var provinceOwnership = new NativeArray<int>(
                _dataBox.Provinces.Select(province => province.Owner).ToArray(), Allocator.TempJob);
            var provinceLifeRating = new NativeArray<int>(
                _dataBox.Provinces.Select(province => province.LifeRating).ToArray(), Allocator.TempJob);

            var contiguousProvinces =
                new NativeMultiHashMap<int, float2>(_dataBox.Provinces.Length, Allocator.TempJob);
            var checkedProvinces = new NativeArray<bool>(_dataBox.Provinces.Length, Allocator.TempJob);
            var furthestNational = new NativeArray<float2>(_politicalBox.CountryHistories.Length, Allocator.TempJob);
            _centerNational = new NativeArray<float2>(_politicalBox.CountryHistories.Length, Allocator.TempJob);
            _longestNational = new NativeArray<float>(_politicalBox.CountryHistories.Length, Allocator.TempJob);
            _nationalAngles = new NativeArray<float>(_politicalBox.CountryHistories.Length, Allocator.TempJob);

            _contiguousJobHandle = new ContiguousJob
            {
                BorderEnds = _dataBox.BorderEnds,
                BorderIndices = _dataBox.BorderIndices,
                CheckedProvinces = checkedProvinces,
                ContiguousProvinces = contiguousProvinces.ToConcurrent(),
                ProvinceCentroids = _centerPoints,
                ProvinceLifeRating = provinceLifeRating,
                ProvinceOwnership = provinceOwnership,
                ProvincePixels = _numPoints,
                NationalCapitals = nationalCapitals,
                CenterNationals = _centerNational
            }.Schedule(_politicalBox.CountryHistories.Length, 1, _initialJobHandle);

            _contiguousJobHandle = new LongestJob
            {
                CenterPoints = _centerNational,
                FurthestPoint = furthestNational,
                LongestDistance = _longestNational,
                PointsFound = contiguousProvinces
            }.Schedule(_politicalBox.CountryHistories.Length, 1, _contiguousJobHandle);

            _contiguousJobHandle = new AnglesJob
            {
                CenterPoints = _centerNational,
                FurthestPoint = furthestNational,
                Angles = _nationalAngles
            }.Schedule(_politicalBox.CountryHistories.Length, 1, _contiguousJobHandle);

            while (!_contiguousJobHandle.IsCompleted)
                yield return null;

            _contiguousJobHandle.Complete();

            _numPoints.Dispose();
            provinceLifeRating.Dispose();
            provinceOwnership.Dispose();
            nationalCapitals.Dispose();
            contiguousProvinces.Dispose();
            checkedProvinces.Dispose();
            furthestNational.Dispose();

            InstantiateNames(nationalHeader.transform, _centerNational, _nationalAngles,
                _stringBox.CountryNames, _longestNational, true, out _nationalLoaded);
        }

        [Serializable]
        private struct CenterCache
        {
            public List<float2> centerPoints, centerNationals;
            public List<float> longestDistance, provinceAngles, longestNational, nationalAngles;
        }

        //[BurstCompile] // RIP, the Native Queue in the job kills Burst.
        private struct ContiguousJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int> NationalCapitals,
                ProvinceOwnership,
                ProvincePixels,
                BorderIndices,
                BorderEnds,
                ProvinceLifeRating; // Filtering out oceans

            [ReadOnly] public NativeArray<float2> ProvinceCentroids;

            [WriteOnly] public NativeArray<float2> CenterNationals;
            [WriteOnly] public NativeMultiHashMap<int, float2>.Concurrent ContiguousProvinces;

            [NativeDisableParallelForRestriction] public NativeArray<bool> CheckedProvinces;

            public void Execute(int index)
            {
                var searchQueue = new NativeQueue<int>(Allocator.Temp);

                var sumProvinceWeights = 0;
                var rawAverage = new float2(0);

                var cursor = NationalCapitals[index];
                do
                {
                    if (CheckedProvinces[cursor]
                        || ProvinceLifeRating[cursor] < 0.1f // Ocean
                        || ProvinceOwnership[cursor] != index)
                        continue;

                    ContiguousProvinces.Add(index, ProvinceCentroids[cursor]);
                    CheckedProvinces[cursor] = true;

                    rawAverage += ProvinceCentroids[cursor] * ProvincePixels[cursor];
                    sumProvinceWeights += ProvincePixels[cursor];

                    var end = cursor > 0 ? BorderEnds[cursor - 1] : 0;
                    for (var i = end; i < BorderEnds[cursor]; i++)
                        searchQueue.Enqueue(BorderIndices[i]);
                } while (searchQueue.TryDequeue(out cursor));

                CenterNationals[index] = rawAverage / (sumProvinceWeights > 0 ? sumProvinceWeights : 1);

                //searchQueue.Dispose(); // Not needed? What is documentation anyways.
            }
        }

        [BurstCompile]
        private struct AnglesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float2> CenterPoints, FurthestPoint;
            [WriteOnly] public NativeArray<float> Angles;

            public void Execute(int index)
            {
                var vector = FurthestPoint[index] - CenterPoints[index];
                var angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;

                if (angle < -90 || angle > 90)
                    angle += 180;

                Angles[index] = angle;
            }
        }

        [BurstCompile]
        private struct PointsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> IdMap;
            [ReadOnly] public int ColorWidth;

            [WriteOnly] public NativeMultiHashMap<int, float2>.Concurrent PixelsFound;

            [NativeDisableParallelForRestriction] public NativeArray<int> NumPoints;
            [NativeDisableParallelForRestriction] public NativeArray<float2> CenterPoints;

            public void Execute(int index)
            {
                var color = IdMap[index];
                var point = color.r + color.g * 256;

                var uv = new int2(index % ColorWidth, index / ColorWidth);
                PixelsFound.Add(point, uv);

                // Yes, I know there's a race condition here but it works.
                var temp = NumPoints[point];
                CenterPoints[point] += (uv - CenterPoints[point]) / ++temp;
                NumPoints[point] = temp;
            }
        }

        [BurstCompile]
        private struct LongestJob : IJobParallelFor
        {
            [ReadOnly] public NativeMultiHashMap<int, float2> PointsFound;
            [ReadOnly] public NativeArray<float2> CenterPoints;

            [WriteOnly] public NativeArray<float2> FurthestPoint;

            [NativeDisableParallelForRestriction] public NativeArray<float> LongestDistance;

            public void Execute(int index)
            {
                if (!PointsFound.TryGetFirstValue(index, out var uv, out var iterator))
                    return;
                do
                {
                    var distance = Mathf.Sqrt(
                        Mathf.Pow(uv.x - CenterPoints[index].x, 2) +
                        Mathf.Pow(uv.y - CenterPoints[index].y, 2)
                    );

                    if (distance < LongestDistance[index])
                        continue;

                    LongestDistance[index] = distance;
                    FurthestPoint[index] = uv;
                } while (PointsFound.TryGetNextValue(out uv, ref iterator));
            }
        }
    }
}