using System;
using System.IO;
using System.Text.RegularExpressions;
using Loading;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


public struct ProvPopInfo : IBufferElementData, IEquatable<ProvPopInfo>
{
    public int Employment;
    public int Culture;
    public int Religion;
    public int Size;

    public bool Equals(ProvPopInfo other)
    {
        return Employment == other.Employment && Culture == other.Culture && Religion == other.Religion;
    }

    public override bool Equals(object obj)
    {
        return obj is ProvPopInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Employment;
            hashCode = (hashCode * 397) ^ Culture;
            hashCode = (hashCode * 397) ^ Religion;
            return hashCode;
        }
    }

    public static bool operator ==(ProvPopInfo left, ProvPopInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ProvPopInfo left, ProvPopInfo right)
    {
        return !left.Equals(right);
    }
}

public static class ProvincePopulationLoad
{
    public static void Main(Entity provinces, NativeHashMap<int, int> idIndex)
    {
        var em = World.Active.EntityManager;
        var provinceList = em.GetBuffer<EntityWrapper>(provinces).AsNativeArray();

        // TODO: Possible parallel?
        foreach (var regionPop in Directory.EnumerateFiles(
            Path.Combine(Application.streamingAssetsPath, "History", "pops", "1836.1.1"),
            "*.txt"))
        {
            var curProv = 0;
            bool employToggle = false, provToggle = false;
            var currentPop = new ProvPopInfo();

            foreach (var line in File.ReadLines(regionPop))
            {
                if (LoadMethods.CommentDetector(line, out var commentSplit))
                    continue;

                if (commentSplit.Contains("{"))
                {
                    if (provToggle)
                    {
                        employToggle = true;
                        currentPop = new ProvPopInfo
                        {
                            Employment = LookupDictionaries.PopTypes[
                                Regex.Match(commentSplit, @"^.+(?=\=)").Value.Trim()]
                        };
                        continue;
                    }

                    provToggle = true;
                    int.TryParse(Regex.Match(commentSplit, @"\d+").Value, out curProv);
                    continue;
                }

                if (employToggle)
                {
                    var equalsSplit = commentSplit.Split(new[] {"="}, StringSplitOptions.RemoveEmptyEntries);
                    switch (equalsSplit[0].Trim())
                    {
                        case "culture":
                            currentPop.Culture = LookupDictionaries.Cultures[equalsSplit[1].Trim()];
                            break;
                        case "religion":
                            currentPop.Religion = LookupDictionaries.Religions[equalsSplit[1].Trim()];
                            break;
                        case "size":
                            if (!int.TryParse(Regex.Match(equalsSplit[1], @"\d+").Value, out var value))
                                throw new Exception("Unknown population size: " + equalsSplit[1]);
                            currentPop.Size = value;
                            break;
                    }
                }

                if (!commentSplit.Contains("}"))
                    continue;

                if (employToggle)
                {
                    employToggle = false;
                    em.GetBuffer<ProvPopInfo>(provinceList[idIndex[curProv]]).Add(currentPop);
                    continue;
                }

                provToggle = false;
            }
        }
    }
}