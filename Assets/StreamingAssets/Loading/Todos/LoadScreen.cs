using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Conversion
{
    public class LoadScreen : MonoBehaviour
    {
        private static GameObject _loadingCanvas;
        private static TextMeshProUGUI _loadingTopText;
        private static int _counter;
        private static bool _completed;
        private CanvasGroup _currentCanvas;

        private void Awake()
        {
            _loadingTopText = transform.Find("Top Text").GetComponent<TextMeshProUGUI>();
            _currentCanvas = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (!_completed)
                return;

            StartCoroutine(FadeOut());
            // Activating start menu animation
            LoadStart.Animate = true;
            enabled = false;
        }

        private IEnumerator FadeOut()
        {
            // Give GC some time to stop dying.
            yield return new WaitForSeconds(2);
            var startTime = Time.time;

            const float fadeSpeed = 2f;

            while (_currentCanvas.alpha > 0.05f)
            {
                var distCovered = (Time.time - startTime) * fadeSpeed;

                _currentCanvas.alpha = Mathf.Lerp(1, 0, distCovered);
                yield return null;
            }

            Destroy(gameObject);
        }

        public static void SetLoadingScreen(LoadingStages loadingStage)
        {
            switch (loadingStage)
            {
                case LoadingStages.Complete:
                    _completed = true;
                    break;
                default:
                    _loadingTopText.text = "Loading " + LoadMethods.AddSpacesToSentence(loadingStage.ToString())
                                                      + Environment.NewLine +
                                                      $"<size=70%>{++_counter} / {(int) LoadingStages.Complete}";
                    break;
            }
        }
    }
}