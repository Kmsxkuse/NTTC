using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Entities;
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
        
        public static Texture2D MapTex; // Set in LoadChain post pixel processing.
        
        private static int _incrementCount = -1;

        private int _skipCounter, _totalCounter;

        private List<RenderTexture> _scalarGoods;
        private int _scalarKernel;
        private RenderTexture _provinceIds;

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
            
            var rtDesc = new RenderTextureDescriptor(MapTex.width, MapTex.height, highRes, 0);   
            
            _scalarGoods = new List<RenderTexture>(Conversion.LoadChain.GoodNum);
            for (var good = 0; good < Conversion.LoadChain.GoodNum; good++)
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
            Graphics.Blit(MapTex, _provinceIds);
        }

        protected override void OnUpdate()
        {
            // System is manually updated at a rate handled by in Timer.cs.
            if (_skipCounter++ < _incrementCount || _incrementCount == -1)
                return;
            _skipCounter = 0;
            TickText.text = (_totalCounter++).ToString();
            
            ScalarShader.SetTexture(_scalarKernel, "Field", _scalarGoods[0]);
            ScalarShader.SetTexture(_scalarKernel, "ProvId", _provinceIds);
            ScalarShader.Dispatch(_scalarKernel, MapTex.width / 8, MapTex.height / 8, 1);

            /*
            var testTex = new Texture2D(MapTex.width, MapTex.height, TextureFormat.RGBA32, false);

            RenderTexture.active = _scalarGoods[0];
            
            testTex.ReadPixels(new Rect(0, 0, MapTex.width, MapTex.height),0,0);

            RenderTexture.active = null;
            
            File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "test.png"), testTex.EncodeToPNG());
            
            throw new Exception("TESSST");
            */
        }
        
        public static ref int GetIncrementCount()
        {
            // Used in Timer to bootstrap between UI buttons and actual update frequency.
            return ref _incrementCount;
        }
    }
}
