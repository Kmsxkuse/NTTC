using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct CultureCollection : IComponentData
    {
    }

    [Serializable]
    public struct CultureEntity : IComponentData, IDataName
    {
        public int Primary, Union; // I serve the Soviet YunYun
        public Color32 Color;
        public float Radicalism;
        public NativeString64 Group, Name;

        public void SetName(string name)
        {
            Name = name;
        }

        public bool GroupType()
        {
            return true;
        }

        public void SetGroup(string group)
        {
            Group = group;
        }
    }

    public static class CulturesLoad
    {
        public static (List<string>, List<string>) Main(bool cache)
        {
            var cultures = new List<CultureEntity>();
            var cultureNames = new List<string>();
            var cultureGroupNames = new List<string>();

            if (cache)
            {
                (cultureNames, cultureGroupNames, cultures) = JsonUtility.FromJson<CulturesOutput>(
                    LoadMethods.Unzip(File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "JsonData", "cultures.txt"))));

                LoadMethods.GenerateCacheEntities<CultureEntity, CultureCollection>(cultures, cultureNames, cultureGroupNames);

                return (cultureNames, cultureGroupNames);
            }

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            bool groupToggle = false,
                cultureToggle = false,
                ignoreToggle = false;
            var currentCulture = new CultureEntity();

            foreach (var line in File.ReadLines(Path.Combine(Application.streamingAssetsPath, "common",
                "cultures.txt")))
            {
                if (LoadMethods.CommentDetector(line, out var commentSplit))
                    continue;

                if (ignoreToggle)
                {
                    if (commentSplit.Contains("}"))
                        ignoreToggle = false;

                    // Assuming no values defined after end brackets
                    continue;
                }

                var equalsSplit = Regex.Match(commentSplit, @"^.+?(?=\=)");
                var preEquals = equalsSplit.Success ? equalsSplit.Value.Trim() : "";

                if (commentSplit.Contains("{"))
                {
                    if (!groupToggle)
                    {
                        groupToggle = true;
                        cultureGroupNames.Add(preEquals);
                        continue;
                    }

                    if (!commentSplit.Contains("_names") && !commentSplit.Contains("color"))
                    {
                        cultureToggle = true;
                        var name = preEquals.Trim();
                        cultureNames.Add(name);
                        currentCulture = new CultureEntity {Group = cultureGroupNames.Last(), Name = name};
                    }
                }

                switch (preEquals)
                {
                    case "union":
                        currentCulture.Union =
                            LookupDictionaries.CountryTags[commentSplit.Substring(equalsSplit.Length + 1).Trim()];
                        continue;
                    case "first_names":
                    case "last_names":
                        // TODO: Implement names generation, someday.
                        if (!commentSplit.Contains("}"))
                            ignoreToggle = true;
                        continue;
                    case "color":
                        currentCulture.Color =
                            LoadMethods.ParseColor32(commentSplit.Substring(equalsSplit.Length + 1));
                        continue;
                    case "leader":
                    case "unit":
                        continue;
                    case "radicalism":
                        if (!float.TryParse(commentSplit.Substring(equalsSplit.Length + 1), out var radical))
                            throw new Exception("Unknown radicalism: " +
                                                commentSplit.Substring(equalsSplit.Length + 1));

                        currentCulture.Radicalism = radical;
                        continue;
                    case "primary":
                        currentCulture.Primary =
                            LookupDictionaries.CountryTags[commentSplit.Substring(equalsSplit.Length + 1).Trim()];
                        continue;
                }

                if (!commentSplit.Contains("}"))
                    continue;

                if (cultureToggle)
                {
                    cultureToggle = false;
                    var targetCulture = em.CreateEntity(typeof(CultureEntity));
                    em.SetComponentData(targetCulture, currentCulture);

                    cultures.Add(currentCulture);
                    continue;
                }

                groupToggle = false;
            }

            File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "JsonData", "cultures.txt"),
                LoadMethods.Zip(JsonUtility.ToJson(
                    new CulturesOutput(cultureNames, cultureGroupNames, cultures))));

            FileUnpacker.GetCollector<CultureCollection>();

            return (cultureNames, cultureGroupNames);
        }

        [Serializable]
        private struct CulturesOutput
        {
            public List<string> Cultures, CultureGroups;
            public List<CultureEntity> CultureEntities;

            public CulturesOutput(List<string> cultures, List<string> cultureGroups, List<CultureEntity> cultureEntities)
            {
                Cultures = cultures;
                CultureGroups = cultureGroups;
                CultureEntities = cultureEntities;
            }

            public void Deconstruct(out List<string> cultureNames, out List<string> cultureGroups, out List<CultureEntity> cultureEntities)
            {
                cultureNames = Cultures;
                cultureGroups = CultureGroups;
                cultureEntities = CultureEntities;
            }
        }
    }
}