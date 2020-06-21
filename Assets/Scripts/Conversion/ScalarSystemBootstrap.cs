using Market;
using TMPro;
using UnityEngine;

namespace Conversion
{
    public class ScalarSystemBootstrap : MonoBehaviour
    {
        public ComputeShader ScalarShader;
        public GameObject TickText;

        public void Awake()
        {
            ScalarSystem.TickText = TickText.GetComponent<TextMeshProUGUI>();
            ScalarSystem.ScalarShader = ScalarShader;
        }
    }
}