using System;
using System.Collections.Generic;
using CamCode;
using Market;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public class LoadChain : MonoBehaviour
    {
        public const int GoodNum = 6;

        private static readonly Queue<IDisposable> BlobAssetReferences = new Queue<IDisposable>();
        // Hardcoded boot strap of Paradox files to Json.
        // Generic was taking too long and far too complex to handle.

        public Texture2D ProvinceMap;

        private void Awake()
        {
            // Disabling physics. Not used in project.
            Physics.autoSimulation = false;

            // Parsing Countries.
            var (_, tagLookup, _) = CountriesLoad.Names();

            // Creating StateCountryProcessing system
            var stateCountryProcessing = World.DefaultGameObjectInjectionWorld
                .GetOrCreateSystem(typeof(StateCountryProcessing));

            // Parsing goods
            /*
            var fileTree = new List<(string, object)>();
            ParseFile(Path.Combine(Application.streamingAssetsPath, "common", "goods.txt"), fileTree);
            var goodsLookup = new Dictionary<string, int>();
            // Ignoring good groups
            var counter = 0;
            foreach (var (_, value) in fileTree)
            foreach (var (key, innerValue) in (List<(string, object)>) value)
            {
                var good = new Goods {Name = key};
                goodsLookup.Add(key, counter++);
                foreach (var (type, data) in (List<(string, object)>) innerValue)
                {
                    switch (type)
                    {
                        case "cost":
                            good.Cost = float.Parse((string) data);
                            continue;
                        case "color":
                            good.Color = ParseColor32((string) data);
                            continue;
                    }
                }
                
                em.SetComponentData(em.CreateEntity(typeof(Goods)), good);
            }
            */

            // Parsing provinces
            var colorLookup = new NativeHashMap<Color, int>(1, Allocator.TempJob);
            var oceanDefault = tagLookup["OCEAN"];
            var (provNames, idLookup, provEntityLookup) =
                DefinitionsLoad.Main(colorLookup, oceanDefault);

            //var map = LoadPng(Path.Combine(Application.streamingAssetsPath, "map", "provinces.png"));

            // DEBUG
            var map = new Texture2D(ProvinceMap.width, ProvinceMap.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Graphics.CopyTexture(ProvinceMap, map);

            // Begin CPU pixel processing jobs.
            var colorMap = new NativeArray<Color32>(map.GetPixels32(), Allocator.TempJob);

            var pixelCollector = new NativeMultiHashMap<int, int>(colorMap.Length, Allocator.TempJob);

            var pixelHandle = new CollectPixels
            {
                ColorMap = colorMap,
                ColorLookup = colorLookup,
                Collector = pixelCollector.AsParallelWriter()
            }.Schedule(colorMap.Length, 32);

            // Parsing states
            var stateLookup = new NativeHashMap<int, int>(1, Allocator.TempJob);
            var (stateNames, stateToProvReference, provToStateReference) =
                StatesLoad.Main(idLookup, stateLookup, provEntityLookup);
            BlobAssetReferences.Enqueue(stateToProvReference);
            BlobAssetReferences.Enqueue(provToStateReference);

            var idMap = new NativeArray<Color32>(colorMap.Length, Allocator.TempJob);

            pixelHandle = new ProcessPixel
            {
                ColorLookup = colorLookup,
                ColorMap = colorMap,
                IdMap = idMap,
                StateLookup = stateLookup
            }.Schedule(colorMap.Length, 32, pixelHandle);

            stateLookup.Dispose(pixelHandle);

            colorMap.Dispose(pixelHandle);
            colorLookup.Dispose(pixelHandle);

            var centroids = new NativeArray<Color32>(idLookup.Count, Allocator.TempJob);

            pixelHandle = new FindCentroid
            {
                Collector = pixelCollector,
                Width = ProvinceMap.width,
                Centroids = centroids
            }.Schedule(centroids.Length, 2, pixelHandle);

            pixelCollector.Dispose(pixelHandle);

            var (factories, maxEmploy) = AgentsLoad.Main();
            foreach (var blobAssetReference in factories)
                BlobAssetReferences.Enqueue(blobAssetReference);

            ProvinceLoad.Main(provEntityLookup, tagLookup, factories, maxEmploy, provToStateReference);
            // Pops load outputs a blob asset reference. Just inlining the two calls.
            BlobAssetReferences.Enqueue(PopsLoad.Main(provToStateReference));

            // Tag states that are not completely owned.
            // Also attaching owned states (plus incomplete which is duplicated) to countries.
            StateCountryProcessing.CallMethod = StateCountryProcessing.ManualMethodCall.TagOwnedStatesAndAttachToCountry;
            stateCountryProcessing.Update();

            // DEBUG
            StateCountryProcessing.MarketIdentities = factories;
            StateCountryProcessing.MaxEmploy = maxEmploy;
            StateCountryProcessing.CallMethod = StateCountryProcessing.ManualMethodCall.SetDebugValues;
            stateCountryProcessing.Update();

            StateCountryProcessing.CallMethod = StateCountryProcessing.ManualMethodCall.DebugSpawnFactories;
            stateCountryProcessing.Update();

            StateCountryProcessing.CallMethod = StateCountryProcessing.ManualMethodCall.DisposeDebugFactoryTemplates;
            stateCountryProcessing.Update();

            // Deleting initialization system.
            World.DefaultGameObjectInjectionWorld.DestroySystem(stateCountryProcessing);

            pixelHandle.Complete();

            map.SetPixels32(idMap.ToArray());
            map.Apply();

            var centroidTex = new Texture2D(centroids.Length, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            centroidTex.SetPixels32(centroids.ToArray());
            centroidTex.Apply();

            LoadMap.MapTexture = map;
            ScalarSystem.IdMapTex = map;
            ScalarSystem.CentroidTex = centroidTex;

            //File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "test.png"), centroidTex.EncodeToPNG());

            idMap.Dispose();
            centroids.Dispose();

            /*
            Texture2D LoadPng(string filePath)
            {
                if (!File.Exists(filePath))
                    throw new Exception("Texture: " + filePath + " does not exist.");

                var fileData = File.ReadAllBytes(filePath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                return tex;
            }
            */
        }

        private void OnDestroy()
        {
            while (BlobAssetReferences.Count > 0)
                BlobAssetReferences.Dequeue().Dispose();
        }

        [BurstCompile]
        private struct ProcessPixel : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> ColorMap;
            [ReadOnly] public NativeHashMap<Color, int> ColorLookup;
            [ReadOnly] public NativeHashMap<int, int> StateLookup;
            [WriteOnly] public NativeArray<Color32> IdMap;

            public void Execute(int index)
            {
                var currentIndex = ColorLookup[ColorMap[index]];

                if (!StateLookup.TryGetValue(currentIndex, out var stateIndex))
                    stateIndex = StateLookup.Count(); // Oceans
                // R and G: Current ID.
                // B and A: Current State.
                IdMap[index] = new Color32((byte) (currentIndex >> 0), (byte) (currentIndex >> 8),
                    (byte) (stateIndex >> 0), (byte) (stateIndex >> 8));
            }
        }

        [BurstCompile]
        private struct CollectPixels : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Color32> ColorMap;
            [ReadOnly] public NativeHashMap<Color, int> ColorLookup;

            public NativeMultiHashMap<int, int>.ParallelWriter Collector;

            public void Execute(int index)
            {
                Collector.Add(ColorLookup[ColorMap[index]], index);
            }
        }

        [BurstCompile]
        private struct FindCentroid : IJobParallelFor
        {
            [ReadOnly] public NativeMultiHashMap<int, int> Collector;
            [ReadOnly] public int Width;

            [WriteOnly] public NativeArray<Color32> Centroids;

            public void Execute(int index)
            {
                if (!Collector.TryGetFirstValue(index, out var point, out var iterator))
                    return;

                var holder = ConvertToFloat2(point);

                var counter = 1;

                while (Collector.TryGetNextValue(out point, ref iterator))
                    holder += (ConvertToFloat2(point) - holder) / counter++;

                var centroid = (int2) math.round(holder);

                Centroids[index] = new Color32((byte) (centroid.x >> 0), (byte) (centroid.x >> 8),
                    (byte) (centroid.y >> 0), (byte) (centroid.y >> 8));
            }

            private float2 ConvertToFloat2(int index)
            {
                var x = index % Width;
                var y = index / Width;

                return new float2(x, y);
            }
        }
    }
}