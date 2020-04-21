using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct ReligionCollection : IComponentData
    {
    }

    [Serializable]
    public struct ReligionEntity : IComponentData, IDataName
    {
        public NativeString64 Group, Name;
        //public int Index;
        public Color32 Color;

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

    public static class ReligionsLoad
    {
        public static (List<string>, List<string>) Main(bool cache)
        {
            var groupNames = new List<string>();
            var religionNames = new List<string>();
            var religionEntities = new List<ReligionEntity>();

            if (cache)
            {
                (religionNames, groupNames, religionEntities) = JsonUtility.FromJson<ReligionsOutput>(
                    LoadMethods.Unzip(File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "JsonData", "religion.txt"))));

                LoadMethods.GenerateCacheEntities<ReligionEntity, ReligionCollection>(religionEntities, religionNames, groupNames);

                return (religionNames, groupNames);
            }

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            bool groupToggle = false, religionToggle = false, colorToggle = false;
            var currentReligion = new ReligionEntity();

            foreach (var line in File.ReadLines(Path.Combine(Application.streamingAssetsPath, "Common",
                "religion.txt")))
            {
                if (LoadMethods.CommentDetector(line, out var commentSplit))
                    continue;

                var equalsSplit = commentSplit.Split(new[] {"="}, StringSplitOptions.RemoveEmptyEntries);

                if (commentSplit.Contains("{"))
                {
                    if (!groupToggle)
                    {
                        groupToggle = true;

                        groupNames.Add(equalsSplit[0].Trim());
                        continue;
                    }

                    if (!religionToggle)
                    {
                        religionToggle = true;
                        currentReligion = new ReligionEntity {Group = groupNames.Last()};
                        religionNames.Add(equalsSplit[0].Trim());
                        continue;
                    }

                    if (!colorToggle)
                        colorToggle = true;
                }

                switch (equalsSplit[0].Trim())
                {
                    case "color":
                        // Why religion colors are in float, I have absolutely no idea.
                        currentReligion.Color = LoadMethods.ParseColor32(equalsSplit[1]);
                        break;
                }

                if (!commentSplit.Contains("}"))
                    continue;

                if (colorToggle)
                {
                    colorToggle = false;
                    continue;
                }

                if (religionToggle)
                {
                    religionToggle = false;

                    var targetEntity = em.CreateEntity(typeof(ReligionEntity));
                    em.SetComponentData(targetEntity, currentReligion);
                    religionEntities.Add(currentReligion);

                    continue;
                }

                groupToggle = false;
            }

            FileUnpacker.GetCollector<ReligionCollection>();

            File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "JsonData", "religion.txt"),
                LoadMethods.Zip(JsonUtility.ToJson(new ReligionsOutput(groupNames, religionNames, religionEntities))));

            /*
            var test = em.GetBuffer<EntityWrapper>(religionCollectorEntity);
            foreach (var religion in test.AsNativeArray())
            {
                var religionEntity = em.GetComponentData<ReligionEntity>(religion.Entity);
                Debug.Log(religionNames[religionEntity.Index]
                          + " of group " + groupNames[em.GetComponentData<ReligionGroupEntity>(religionEntity.Group).Index]);
            }
            */

            return (religionNames, groupNames);
        }

        [Serializable]
        private struct ReligionsOutput
        {
            public List<string> GroupNames, ReligionNames;
            public List<ReligionEntity> ReligionEntities;

            public ReligionsOutput(List<string> groupNames, List<string> religionNames, List<ReligionEntity> religionEntities)
            {
                GroupNames = groupNames;
                ReligionNames = religionNames;
                ReligionEntities = religionEntities;
            }
            
            public void Deconstruct(out List<string> groupNames, out List<string> religionNames, out List<ReligionEntity> religionEntities)
            {
                groupNames = GroupNames;
                religionNames = ReligionNames;
                religionEntities = ReligionEntities;
            }

        }
    }
}