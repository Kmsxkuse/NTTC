using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Conversion;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Market
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class ScalarSystem : SystemBase
    {
        // Bootstrapped in from mono system ScalarSystemBootstrap.cs.
        // Because Unity says fuck you.
        public static TextMeshProUGUI TickText;
        public static ComputeShader ScalarShader;

        public static Texture2D IdMapTex, CentroidTex; // Set in LoadChain post pixel processing.

        private static int _incrementCount = -1;
        private RenderTexture _provinceIds, _provinceCentroids;

        private List<RenderTexture> _scalarGoods;
        private int _scalarKernel;

        private int _skipCounter, _totalCounter;

        protected override void OnStartRunning()
        {
            var highRes = RenderTextureFormat.ARGB32;
            // https://stackoverflow.com/questions/41566049/understanding-unitys-rgba-encoding-in-float-encodefloatrgba
            /* High res wont be needed. Really need 256^4 data?
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                highRes = RenderTextureFormat.ARGBFloat;
            else
                Debug.LogWarning("WARNING: High level float precision not supported on this computer. " +
                                 "Please either use or buy a computer from within the previous half decade before playing.");
                                 */

            var rtDesc = new RenderTextureDescriptor(IdMapTex.width, IdMapTex.height, highRes, 0);

            _scalarGoods = new List<RenderTexture>(LoadChain.GoodNum);
            for (var good = 0; good < LoadChain.GoodNum; good++)
            {
                var targetRendTex = new RenderTexture(rtDesc)
                {
                    enableRandomWrite = true
                };
                targetRendTex.Create();

                _scalarGoods.Add(targetRendTex);
            }

            _scalarKernel = ScalarShader.FindKernel("ScalarProcess");

            _provinceIds = new RenderTexture(rtDesc);
            _provinceIds.Create();
            Graphics.Blit(IdMapTex, _provinceIds);

            _provinceCentroids = new RenderTexture(CentroidTex.width, CentroidTex.height, 0, highRes);
            _provinceCentroids.Create();
            Graphics.Blit(CentroidTex, _provinceCentroids);
        }

        protected override void OnUpdate()
        {
            // System is manually updated at a rate handled by in Timer.cs.
            if (_skipCounter++ < _incrementCount || _incrementCount == -1)
                return;
            _skipCounter = 0;
            TickText.text = (_totalCounter++).ToString();

            Employment();

            ScalarShader.SetTexture(_scalarKernel, "Field", _scalarGoods[0]);
            ScalarShader.SetTexture(_scalarKernel, "ProvinceIds", _provinceIds);
            ScalarShader.SetTexture(_scalarKernel, "ProvCentroids", _provinceCentroids);
            ScalarShader.Dispatch(_scalarKernel, IdMapTex.width / 8, IdMapTex.height / 8, 1);

            /*
            var testTex = new Texture2D(IdMapTex.width, IdMapTex.height, TextureFormat.RGBA32, false);

            RenderTexture.active = _scalarGoods[0];
            
            testTex.ReadPixels(new Rect(0, 0, IdMapTex.width, IdMapTex.height),0,0);

            RenderTexture.active = null;
            
            File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "test.png"), testTex.EncodeToPNG());
            
            throw new Exception("TESSST");
            */
        }

        // Foreach not supported in burst compiled jobs.
        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        private void Employment()
        {
            // Matching pops with factories. Leftovers go to province RGO.

            var facEntityCount = GetEntityQuery(typeof(Factory), ComponentType.Exclude<RgoGood>())
                .CalculateEntityCount();

            var provCount = GetEntityQuery(typeof(Province), ComponentType.Exclude<UncolonizedProvince>())
                .CalculateEntityCount();

            Entities
                .WithName("Clear_Factory_Employments")
                .ForEach((ref DynamicBuffer<FactoryEmployment> factoryEmployments) =>
                {
                    // Clearing old employment lists.
                    factoryEmployments.Clear();
                }).ScheduleParallel();

            var incompleteFactoriesByState = new NativeMultiHashMap<Entity, Entity>(facEntityCount, Allocator.TempJob);
            var fbsParWriter = incompleteFactoriesByState.AsParallelWriter();

            var unemployedByState = new NativeMultiHashMap<Entity, FactoryEmployment>(provCount * 10, Allocator.TempJob);
            var ubsParWriter = unemployedByState.AsParallelWriter();

            var factoryData = GetComponentDataFromEntity<Factory>();
            var factEmployBuffer = GetBufferFromEntity<FactoryEmployment>();

            Entities
                .WithName("Province_Employment")
                .WithNativeDisableParallelForRestriction(factoryData)
                .WithNativeDisableParallelForRestriction(factEmployBuffer)
                .WithNone<UncolonizedProvince>()
                .ForEach((Entity entity, ref DynamicBuffer<Population> population, ref DynamicBuffer<FactoryWrapper> factoriesInProv,
                    in Province province) =>
                {
                    // Reinterpret because ref not allowed in local functions.
                    var factoryEntities = factoriesInProv.Reinterpret<Entity>();
                    var popCopy = population.Reinterpret<Population>();

                    ref var state = ref province.ProvToState.Value.Lookup[province.Index];

                    var popIndex = 0;
                    var currentAvailableToBeEmployed = population[popIndex].Quantity;

                    for (var factoryIndex = 0; factoryIndex < factoryEntities.Length; factoryIndex++)
                    {
                        var targetFactory = factoryData[factoryEntities[factoryIndex]];
                        var employBuffer = factEmployBuffer[factoryEntities[factoryIndex]];

                        var remainingCapacity = targetFactory.MaximumEmployment;

                        while (true)
                        {
                            remainingCapacity -= currentAvailableToBeEmployed;

                            if (remainingCapacity > 0)
                            {
                                ReflectEmploymentOnPops(currentAvailableToBeEmployed);

                                if (++popIndex == population.Length)
                                {
                                    SetTotalEmployed();
                                    
                                    // Sending factories to state based employment clearing house.
                                    for (var remainingFacIndex = factoryIndex; remainingFacIndex < factoryEntities.Length; 
                                        remainingFacIndex++)
                                    {
                                        var remainingFactEntity = factoryEntities[remainingFacIndex];
                                        var remainingFactData = factoryData[remainingFactEntity];

                                        if (remainingFactData.MaximumEmployment - remainingFactData.TotalEmployed == 0)
                                            continue;
                                
                                        fbsParWriter.Add(state,remainingFactEntity);
                                    }
                                    return;
                                }

                                currentAvailableToBeEmployed = population[popIndex].Quantity;
                                continue;
                            }

                            if (remainingCapacity == 0)
                            {
                                // Wow, perfect fit!
                                ReflectEmploymentOnPops(currentAvailableToBeEmployed);
                                currentAvailableToBeEmployed = 0;
                                break;
                            }

                            // if (remainingCapacity < 0)
                            ReflectEmploymentOnPops(remainingCapacity + currentAvailableToBeEmployed);

                            currentAvailableToBeEmployed = -remainingCapacity;
                            remainingCapacity = 0;
                            break;
                        }

                        SetTotalEmployed();

                        void SetTotalEmployed()
                        {
                            targetFactory.TotalEmployed = targetFactory.MaximumEmployment - remainingCapacity;
                            factoryData[factoryEntities[factoryIndex]] = targetFactory;
                        }

                        void ReflectEmploymentOnPops(int numEmployed)
                        {
                            var targetPop = popCopy[popIndex];
                            targetPop.Employed = numEmployed;
                            popCopy[popIndex] = targetPop;
                            
                            employBuffer.Add(new FactoryEmployment(entity, popIndex, numEmployed));
                        }
                    }
                    
                    // Remaining pop is sent to state employment clearing.

                    for (var remainingPopIndex = popIndex; remainingPopIndex < population.Length; remainingPopIndex++)
                    {
                        var targetPop = population[remainingPopIndex];
                        var unemployed = targetPop.Quantity - targetPop.Employed;
                        if (unemployed <= 0)
                            continue;
                        
                        ubsParWriter.Add(state, new FactoryEmployment(entity, remainingPopIndex,unemployed));
                    }
                }).ScheduleParallel();

            var provPopulation = GetBufferFromEntity<Population>();
            
            Entities
                .WithName("State_Employment")
                .WithReadOnly(incompleteFactoriesByState)
                .WithReadOnly(unemployedByState)
                .WithNativeDisableParallelForRestriction(factoryData)
                .WithNativeDisableParallelForRestriction(factEmployBuffer)
                .WithNativeDisableParallelForRestriction(provPopulation)
                .WithNone<PartialOwnership>()
                .WithAll<State>()
                .ForEach((Entity entity) =>
                {
                    if (!incompleteFactoriesByState.TryGetFirstValue(entity, out var factory, out var iterator)
                        || !unemployedByState.TryGetFirstValue(entity, out var popEmploy, out var popIterator))
                        return;
                    
                    var targetFactory = factoryData[factory];
                    var remainingCapacity = targetFactory.MaximumEmployment - targetFactory.TotalEmployed;

                    while (true)
                    {
                        var employed = math.min(popEmploy.Quantity, remainingCapacity);

                        popEmploy.Quantity -= employed;
                        remainingCapacity -= employed;
                        
                        factEmployBuffer[factory].Add(
                            new FactoryEmployment(popEmploy.Province, popEmploy.PopIndex, employed));

                        // Fucking reflection... Ref var doesn't work for any of this.
                        var popBuffer = provPopulation[popEmploy.Province];
                        var oldPop = popBuffer[popEmploy.PopIndex];
                        oldPop.Employed += employed;
                        popBuffer[popEmploy.PopIndex] = oldPop;
                        
                        
                        targetFactory.TotalEmployed += employed;
                        factoryData[factory] = targetFactory;

                        if (remainingCapacity == 0)
                        {
                            // Get new factory.
                            if (!incompleteFactoriesByState.TryGetNextValue(out factory, ref iterator))
                                return;
                            
                            targetFactory = factoryData[factory];
                            remainingCapacity = targetFactory.MaximumEmployment - targetFactory.TotalEmployed;
                        }

                        if (popEmploy.Quantity == 0 && !unemployedByState.TryGetNextValue(out popEmploy, ref popIterator))
                            // No more pops left.
                            return;
                    }
                    
                }).ScheduleParallel();

            incompleteFactoriesByState.Dispose(Dependency);
            unemployedByState.Dispose(Dependency);
        }

        public static ref int GetIncrementCount()
        {
            // Used in Timer to bootstrap between UI buttons and actual update frequency.
            return ref _incrementCount;
        }
    }
}