using Market;
using TMPro;
using UnityEngine;

namespace Conversion
{
    public class ScalarSystemBootstrap : MonoBehaviour
    {
        public GameObject TickText;
        public ComputeShader ScalarShader;

        public void Awake()
        {
            ScalarSystem.TickText = TickText.GetComponent<TextMeshProUGUI>();
            ScalarSystem.ScalarShader = ScalarShader;
        }
    }
}
