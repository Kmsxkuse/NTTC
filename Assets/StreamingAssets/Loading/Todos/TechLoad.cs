using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    [Serializable]
    public struct TechInfo
    {
        public int area,
            year,
            cost,
            rangeIndex;
    }

    public struct FolderCollection : IComponentData
    {
    }

    public struct SchoolCollection : IComponentData
    {
    }

    public struct TechnologyCollection : IComponentData
    {
    }

    public struct FolderEntity : IComponentData
    {
        public int Index;
    }

    public struct SchoolEntity : IComponentData
    {
        public int Index;
    }

    public struct AreaEntity : IComponentData
    {
        public int Index;
        public Entity Folder;
    }

    public struct TechnologyEntity : IComponentData
    {
        public int Index, Year, Cost;
        public Entity Area;
    }

    public static class TechLoad
    {
        public static (Entity, Entity, Entity, List<List<IncompleteInventions>>) Main(ref StringBox stringBox)
        {
            var folderNames = new List<string>();
            var areaNames = new List<string>();
            var schoolNames = new List<string>();
            var technologyNames = new List<string>();

            var techLookup = new Dictionary<string, int>();
            var areaLookup = new Dictionary<string, int>();
            var foldersLookup = new Dictionary<string, int>();
            var schoolReadBeforeFolders = new Queue<string>();
            var schoolQueueing = false;

            var folders = new NativeList<EntityWrapper>(Allocator.Temp);
            var schools = new NativeList<EntityWrapper>(Allocator.Temp);
            var areas = new NativeList<EntityWrapper>(Allocator.Temp);
            var technologies = new NativeList<EntityWrapper>(Allocator.Temp);

            var currentUpper = CurrentUpper.None;
            var lowerToggle = false;
            var em = World.Active.EntityManager;
            var currentFolder = new Entity();
            var currentSchool = new Entity();

            using (var tempAreas = new NativeList<EntityWrapper>(Allocator.Temp))
            using (var tempSchoolValues = new NativeList<DataValue>(Allocator.Temp))
            {
                foreach (var rawLine in File.ReadLines(Path.Combine(Application.streamingAssetsPath,
                    "Common", "technology.txt")))
                {
                    if (LoadMethods.CommentDetector(rawLine, out var line))
                        continue;

                    ParseCommonTechFile(line);
                }

                while (schoolReadBeforeFolders.Any())
                    ParseCommonTechFile(schoolReadBeforeFolders.Dequeue());

                void ParseCommonTechFile(string line)
                {
                    if (schoolQueueing)
                    {
                        // Buffer school section until exit.
                        // Folder section contains critical info needed for schools parsing.

                        if (line.Contains("{"))
                            if (!lowerToggle)
                                lowerToggle = true;
                            else
                                throw new Exception("Exceeded maximum nesting in Common/technology.txt.");

                        if (line.Contains("}"))
                            if (lowerToggle)
                                lowerToggle = false;
                            else
                                schoolQueueing = false;

                        schoolReadBeforeFolders.Enqueue(line);
                        return;
                    }

                    var preEquals = Regex.Match(line, @"^.+?(?=\=)");
                    switch (currentUpper)
                    {
                        case CurrentUpper.None:
                            switch (preEquals.Value.Trim())
                            {
                                case "folders":
                                    currentUpper = CurrentUpper.Folder;
                                    break;
                                case "schools":
                                    if (folders.Length < 1)
                                    {
                                        schoolQueueing = true;
                                        schoolReadBeforeFolders.Enqueue(line);
                                        return;
                                    }

                                    for (var i = 0; i < folders.Length; i++)
                                        foldersLookup.Add(folderNames[i], i);

                                    currentUpper = CurrentUpper.Schools;
                                    break;
                                default:
                                    throw new NotImplementedException("Unknown tech type: " + line);
                            }

                            break;
                        case CurrentUpper.Folder:
                            if (!lowerToggle)
                            {
                                if (line.Contains("}"))
                                    break;

                                lowerToggle = true;
                                currentFolder = em.CreateEntity(typeof(FolderEntity));
                                em.SetComponentData(currentFolder, new FolderEntity {Index = folderNames.Count});
                                folderNames.Add(preEquals.Value.Trim());
                                break;
                            }

                            if (preEquals.Success)
                                throw new Exception("Invalid equals detected! " + line);

                            var newFolderEntry = line.Replace("}", "").Trim();

                            if (newFolderEntry == string.Empty)
                                break;

                            areaLookup.Add(newFolderEntry, areaNames.Count);
                            var targetArea = em.CreateEntity(typeof(AreaEntity));
                            em.SetComponentData(targetArea,
                                new AreaEntity {Index = areaNames.Count, Folder = currentFolder});
                            tempAreas.Add(targetArea);
                            areas.Add(targetArea);
                            areaNames.Add(newFolderEntry);
                            break;
                        case CurrentUpper.Schools:
                            if (!lowerToggle)
                            {
                                if (line.Contains("}"))
                                    break;

                                lowerToggle = true;

                                currentSchool = em.CreateEntity(typeof(SchoolEntity));
                                em.SetComponentData(currentSchool, new SchoolEntity {Index = schoolNames.Count});
                                schoolNames.Add(preEquals.Value.Trim());
                                break;
                            }

                            var newSchoolEntry = preEquals.Value.Replace("}", "").Trim();

                            if (newSchoolEntry == string.Empty)
                                break;

                            int assignedNumber;
                            if (!Enum.TryParse(newSchoolEntry.Replace("_", ""),
                                true, out LoadVariables foundVar))
                            {
                                if (!foldersLookup.TryGetValue(
                                    Regex.Match(newSchoolEntry, @"^.+?(?=_research_bonus)").Value,
                                    out var folderIndex))
                                    throw new Exception("Unknown folder _research_bonus: " + newSchoolEntry);
                                assignedNumber = folderIndex + (int) MagicUnifiedNumbers.ResearchBonus;
                            }
                            else
                            {
                                assignedNumber = (int) foundVar;
                            }

                            if (!float.TryParse(line.Substring(preEquals.Length + 1), out var schoolValue))
                                throw new Exception("Unknown float value. " + line.Substring(preEquals.Length + 1));

                            if (math.abs(schoolValue) < 0.01)
                                break;

                            tempSchoolValues.Add(new DataValue(assignedNumber, schoolValue));
                            break;
                    }

                    if (!line.Contains("}"))
                        return;

                    if (lowerToggle)
                    {
                        lowerToggle = false;
                        if (currentUpper == CurrentUpper.Schools)
                        {
                            em.AddBuffer<DataValue>(currentSchool).AddRange(tempSchoolValues);
                            tempSchoolValues.Clear();
                            schools.Add(currentSchool);
                        }
                        else
                        {
                            // Folders
                            em.AddBuffer<EntityWrapper>(currentFolder).AddRange(tempAreas);
                            tempAreas.Clear();
                            folders.Add(currentFolder);
                        }
                    }
                    else
                    {
                        currentUpper = CurrentUpper.None;
                    }
                }
            }

            // Actual tech parsing now
            stringBox.SchoolNames = schoolNames;
            stringBox.FolderNames = folderNames;
            stringBox.AreaNames = areaNames;

            var allTechInventions = new List<List<IncompleteInventions>>();

            foreach (var techPath in Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, "Technologies"),
                "*.txt"))
            {
                if (!foldersLookup.TryGetValue(Path.GetFileNameWithoutExtension(techPath), out _))
                    throw new Exception("Unknown tech folder type! " + techPath);

                var fileTree = new List<KeyValuePair<int, object>>();
                var values = new List<string>();

                FileUnpacker.ParseFile(techPath, fileTree, values, TechMagicOverride);

                using (var parentLocation = new NativeMultiHashMap<int, int>(10, Allocator.TempJob))
                {
                    foreach (var technology in fileTree)
                    {
                        var techRanges = new List<DataRange>();
                        var techActions = new List<DataValue>();
                        var passInventions = new List<IncompleteInventions>();

                        parentLocation.Add(technology.Key, techRanges.Count);
                        techRanges.Add(new DataRange(technology.Key, -1, -1));

                        var currentTechnology = new TechnologyEntity
                        {
                            Index = technologyNames.Count,
                            Year = -1
                        };

                        technologyNames.Add(values[technology.Key - (int) MagicUnifiedNumbers.Technology]);

                        FileUnpacker.ProcessQueue(technology, techActions, techRanges,
                            parentLocation, values, TechSwitchOverride);

                        var targetTechnology = em.CreateEntity(typeof(TechnologyEntity));
                        em.SetComponentData(targetTechnology, currentTechnology);
                        em.AddBuffer<EntityWrapper>(targetTechnology); // Inventions associated with technology.
                        technologies.Add(targetTechnology);

                        using (var tempRange = new NativeArray<DataRange>(techRanges.ToArray(), Allocator.Temp))
                        {
                            em.AddBuffer<DataRange>(targetTechnology).AddRange(tempRange);
                        }

                        using (var tempValues = new NativeArray<DataValue>(techActions.ToArray(), Allocator.Temp))
                        {
                            em.AddBuffer<DataValue>(targetTechnology).AddRange(tempValues);
                        }

                        allTechInventions.Add(passInventions);

                        bool TechSwitchOverride(string targetStr, KeyValuePair<int, object> target)
                        {
                            switch ((LoadVariables) target.Key)
                            {
                                case LoadVariables.Area:
                                    if (!areaLookup.TryGetValue(targetStr, out var areaIndex))
                                        throw new Exception("Unknown area. " + targetStr);

                                    currentTechnology.Area = areas[areaIndex];
                                    return true;
                                case LoadVariables.Year:
                                    if (!int.TryParse(targetStr, out var year))
                                        throw new Exception("Unknown year. " + targetStr);

                                    if (currentTechnology.Year == -1)
                                        currentTechnology.Year = year;
                                    else
                                        techActions.Add(new DataValue(target.Key, year));

                                    return true;
                                case LoadVariables.Cost:
                                    if (!int.TryParse(targetStr, out var cost))
                                        throw new Exception("Unknown cost. " + targetStr);

                                    currentTechnology.Cost = cost;
                                    return true;
                                case LoadVariables.Invention: // Inventions are completed after inventions are parsed
                                    passInventions.Add(new IncompleteInventions(targetStr, techActions.Count));
                                    techActions.Add(new DataValue((int) LoadVariables.Invention, -1));
                                    return true;
                                default:
                                    return false;
                            }
                        }
                    }
                }

                int TechMagicOverride(int parent, string str)
                {
                    if (parent == -1)
                    {
                        values.Add(str);
                        techLookup.Add(str, values.Count - 1);
                        return (int) MagicUnifiedNumbers.Technology + values.Count - 1;
                    }

                    // Previous technology
                    if (techLookup.TryGetValue(str, out var techIndex))
                        return techIndex + (int) MagicUnifiedNumbers.Technology;

                    return (int) MagicUnifiedNumbers.ContinueMagicNumbers;
                }
            }

            stringBox.TechNames = technologyNames;

            var techCollector = FileUnpacker.GetCollector<TechnologyCollection>(technologies);
            technologies.Dispose();

            var schoolCollector = FileUnpacker.GetCollector<SchoolCollection>(schools);
            schools.Dispose();

            var folderCollector = FileUnpacker.GetCollector<FolderCollection>(folders);
            folders.Dispose();

            return (techCollector, schoolCollector, folderCollector, allTechInventions);
        }

        private enum CurrentUpper
        {
            None,
            Folder,
            Schools
        }
    }
}