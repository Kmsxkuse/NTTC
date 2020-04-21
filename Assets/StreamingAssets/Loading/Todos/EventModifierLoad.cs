using System.Collections.Generic;
using System.IO;
using Conversion;
using Unity.Entities;
using UnityEngine;

namespace Loading
{
    public struct EventModifierCollection : IComponentData
    {
    }

    public struct EventModifierEntity : IComponentData
    {
        public int Index;
    }

    public static class EventModifierLoad
    {
        public static (Entity, List<string>) Main()
        {
            var em = World.Active.EntityManager;

            var paths = new[]
            {
                Path.Combine(Application.streamingAssetsPath, "Common", "event_modifiers.txt"),
                Path.Combine(Application.streamingAssetsPath, "Common", "static_modifiers.txt")
            };

            return LoadGeneric.Main<EventModifierEntity, EventModifierCollection>(paths, SetEntityIndex);

            void SetEntityIndex(Entity target, int index)
            {
                em.SetComponentData(target, new EventModifierEntity {Index = index});
            }
        }
    }
}