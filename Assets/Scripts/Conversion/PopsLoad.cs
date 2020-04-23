using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class PopsLoad
{
    public static void Main()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        
        // HARDCODED!
        var popIdentity = em.AddBuffer<MarketMatrix>(em.CreateEntity(typeof(MarketIdentity)));
        // Good 1
        popIdentity.Add(new MarketMatrix
        {
            Delta = 1,
            Demand = 4,
            Priority = 1
        });
        // Good 2
        popIdentity.Add(new MarketMatrix
        {
            Delta = 1,
            Demand = 2,
            Priority = 2
        });
        // Good 3
        popIdentity.Add(new MarketMatrix
        {
            Delta = 1,
            Demand = 2,
            Priority = 2
        });

        using (var provinces = 
            em.CreateEntityQuery(typeof(Province)).ToEntityArray(Allocator.TempJob))
        {
            foreach (var province in provinces)
            {
                var rand = Random.Range(3, 8); // 3 to 7 different types of pops.
                var randomPop = new NativeArray<PopWrapper>(rand, Allocator.Temp);
                for (var i = 0; i < rand; i++)
                {
                    var pop = new Population
                    {
                        Culture = i,
                        Employment = 0,
                        Quantity = Random.Range(300, 1500),
                        Religion = 0,
                        Satisfaction = 0,
                        Wealth = 500
                    };

                    var targetPop = em.CreateEntity(typeof(Population));
                    em.SetComponentData(targetPop, pop);
                    randomPop[i] = targetPop;

                    // HARDCODED 3 types of goods!
                }

                em.AddBuffer<PopWrapper>(province).AddRange(randomPop);
                randomPop.Dispose();
            }    
        }
    }
}
