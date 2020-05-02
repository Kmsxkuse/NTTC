using UnityEngine;

namespace CamCode
{
    public class OceanCam : MonoBehaviour
    {
        private void Start()
        {
            // Literally a copy of Terrain Cam except with different Main Cam hooks.
            var cam = MainCam.OceanCam = GetComponent<Camera>();
            var renderTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0);
            cam.forceIntoRenderTexture = true;
            cam.targetTexture = MainCam.OceanTexture = renderTexture;
        }
    }
}