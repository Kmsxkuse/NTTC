﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    [Serializable]
    public struct JsonListWrapper<TInput>
    {
        // God damn unity Json Utility.

        public List<TInput> List;

        public JsonListWrapper(List<TInput> list)
        {
            List = list;
        }

        public static implicit operator List<TInput>(JsonListWrapper<TInput> jsonListWrapper)
        {
            return jsonListWrapper.List;
        }

        public static implicit operator JsonListWrapper<TInput>(List<TInput> list)
        {
            return new JsonListWrapper<TInput>(list);
        }
    }

    [Serializable]
    public struct JsonTupleWrapper<T1, T2>
    {
        // God damn unity Json Utility.

        public T1 P1;
        public T2 P2;

        public JsonTupleWrapper((T1 p1, T2 p2) input)
        {
            P1 = input.p1;
            P2 = input.p2;
        }

        public static implicit operator (T1, T2)(JsonTupleWrapper<T1, T2> jsonTupleWrapper)
        {
            return (jsonTupleWrapper.P1, jsonTupleWrapper.P2);
        }

        public static implicit operator JsonTupleWrapper<T1, T2>((T1, T2) tuple)
        {
            return new JsonTupleWrapper<T1, T2>(tuple);
        }
    }

    public struct EntityWrapper : IBufferElementData
    {
        // Temporary. Should be redefined to pure Entity array.

        private Entity _entity;

        public static implicit operator EntityWrapper(Entity entity)
        {
            return new EntityWrapper {_entity = entity};
        }

        public static implicit operator Entity(EntityWrapper wrapper)
        {
            return wrapper._entity;
        }
    }

    public struct DataCollection : IComponentData
    {
        // Empty Tag.
    }

    [Serializable]
    public struct FirstLevelCore : IBufferElementData, IFirstLevelData<FirstLevelCore>
    {
        public int Type;
        public float Value;

        public FirstLevelCore Assignment(int type, float value)
        {
            Type = type;
            Value = value;
            return this;
        }

        public override string ToString()
        {
            return $"Key: {(LoadVariables) Type}. Value: {Value}.";
        }
    }

    [Serializable]
    public struct DataValue : IBufferElementData
    {
        public enum DataLogic
        {
            Value = 0,
            Check = 1,
            Boolean = 2
        }

        public DataLogic Logic;
        public int Type;
        public float Value;
        public int Pass;
        public int Fail;

        public DataValue(DataLogic logic, int type, float value, int pass, int fail)
        {
            Logic = logic;
            Type = type;
            Value = value;
            Pass = pass;
            Fail = fail;
        }
    }

    public interface IDataValue<out TWrapper>
    {
        TWrapper GenerateWrapper(DataValue input);
    }

    public interface IDataEntity
    {
        LoadVariables[] DefaultTypes();
        LoadVariables[] RangeTypes();
        // Assign defaults corresponds to DefaultTypes.
        void AssignDefaults(IReadOnlyList<string> construction);
    }

    public interface IDataName
    {
        void SetName(string name);
        bool GroupType();
        void SetGroup(string group);
    }

    public interface IFirstLevelData<out TSubType>
    {
        TSubType Assignment(int type, float value);
    }

    public static class LoadMethods
    {
        public static Dictionary<string, string> LocalizedReplacer;
        private static bool _useLocalization = true;

        // Thanks War Thunder Wiki Bot.
        public static string NameCleaning(string rawName)
        {
            if (_useLocalization && LocalizedReplacer.TryGetValue(rawName, out var localization))
                return localization;

            return CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(rawName.Replace('_', ' ')).Trim();
        }

        public static void DisposeLocalization()
        {
            LocalizedReplacer.Clear();
            LocalizedReplacer = null;
            _useLocalization = false;
        }

        public static string AddSpacesToSentence(string text, bool preserveAcronyms = false)
        {
            // https://stackoverflow.com/questions/272633/add-spaces-before-capital-letters
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            var newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (var i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if (text[i - 1] != ' ' && !char.IsUpper(text[i - 1]) ||
                        preserveAcronyms && char.IsUpper(text[i - 1]) &&
                        i < text.Length - 1 && !char.IsUpper(text[i + 1]))
                        newText.Append(' ');
                newText.Append(text[i]);
            }

            return newText.ToString();
        }

        public static bool YesNoConverter(string word)
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

        public static Color32 ParseColor32(string line)
        {
            var subColor = Regex.Match(line, @"\d(.*)\d").Value
                .Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            if (subColor.Length != 3)
                throw new Exception("Color invalid. { R G B }: " + line);

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
                var points = line.Count(x => x.Equals('.'));
                switch (points)
                {
                    case 3:
                    case 2:
                    case 1:
                        return false;
                    case 0:
                        return true;
                    default:
                        throw new Exception("Color invalid. { R G B }: " + line);
                }
            }
        }

        public static bool CommentDetector(string line, out string sliced)
        {
            // Comment Detector. Will also lowercase everything. Throwing away comments.
            sliced = line.ToLowerInvariant().Split(new[] {"#"}, StringSplitOptions.None)[0].Trim();
            return sliced.Length == 0;
        }

        public static void GenerateCacheEntities<TEntity, TCollection>(IReadOnlyList<TEntity> entities, IReadOnlyList<string> names,
            IReadOnlyList<string> groups = null)
            where TEntity : struct, IComponentData, IDataName
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            for (var cursor = 0; cursor < names.Count; cursor++)
            {
                var entity = entities[cursor];
                entity.SetName(names[cursor]);
                if(entity.GroupType())
                    entity.SetGroup(groups[cursor]);
                em.SetComponentData(em.CreateEntity(typeof(TEntity)), entity);
            }

            FileUnpacker.GetCollector<TCollection>();
        }

        private static void CopyTo(Stream src, Stream dest)
        {
            // https://stackoverflow.com/questions/7343465/compression-decompression-string-with-c-sharp
            var bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) dest.Write(bytes, 0, cnt);
        }

        public static byte[] Zip(string str, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            var bytes = encoding.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes, Encoding encoding = null)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                if (encoding == null)
                    encoding = Encoding.UTF8;

                return encoding.GetString(mso.ToArray());
            }
        }

        /*
        public static (Entity, EntityManager) MapEntity()
        {
            // Must be located in start!
            var em = World.Active.EntityManager;

            using (var entityArray = em.CreateEntityQuery(typeof(Map)).ToEntityArray(Allocator.TempJob))
            {
                return (entityArray[0], em);
            }
        }
        */
    }
}