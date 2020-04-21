using System.Collections.Generic;
using System.IO;
using Conversion;
using Unity.Entities;
using UnityEngine;

namespace Loading
{
    public struct NationalValueCollection : IComponentData
    {
    }

    public struct NationalValueEntity : IComponentData
    {
        public int Index;
    }

    public static class NationalValueLoad
    {
        public static (Entity, List<string>) Main()
        {
            var em = World.Active.EntityManager;

            var paths = new[]
            {
                Path.Combine(Application.streamingAssetsPath, "Common", "nationalvalues.txt")
            };

            return LoadGeneric.Main<NationalValueEntity, NationalValueCollection>(paths, SetEntityIndex);

            void SetEntityIndex(Entity target, int index)
            {
                em.SetComponentData(target, new NationalValueEntity {Index = index});
            }
        }
    }
}