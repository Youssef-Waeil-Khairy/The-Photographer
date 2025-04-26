using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SAE_Dubai.Leonardo
{
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private Image fadePanel;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float defaultFadeDuration = 0.5f;

        private Coroutine currentFadeCoroutine;

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
                return;
            }

            if (fadeCanvasGroup != null) {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
            else if (fadePanel != null) {
                Color panelColor = fadePanel.color;
                panelColor.a = 0f;
                fadePanel.color = panelColor;
                fadePanel.raycastTarget = false;
            }
            else {
                Debug.LogError("ScreenFader: No fadePanel or fadeCanvasGroup assigned!", this);
            }
        }

        /// <summary>
        /// Starts fading the screen TO black (or the panel's color).
        /// </summary>
        /// <param name="duration">How long the fade should take. Uses default if <= 0.</param>
        /// <param name="onComplete">Optional action to execute when fade-out finishes.</param>
        public void StartFadeOut(float duration = 0, Action onComplete = null) {
            float fadeTime = duration > 0 ? duration : defaultFadeDuration;
            StartFade(1f, fadeTime, onComplete);
            // Faded TO alpha 1.
        }

        /// <summary>
        /// Starts fading the screen FROM black (or the panel's color).
        /// </summary>
        /// <param name="duration">How long the fade should take. Uses default if <= 0.</param>
        /// <param name="onComplete">Optional action to execute when fade-in finishes.</param>
        public void StartFadeIn(float duration = 0, Action onComplete = null) {
            float fadeTime = duration > 0 ? duration : defaultFadeDuration;
            StartFade(0f, fadeTime, onComplete);
            // Faded TO alpha 0.
        }


        private void StartFade(float targetAlpha, float duration, Action onComplete) {
            if (currentFadeCoroutine != null) {
                // ? Stop existing fade if any.
                StopCoroutine(currentFadeCoroutine);
            }

            currentFadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha, duration, onComplete));
        }

        private IEnumerator FadeCoroutine(float targetAlpha, float duration, Action onComplete) {
            // * Making sure panel is active and can block input during fade.
            if (fadeCanvasGroup != null) {
                fadeCanvasGroup.blocksRaycasts = true;
            }
            else if (fadePanel != null) {
                fadePanel.raycastTarget = true;
            }

            float startAlpha;
            float timer = 0f;

            // Get starting alpha.
            if (fadeCanvasGroup != null) {
                startAlpha = fadeCanvasGroup.alpha;
            }
            else if (fadePanel != null) {
                startAlpha = fadePanel.color.a;
            }
            else {
                yield break;
            }


            while (timer < duration) {
                timer += Time.unscaledDeltaTime; // ! Use unscaled time if Time.timeScale might be 0 ( for pausing issues)
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);

                // Update alpha.
                if (fadeCanvasGroup != null) {
                    fadeCanvasGroup.alpha = newAlpha;
                }
                else
                {
                    Color panelColor = fadePanel.color;
                    panelColor.a = newAlpha;
                    fadePanel.color = panelColor;
                }

                yield return null;
            }

            if (fadeCanvasGroup != null) {
                fadeCanvasGroup.alpha = targetAlpha;
                fadeCanvasGroup.blocksRaycasts = (targetAlpha > 0.1f);
            }
            else if (fadePanel != null) {
                Color panelColor = fadePanel.color;
                panelColor.a = targetAlpha;
                fadePanel.color = panelColor;
                fadePanel.raycastTarget = (targetAlpha > 0.1f);
            }

            currentFadeCoroutine = null;
            // Execute callback if provided.
            onComplete?.Invoke();
        }
    }
}