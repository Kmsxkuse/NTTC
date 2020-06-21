using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Conversion
{
    public static class RiversLoad
    {
        public static void Main()
        {
            var test = new[]
            {
                new Water("Ocean", new Color32(255, 0, 128, 255), 1),
                new Water("Land", new Color32(255, 255, 255, 255), 1),
                new Water("Start", new Color32(0, 255, 0, 255), 1),
                new Water("Merge", new Color32(255, 0, 0, 255), 1),
                new Water("River 1", new Color32(0, 0, 100, 255), 1),
                new Water("River 2", new Color32(0, 0, 150, 255), 1),
                new Water("River 3", new Color32(0, 0, 200, 255), 1),
                new Water("River 4", new Color32(0, 0, 255, 255), 1),
                new Water("River 5", new Color32(0, 100, 255, 255), 1),
                new Water("River 6", new Color32(0, 200, 255, 255), 1),
                new Water("River 7", new Color32(0, 255, 255, 255), 1)
            };

            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "map", "river.json"),
                JsonConvert.SerializeObject(test, Formatting.Indented));
        }
    }
}