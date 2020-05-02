using System.IO;
using Newtonsoft.Json;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public static class AgentsLoad
    {
        public static BlobAssetReference<MarketMatrix>[] Main()
        {
            // HARDCODED GOODS
            return new[]
            {
                MarketConvert.Main(JsonConvert.DeserializeObject<MarketJson>(
                    File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "One.json")))),
                MarketConvert.Main(JsonConvert.DeserializeObject<MarketJson>(
                    File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Two.json")))),
                MarketConvert.Main(JsonConvert.DeserializeObject<MarketJson>(
                    File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Three.json")))),
                MarketConvert.Main(JsonConvert.DeserializeObject<MarketJson>(
                    File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Four.json")))),
                MarketConvert.Main(JsonConvert.DeserializeObject<MarketJson>(
                    File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Five.json")))),
                MarketConvert.Main(JsonConvert.DeserializeObject<MarketJson>(
                    File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Six.json"))))
            };
        }
    }
}