using UnityEngine;

namespace CamCode
{
    public class LoadMap : MonoBehaviour
    {
        public static Texture2D MapTexture;
        public GameObject MainMap;

        private void Start()
        {
            var meshRenderer = MainMap.GetComponent<MeshRenderer>();
            var material = meshRenderer.material;

            // Setting main map.
            material.mainTexture = MapTexture;

            // Duplicating map placing it to the right.
            var subMap = Instantiate(MainMap, MainMap.transform);
            subMap.transform.localPosition = Vector3.right;
            MainMap.transform.localScale = new Vector3(MapTexture.width / 100f, MapTexture.height / 100f, 1);

            MainCam.MainTransform = MainMap.transform;
            MainCam.SubTransform = subMap.transform;
            MainCam.Bounds = meshRenderer.bounds;
        }
    }
}