using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct CountryPartyCollection : IComponentData
    {
    }

    public struct CountryEntity : IComponentData
    {
        public Color32 Color;
        public int Index;
        public bool GovernColors;
    }

    public struct CountryParty : IComponentData
    {
        public int Index, StartDate, EndDate, Ideology;
    }


    public struct CountryGovernColors : IBufferElementData
    {
        public readonly int Type;
        public readonly Color32 Color;

        public CountryGovernColors(int type, Color32 color)
        {
            Type = type;
            Color = color;
        }
    }

    public static class CountriesLoad
    {
        public static (List<string>, Dictionary<string, Entity>, List<string>) Names()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var countryTags = new Dictionary<string, Entity>();
            var countries = new List<string>();
            var countryPaths = new List<string>();

            // Creating default countries.
            var target = em.CreateEntity(typeof(Country));
            em.SetComponentData(target, new Country {Color = new Color32(0, 191, 255, 255)});
            countryTags.Add("OCEAN", target);
            em.SetName(target, "Country: Ocean"); // DEBUG
            
            target = em.CreateEntity(typeof(Country));
            em.SetComponentData(target, new Country {Color = new Color32(255, 228, 181, 255)});
            countryTags.Add("UNCOLONIZED", target);
            em.SetName(target, "Country: Uncolonized"); // DEBUG

            foreach (var rawCommonLine in File.ReadLines(Path.Combine(Application.streamingAssetsPath,
                "Common", "countries.txt")))
            {
                if (CommentDetector(rawCommonLine, out var line))
                    continue;

                var tag = Regex.Match(line, @"^.+?(?=\=)").Value.Trim();
                if (string.IsNullOrEmpty(tag))
                    throw new Exception("No tag found. " + line);

                if (tag.Equals("dynamic_tags"))
                {
                    if (YesNoConverter(Regex.Match(line, @"(?<=\=).+$").Value))
                        continue;

                    break;
                }

                var targetFile = Regex.Match(line, "(?<=\").+(?=\")").Value.Trim();
                if (string.IsNullOrEmpty(targetFile))
                    throw new Exception("No country file found. " + line);

                //countryPaths.Add(Path.Combine(Application.streamingAssetsPath, "Common", targetFile));
                //countries.Add(Path.GetFileNameWithoutExtension(countryPaths.Last()));

                var fileTree = new List<(string, object)>();
                ParseFile.Main(Path.Combine(Application.streamingAssetsPath,
                    "common", targetFile), fileTree);

                var currentCountry = new Country();

                foreach (var (key, value) in fileTree)
                {
                    if (!key.Equals("color"))
                        continue;

                    var color = ParseColor32((string) value);
                    currentCountry.Color = color;
                    break;
                }

                target = em.CreateEntity(typeof(Country));
                em.SetComponentData(target, currentCountry);
                countryTags.Add(tag, target);
                em.SetName(target, "Country: " + Path.GetFileNameWithoutExtension(targetFile)); // DEBUG
            }

            return (countries, countryTags, countryPaths);

            bool CommentDetector(string line, out string sliced)
            {
                // Comment Detector. Will also lowercase everything. Throwing away comments.
                sliced = line.ToLowerInvariant().Split(new[] {"#"}, StringSplitOptions.None)[0].Trim();
                return sliced.Length == 0;
            }

            bool YesNoConverter(string word)
            {
                switch (word.Trim())
                {
                    case "yes":
                        return true;
                    case "no":
                        return false;
                    default:
                        throw new Exception("Unknown yes/no. " + word);
                }
            }

            Color32 ParseColor32(string colorString)
            {
                var subColor = Regex.Match(colorString, @"\d(.*)\d").Value
                    .Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                if (subColor.Length != 3)
                    throw new Exception("Color invalid. { R G B }: " + colorString);

                if (FloatDetector())
                {
                    byte.TryParse(subColor[0], out var r);
                    byte.TryParse(subColor[1], out var g);
                    byte.TryParse(subColor[2], out var b);
                    return new Color32(r, g, b, 255);
                }
                else
                {
                    float.TryParse(subColor[0], out var r);
                    float.TryParse(subColor[1], out var g);
                    float.TryParse(subColor[2], out var b);
                    return new Color(r, g, b, 1);
                }

                bool FloatDetector()
                {
                    var points = colorString.Count(x => x.Equals('.'));
                    switch (points)
                    {
                        case 3:
                        case 2:
                        case 1:
                            return false;
                        case 0:
                            return true;
                        default:
                            throw new Exception("Color invalid. { R G B }: " + colorString);
                    }
                }
            }
        }

        [Serializable]
        public struct CountryNameOutput
        {
            public List<string> CountryTags, Countries, CountryPaths;

            public CountryNameOutput(List<string> countryTags, List<string> countries, List<string> countryPaths)
            {
                CountryTags = countryTags;
                Countries = countries;
                CountryPaths = countryPaths;
            }

            public static implicit operator (List<string>, List<string>, List<string>)(CountryNameOutput countryNameOutput)
            {
                return (countryNameOutput.Countries, countryNameOutput.CountryTags, countryNameOutput.CountryPaths);
            }
        }

        /*
        public static (Entity, List<string>) Main(Entity countries, IEnumerable<string> countryPaths)
        {
            var countryPartyNames = new List<string>();
            var parties = new NativeList<EntityWrapper>(Allocator.Temp);

            var em = World.Active.EntityManager;

            using (var governColors = new NativeList<CountryGovernColors>(Allocator.Temp))
            {
                foreach (var countryPath in countryPaths)
                {
                    var fileTree = new List<KeyValuePair<int, object>>();
                    var values = new List<string>();

                    FileUnpacker.ParseFile(countryPath, fileTree, values, CountryMagicOverride);

                    var currentCountry = new CountryEntity();

                    foreach (var countryKvP in fileTree)
                        switch ((LoadVariables) countryKvP.Key)
                        {
                            case LoadVariables.Party:
                                var currentParty = new CountryParty();
                                using (var tempPolicies = new NativeList<DataValue>(Allocator.Temp))
                                {
                                    foreach (var partyKvP in (List<KeyValuePair<int, object>>) countryKvP.Value)
                                    {
                                        var targetStr = values[(int) partyKvP.Value];
                                        switch ((LoadVariables) partyKvP.Key)
                                        {
                                            case LoadVariables.Name:
                                                currentParty.Index = countryPartyNames.Count;
                                                countryPartyNames.Add(targetStr);
                                                break;
                                            case LoadVariables.StartDate:
                                                if (!DateTime.TryParse(targetStr, out var startResult))
                                                    throw new Exception("Unknown date. " + targetStr);
                                                currentParty.StartDate = int.Parse(startResult.ToString("yyyyMMdd"));
                                                break;
                                            case LoadVariables.EndDate:
                                                if (!DateTime.TryParse(targetStr, out var endResult))
                                                    throw new Exception("Unknown date. " + targetStr);
                                                currentParty.EndDate = int.Parse(endResult.ToString("yyyyMMdd"));
                                                break;
                                            case LoadVariables.Ideology:
                                                if (!LookupDictionaries.Ideologies.TryGetValue(targetStr,
                                                    out var ideologyIndex))
                                                    throw new Exception("Unknown ideology. " + targetStr);
                                                currentParty.Ideology = ideologyIndex;
                                                break;
                                            default:
                                                if (partyKvP.Key / 10000 * 10000 !=
                                                    (int) MagicUnifiedNumbers.PolicyGroup)
                                                    break;
                                                if (!LookupDictionaries.SubPolicies.TryGetValue(
                                                    values[(int) partyKvP.Value],
                                                    out var subPolicyIndex))
                                                    throw new Exception(
                                                        "Unknown sub policy. " + values[(int) partyKvP.Value]);

                                                tempPolicies.Add(new DataValue(
                                                    partyKvP.Key - (int) MagicUnifiedNumbers.PolicyGroup,
                                                    subPolicyIndex));
                                                break;
                                        }

                                        var targetParty = em.CreateEntity(typeof(CountryParty));
                                        em.SetComponentData(targetParty, currentParty);
                                        em.AddBuffer<DataValue>(targetParty).AddRange(tempPolicies);
                                        parties.Add(targetParty);
                                    }
                                }

                                break;
                            case LoadVariables.Color:
                                currentCountry.Color = LoadMethods.ParseColor32(values[(int) countryKvP.Value]);
                                break;
                            default:
                                if (countryKvP.Key / 10000 * 10000 != (int) MagicUnifiedNumbers.Government)
                                    break;

                                currentCountry.GovernColors = true;
                                governColors.Add(new CountryGovernColors(
                                    countryKvP.Key - (int) MagicUnifiedNumbers.Government,
                                    LoadMethods.ParseColor32(values[(int) countryKvP.Value])));
                                break;
                        }

                    var targetCountry = em.CreateEntity(typeof(CountryEntity));
                    em.SetComponentData(targetCountry, currentCountry);
                    // Govern colors can be empty.
                    em.AddBuffer<CountryGovernColors>(targetCountry).AddRange(governColors);
                    governColors.Clear();

                    countries.Add(targetCountry);

                    int CountryMagicOverride(int parent, string target)
                    {
                        // Color was already parsed and used.
                        if (parent == (int) LoadVariables.UnitNames)
                            return (int) MagicUnifiedNumbers.SkipMagicNumbers;

                        return (int) MagicUnifiedNumbers.ContinueMagicNumbers;
                    }
                }
            }

            var countryPartyCollector = FileUnpacker.GetCollector<CountryPartyCollection>(parties);
            parties.Dispose();

            return (countryPartyCollector, countryPartyNames);
        }
        */
    }
}