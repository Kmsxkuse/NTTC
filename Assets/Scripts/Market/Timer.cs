using System;
using TMPro;
using UnityEngine;

namespace Market
{
    public class Timer : MonoBehaviour
    {
        public GameObject TickText, SpeedText;
        
        private static int _skipCounter, _tickCounter, _incrementCount = -1;
        private TextMeshProUGUI _tickText, _speedText;

        private void Start()
        {
            _tickText = TickText.GetComponent<TextMeshProUGUI>();
            _speedText = SpeedText.GetComponent<TextMeshProUGUI>();
            SetSpeedText();
        }

        private void Update()
        {
            if (_skipCounter++ < _incrementCount || _incrementCount == -1)
                return;
            _skipCounter = 0;
            _tickText.text = (_tickCounter++).ToString();
        }

        public void Increase()
        {
            if (_incrementCount == -1)
                _incrementCount = 64;
            else if (_incrementCount > 2)
                _incrementCount /= 2;
            SetSpeedText();
        }

        public void Decrease()
        {
            if (_incrementCount == -1)
                return;
            
            if (_incrementCount < 64)
                _incrementCount *= 2;
            else
                _incrementCount = -1;
            SetSpeedText();
        }

        private void SetSpeedText()
        {
            _speedText.text = _incrementCount == -1 ? "Paused" : _incrementCount.ToString();
        }
    }
}
