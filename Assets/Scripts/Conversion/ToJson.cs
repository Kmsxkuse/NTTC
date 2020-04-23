using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CamCode;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Conversion
{
    public class ToJson : MonoBehaviour
    {
        // Hardcoded boot strap of Paradox files to Json.
        // Generic was taking too long and far too complex to handle.
        private void Awake()
        {
            // Parsing Countries.
            var (_, tagLookup, _) = CountriesLoad.Names();
            
            // Parsing goods
            var fileTree = new List<(string, object)>();
            /*
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
            var (provNames, idLookup, provEntityLookup)
                = DefinitionsLoad.Main(colorLookup, oceanDefault);
            
            // Parsing states
            var stateLookup = new NativeHashMap<int, int>(1, Allocator.TempJob);
            var stateNames = RegionsLoad.Main(idLookup, stateLookup);

            var map = LoadPng(Path.Combine(Application.streamingAssetsPath,
                "map", "provinces.png"));
            map.filterMode = FilterMode.Point;
            
            var colorMap = new NativeArray<Color32>(map.GetPixels32(), Allocator.TempJob);
            var idMap = new NativeArray<Color32>(colorMap.Length, Allocator.TempJob);
            
            var pixelHandle = new ProcessPixel
            {
                ColorLookup = colorLookup,
                ColorMap = colorMap,
                IdMap = idMap,
                StateLookup = stateLookup
            }.Schedule(colorMap.Length, 32);
            
            ProvinceLoad.Main(provEntityLookup, tagLookup);
            
            pixelHandle.Complete();
            
            map.SetPixels32(idMap.ToArray());
            map.Apply();
            
            LoadMap.Texture = map;

            idMap.Dispose();
            colorLookup.Dispose();
            stateLookup.Dispose();
            
            //var test = JsonConvert.SerializeObject(fileTree, Formatting.Indented);
            //File.WriteAllText(Path.Combine(Application.strea
            //mingAssetsPath, "JsonData", "test.txt"), test);
            //Debug.Log(test);
        
            Texture2D LoadPng(string filePath) {
                if (!File.Exists(filePath)) 
                    throw new Exception("Texture: " + filePath + " does not exist.");
        
                var fileData = File.ReadAllBytes(filePath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                return tex;
            }
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
                var currentIndex= ColorLookup[ColorMap[index]];
                
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
