using UnityEngine;

namespace CamCode
{
    public class LoadMap : MonoBehaviour
    {
        public GameObject MainMap;
    
        public static Texture2D Texture;
        
        private void Start()
        {
            var meshRenderer = MainMap.GetComponent<MeshRenderer>();
            var material = meshRenderer.material;
            material.mainTexture = Texture;
            
            // Duplicating map placing it to the right.
            var subMap = Instantiate(MainMap, MainMap.transform);
            subMap.transform.localPosition = Vector3.right;
            MainMap.transform.localScale = new Vector3(Texture.width / 100f, Texture.height / 100f, 1);

            MainCam.MainTransform = MainMap.transform;
            MainCam.SubTransform = subMap.transform;
            MainCam.Bounds = meshRenderer.bounds;
        }
    }
}
