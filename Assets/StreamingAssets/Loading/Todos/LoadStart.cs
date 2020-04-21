using System.Collections;
using Clicking;
using UnityEngine;

namespace Conversion
{
    public class LoadStart : MonoBehaviour
    {
        public static bool Animate, BeginFade = false;
        private bool _animated, _faded;
        private CanvasGroup _currentCanvas;
        private RectTransform _titleImage, _buttons;
        [SerializeField] private GameObject titleImage, buttons, nationSelect;

        private void Awake()
        {
            _titleImage = titleImage.GetComponent<RectTransform>();
            _buttons = buttons.GetComponent<RectTransform>();
            _currentCanvas = GetComponent<CanvasGroup>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (Animate && !_animated)
            {
                _animated = true;
                StartCoroutine(RollIn());
            }

            if (!BeginFade || _faded)
                return;

            _faded = true;
            StartCoroutine(FadeOut());
            enabled = false;
        }

        private IEnumerator RollIn()
        {
            // Give GC some time to stop dying.
            yield return new WaitForSeconds(2);
            var startTime = Time.time;

            const float speed = 1000;

            const float journeyLength = 1000; // HARDCODED!
            const float offset = 100;
            var fractionJourney = 0f;

            while (fractionJourney < 1)
            {
                var distCovered = (Time.time - startTime) * speed;
                fractionJourney = distCovered / journeyLength;

                _titleImage.offsetMin = new Vector2(Mathf.Lerp(-journeyLength + offset, offset, fractionJourney), 0);
                _buttons.offsetMin = new Vector2(Mathf.Lerp(journeyLength - offset, -offset, fractionJourney), 0);
                yield return null;
            }
        }

        private IEnumerator FadeOut()
        {
            yield return null;

            var startTime = Time.time;

            const float fadeSpeed = 2.5f;

            while (_currentCanvas.alpha > 0.05f)
            {
                var distCovered = (Time.time - startTime) * fadeSpeed;

                _currentCanvas.alpha = Mathf.Lerp(1, 0, distCovered);
                yield return null;
            }

            // Resetting animations
            Animate = false;
            _animated = false;

            nationSelect.GetComponent<ClickNation>().enabled = true;

            gameObject.SetActive(false);
        }
    }
}