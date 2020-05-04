using System.IO;
using Newtonsoft.Json;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public static class AgentsLoad
    {
        public static (BlobAssetReference<MarketMatrix>[], int[]) Main()
        {
            // HARDCODED GOODS
            var one = JsonConvert.DeserializeObject<MarketJson>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "One.json")));
            var two = JsonConvert.DeserializeObject<MarketJson>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Two.json")));
            var three = JsonConvert.DeserializeObject<MarketJson>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Three.json")));
            var four = JsonConvert.DeserializeObject<MarketJson>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Four.json")));
            var five = JsonConvert.DeserializeObject<MarketJson>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Five.json")));
            var six = JsonConvert.DeserializeObject<MarketJson>(
                File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Custom", "Agents", "Six.json")));

            return (new[]
            {
                MarketConvert.Main(one), MarketConvert.Main(two), MarketConvert.Main(three), MarketConvert.Main(four),
                MarketConvert.Main(five), MarketConvert.Main(six)
            }, new[]
            {
                one.MaximumEmployment, two.MaximumEmployment,
                three.MaximumEmployment, four.MaximumEmployment, five.MaximumEmployment, six.MaximumEmployment
            });
        }
    }
}