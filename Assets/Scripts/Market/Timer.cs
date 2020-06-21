using TMPro;
using UnityEngine;

namespace Market
{
    public class Timer : MonoBehaviour
    {
        private TextMeshProUGUI _speedText;
        public GameObject SpeedText;

        private void Start()
        {
            _speedText = SpeedText.GetComponent<TextMeshProUGUI>();
            SetSpeedText();
        }

        public void Increase()
        {
            ref var incrementCount = ref ScalarSystem.GetIncrementCount();

            if (incrementCount == -1)
                incrementCount = 64;
            else if (incrementCount > 1)
                incrementCount /= 2;
            SetSpeedText();
        }

        public void Decrease()
        {
            ref var incrementCount = ref ScalarSystem.GetIncrementCount();

            if (incrementCount == -1)
                return;

            if (incrementCount < 64)
                incrementCount *= 2;
            else
                incrementCount = -1;
            SetSpeedText();
        }

        private void SetSpeedText()
        {
            ref var incrementCount = ref ScalarSystem.GetIncrementCount();
            _speedText.text = incrementCount == -1 ? "Paused" : incrementCount.ToString();
        }
    }
}