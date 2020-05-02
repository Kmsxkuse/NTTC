using UnityEngine;

namespace CamCode
{
    public class TerrainCam : MonoBehaviour
    {
        private void Start()
        {
            var cam = MainCam.TerrainCam = GetComponent<Camera>();
            var renderTexture = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0);
            cam.forceIntoRenderTexture = true;
            cam.targetTexture = MainCam.TerrainTexture = renderTexture;
        }
    }
}