using System;
using System.Collections.Generic;
using System.IO;
using CamCode;
using Market;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Conversion
{
    public class LoadChain : MonoBehaviour
    {
        private static readonly Queue<IDisposable> BlobAssetReferences = new Queue<IDisposable>();
        // Hardcoded boot strap of Paradox files to Json.
        // Generic was taking too long and far too complex to handle.

        public Texture2D ProvinceMap;

        private void Awake()
        {
            // Parsing Countries.
            var (_, tagLookup, _) = CountriesLoad.Names();

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

            // Parsing states
            var stateLookup = new NativeHashMap<int, int>(1, Allocator.TempJob);
            var (stateNames, stateToProvReference, provToStateReference) =
                StatesLoad.Main(idLookup, stateLookup, provEntityLookup);
            BlobAssetReferences.Enqueue(stateToProvReference);
            BlobAssetReferences.Enqueue(provToStateReference);

            //var map = LoadPng(Path.Combine(Application.streamingAssetsPath, "map", "provinces.png"));

            // DEBUG
            var map = new Texture2D(ProvinceMap.width, ProvinceMap.height, TextureFormat.RGBA32, false);
            Graphics.CopyTexture(ProvinceMap, map);

            map.filterMode = FilterMode.Point;

            // Disposed automatically after job.
            var colorMap = new NativeArray<Color32>(map.GetPixels32(), Allocator.TempJob);
            var idMap = new NativeArray<Color32>(colorMap.Length, Allocator.TempJob);

            var pixelHandle = new ProcessPixel
            {
                ColorLookup = colorLookup,
                ColorMap = colorMap,
                IdMap = idMap,
                StateLookup = stateLookup
            }.Schedule(colorMap.Length, 32);

            colorLookup.Dispose(pixelHandle);
            stateLookup.Dispose(pixelHandle);

            var (factories, maxEmploy) = AgentsLoad.Main();
            foreach (var blobAssetReference in factories)
                BlobAssetReferences.Enqueue(blobAssetReference);

            ProvinceLoad.Main(provEntityLookup, tagLookup, factories, maxEmploy, provToStateReference);
            // Pops load outputs a blob asset reference. Just inlining the two calls.
            BlobAssetReferences.Enqueue(PopsLoad.Main(provToStateReference));
            
            // DEBUG
            MarketSystem.SetDebugValues(factories, maxEmploy);

            pixelHandle.Complete();

            map.SetPixels32(idMap.ToArray());
            map.Apply();

            LoadMap.Texture = map;
            
            idMap.Dispose();

            Texture2D LoadPng(string filePath)
            {
                if (!File.Exists(filePath))
                    throw new Exception("Texture: " + filePath + " does not exist.");

                var fileData = File.ReadAllBytes(filePath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                return tex;
            }
        }

        private void OnDestroy()
        {
            while (BlobAssetReferences.Count > 0)
                BlobAssetReferences.Dequeue().Dispose();
        }

        [BurstCompile]
        private struct ProcessPixel : IJobParallelFor
        {
            [DeallocateOnJobCompletion] public NativeArray<Color32> ColorMap;
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
    }
}