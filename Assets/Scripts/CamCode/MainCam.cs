using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CamCode
{
    public class MainCam : MonoBehaviour
    {
        public Material MapMaterial, BlendMaterial;

        public static Transform MainTransform, SubTransform;
        public static Bounds Bounds;
        public static RenderTexture TerrainTexture, OceanTexture;
        public static Camera TerrainCam, OceanCam;

        private Camera _camera;
        private ComputeBuffer _provLookup;
        private EntityManager _em;
        private float _orthographicSize;
        
        private static readonly int ProvColorBuffer = Shader.PropertyToID("floatBuffer");
        private static readonly int CountryBorderToggle = Shader.PropertyToID("countryBorderToggle");
        private static readonly int SecondTex = Shader.PropertyToID("_SecondTex");
        private static readonly int SkipColor = Shader.PropertyToID("_SkipColor");
        private static readonly int SkipDirection = Shader.PropertyToID("_SkipDirection");
        private static readonly int BlendStrength = Shader.PropertyToID("_BlendStrength");

        private Vector2 _movement;
        private Vector3 _delta, _dragOrigin;
        private bool _handOver, _orthoChanged, _dragLock;
        private float _scrollDirection;

        private void Start()
        {
            _camera = Camera.main;
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;

            // Default political map mode.
            using (var provinces =
                _em.CreateEntityQuery(typeof(Province)).ToEntityArray(Allocator.TempJob))
            {
                _provLookup = new ComputeBuffer(provinces.Length, 16, ComputeBufferType.Structured);

                var provTable = new float4[provinces.Length];
                foreach (var provData in provinces.Select(provinceEntity => _em.GetComponentData<Province>(provinceEntity)))
                {
                    var color = _em.GetComponentData<Country>(provData.Owner).Color;
                    provTable[provData.Index] = new float4(color.r, color.g, color.b, color.a);
                }
                
                _provLookup.SetData(provTable);
            }
            // Ocean tiles special colors. Defined in Countries Load.
            BlendMaterial.SetVector(SkipColor, (Color) (new Color32(0, 191, 255, 255)));
        }

        private void Update()
        {
            _orthographicSize = _camera.orthographicSize;
            
            var oldPosition = MainTransform.position;
            
            // Looper code. Horizontal teleportation.
            if (oldPosition.x < _orthographicSize * _camera.aspect - Bounds.max.x * 0.75)
            {
                // RIGHT
                SubTransform.localPosition = Vector2.right;
                _handOver = true;
            }
            else if (oldPosition.x > Bounds.max.x * 0.75 - _orthographicSize * _camera.aspect)
            {
                // LEFT
                SubTransform.localPosition = Vector2.left;
                _handOver = true;
            }

            if (_handOver && math.abs(oldPosition.x) > Bounds.max.x * 2)
            {
                // Switch main and sub maps.
                var oldMain = MainTransform;
                MainTransform = SubTransform;
                SubTransform = oldMain;
                
                MainTransform.SetParent(null);
                SubTransform.SetParent(MainTransform);
                
                _handOver = false;
            
                oldPosition = MainTransform.position;
            }
            
            // Movement code.
            if (_dragLock)
                _delta = _dragOrigin - _camera.ScreenToWorldPoint(Input.mousePosition);
            
            if (_orthoChanged)
            {
                _movement /= _orthographicSize;
                _camera.orthographicSize = TerrainCam.orthographicSize = OceanCam.orthographicSize =
                    _orthographicSize = math.clamp(_scrollDirection + _orthographicSize, 0.2f, 5f);
                _movement *= _orthographicSize;
                
                _orthoChanged = false;
            }
            
            MainTransform.position = new Vector3
            (
                // Subtract as the map is moving, not the camera.
                oldPosition.x - (_dragLock ? _delta.x : _movement.x),
                math.clamp(oldPosition.y - (_dragLock ? _delta.y : _movement.y),
                    _orthographicSize - Bounds.max.y, Bounds.max.y - _orthographicSize)
            );
            
            if (_dragLock)
                _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
        }

        public void Move(InputAction.CallbackContext callbackContext)
        {
            var speed = 0.15f * _orthographicSize; // Inversing for correct direction movement.
            if (callbackContext.performed)
                _movement = callbackContext.ReadValue<Vector2>() * speed;
            else if (callbackContext.canceled)
                _movement = Vector2.zero;
        }

        public void Zoom(InputAction.CallbackContext callbackContext)
        {
            var speed = -0.25f * _orthographicSize;

            if (callbackContext.performed)
            {
                _scrollDirection = math.clamp(callbackContext.ReadValue<Vector2>().y, -1, 1) * speed;
                _orthoChanged = true;
            }
            else if (callbackContext.canceled)
                _scrollDirection = 0;
        }

        public void Drag(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.performed)
            {
                _dragLock = true;
                _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
            }
            else if (callbackContext.canceled)
                _dragLock = false;
        }
        
        private void OnDestroy()
        {
            _provLookup.Dispose();
        }

        // Border generation
        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            MapMaterial.SetBuffer(ProvColorBuffer, _provLookup);
            // Passing which border to render.
            MapMaterial.SetInt(CountryBorderToggle, _orthographicSize > 1.5f ? 0 : 1);

            var terrainTemp = RenderTexture.GetTemporary(src.descriptor);
            var oceanTemp = RenderTexture.GetTemporary(src.descriptor);
            
            // Province coloring.
            Graphics.Blit(src, terrainTemp, MapMaterial);
            
            // Overlay combination. Fuck Unity.
            // Terrain
            BlendMaterial.SetTexture(SecondTex, TerrainTexture);
            BlendMaterial.SetFloat(SkipDirection, 1);
            BlendMaterial.SetFloat(BlendStrength, 0.5f);
            Graphics.Blit(terrainTemp, oceanTemp, BlendMaterial);
            // Ocean
            BlendMaterial.SetTexture(SecondTex, OceanTexture);
            BlendMaterial.SetFloat(SkipDirection, 0);
            BlendMaterial.SetFloat(BlendStrength, 0.75f);
            Graphics.Blit(oceanTemp, dest, BlendMaterial);
            
            terrainTemp.Release();
            oceanTemp.Release();
        }
    }
}
