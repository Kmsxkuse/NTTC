using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Conversion
{
    public struct GoodsCollection : IComponentData
    {
    }

    public struct GoodsCategoryCollection : IComponentData
    {
    }

    public struct GoodsEntity : IComponentData
    {
        public float Cost;
        public int Index;
        public Color32 Color;

        public bool Availability,
            OverseasPenalty,
            Money,
            Tradable;

        public Entity Category;
    }

    public struct GoodsCategoryEntity : IComponentData
    {
        public int Index;
    }

    public static class GoodsLoad
    {
        public static (Entity, Entity, List<string>, List<string>) Main()
        {
            var goods = new NativeList<EntityWrapper>(Allocator.Temp);
            var goodsCategory = new NativeList<EntityWrapper>(Allocator.Temp);
            var goodNames = new List<string>();
            var goodCategoryNames = new List<string>();

            var fileTree = new List<KeyValuePair<int, object>>();
            var values = new List<string>();

            var em = World.Active.EntityManager;
            var currentCategory = new Entity();

            FileUnpacker.ParseFile(Path.Combine(Application.streamingAssetsPath, "Common", "goods.txt"), fileTree,
                values, GoodsMagicOverride);

            foreach (var category in fileTree)
            foreach (var goodKvp in (List<KeyValuePair<int, object>>) category.Value)
            {
                var currentEntity = goods[goodKvp.Key - (int) MagicUnifiedNumbers.Goods];
                var currentGood = em.GetComponentData<GoodsEntity>(currentEntity);

                foreach (var goodProperty in (List<KeyValuePair<int, object>>) goodKvp.Value)
                {
                    var targetStr = values[(int) goodProperty.Value];
                    switch ((LoadVariables) goodProperty.Key)
                    {
                        case LoadVariables.Cost:
                            float.TryParse(targetStr, out var cost);
                            currentGood.Cost = cost;
                            break;
                        case LoadVariables.Color:
                            currentGood.Color = LoadMethods.ParseColor32(targetStr);
                            break;
                        case LoadVariables.AvailableFromStart:
                            currentGood.Availability = LoadMethods.YesNoConverter(targetStr);
                            break;
                        case LoadVariables.OverseasPenalty:
                            currentGood.OverseasPenalty = LoadMethods.YesNoConverter(targetStr);
                            break;
                        case LoadVariables.Money:
                            currentGood.Money = LoadMethods.YesNoConverter(targetStr);
                            break;
                        case LoadVariables.Tradeable:
                            currentGood.Tradable = LoadMethods.YesNoConverter(targetStr);
                            break;
                    }
                }

                em.SetComponentData(currentEntity, currentGood);
            }

            var goodCollectorEntity = FileUnpacker.GetCollector<GoodsCollection>(goods);
            goods.Dispose();

            var categoryCollectorEntity = FileUnpacker.GetCollector<GoodsCategoryCollection>(goodsCategory);
            goodsCategory.Dispose();

            return (goodCollectorEntity, categoryCollectorEntity, goodNames, goodCategoryNames);

            int GoodsMagicOverride(int parent, string target)
            {
                switch (parent)
                {
                    case -1:
                        var targetCategory = em.CreateEntity(typeof(GoodsCategoryEntity));
                        em.SetComponentData(targetCategory, new GoodsCategoryEntity {Index = goodCategoryNames.Count});
                        goodsCategory.Add(targetCategory);
                        currentCategory = targetCategory;
                        goodCategoryNames.Add(target);
                        return (int) MagicUnifiedNumbers.Placeholder;
                    case (int) MagicUnifiedNumbers.Placeholder:
                        var targetGood = em.CreateEntity(typeof(GoodsEntity));
                        em.SetComponentData(targetGood,
                            new GoodsEntity {Index = goodNames.Count, Category = currentCategory});
                        goods.Add(targetGood);
                        goodNames.Add(target);
                        return (int) MagicUnifiedNumbers.Goods + goodNames.Count - 1;
                    default:
                        return (int) MagicUnifiedNumbers.ContinueMagicNumbers;
                }
            }
        }
    }
}