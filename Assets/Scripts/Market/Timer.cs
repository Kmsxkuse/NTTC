using System;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Market
{
    public class Timer : MonoBehaviour
    {
        public GameObject TickText, SpeedText;
        
        private TextMeshProUGUI _speedText;

        private void Start()
        {
            MarketSystem.TickText = TickText.GetComponent<TextMeshProUGUI>();
            _speedText = SpeedText.GetComponent<TextMeshProUGUI>();
            SetSpeedText();
        }

        public void Increase()
        {
            ref var incrementCount = ref MarketSystem.GetIncrementCount();
            
            if (incrementCount == -1)
                incrementCount = 64;
            else if (incrementCount > 1)
                incrementCount /= 2;
            SetSpeedText();
        }

        public void Decrease()
        {
            ref var incrementCount = ref MarketSystem.GetIncrementCount();
            
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
            ref var incrementCount = ref MarketSystem.GetIncrementCount();
            _speedText.text = incrementCount == -1 ? "Paused" : incrementCount.ToString();
        }
    }
}
