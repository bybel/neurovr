using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Modern, smooth UI animation system
    /// Provides elegant transitions: fade, slide, scale, pulse
    /// </summary>
    public static class UIAnimator
    {
        public enum SlideDirection { Left, Right, Up, Down }
        public enum EasingType { Linear, EaseInOut, EaseOut, EaseIn }

        // Fade Animations
        public static IEnumerator FadeIn(CanvasGroup canvasGroup, float duration = -1f, EasingType easing = EasingType.EaseOut)
        {
            if (duration < 0) duration = 0.3f; // Hardcoded default
            yield return Fade(canvasGroup, 0f, 1f, duration, easing);
        }

        public static IEnumerator FadeOut(CanvasGroup canvasGroup, float duration = -1f, EasingType easing = EasingType.EaseIn)
        {
            if (duration < 0) duration = 0.3f; // Hardcoded default
            yield return Fade(canvasGroup, 1f, 0f, duration, easing);
        }

        private static IEnumerator Fade(CanvasGroup canvasGroup, float from, float to, float duration, EasingType easing)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, ApplyEasing(t, easing));
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        // Slide Animations
        public static IEnumerator SlideIn(RectTransform rectTransform, SlideDirection direction, float distance = 1000f, float duration = -1f, EasingType easing = EasingType.EaseOut)
        {
            if (duration < 0) duration = NeuroReachVR.Utils.NeuroVRConstants.UI_SLIDE_DURATION;
            Vector2 startPos = GetOffscreenPosition(rectTransform, direction, distance);
            Vector2 endPos = rectTransform.anchoredPosition;
            yield return Slide(rectTransform, startPos, endPos, duration, easing);
        }

        public static IEnumerator SlideOut(RectTransform rectTransform, SlideDirection direction, float distance = 1000f, float duration = -1f, EasingType easing = EasingType.EaseIn)
        {
            if (duration < 0) duration = NeuroReachVR.Utils.NeuroVRConstants.UI_SLIDE_DURATION;
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = GetOffscreenPosition(rectTransform, direction, distance);
            yield return Slide(rectTransform, startPos, endPos, duration, easing);
        }

        private static IEnumerator Slide(RectTransform rectTransform, Vector2 from, Vector2 to, float duration, EasingType easing)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rectTransform.anchoredPosition = Vector2.Lerp(from, to, ApplyEasing(t, easing));
                yield return null;
            }
            rectTransform.anchoredPosition = to;
        }

        private static Vector2 GetOffscreenPosition(RectTransform rectTransform, SlideDirection direction, float distance)
        {
            Vector2 currentPos = rectTransform.anchoredPosition;
            return direction switch
            {
                SlideDirection.Left => currentPos + Vector2.left * distance,
                SlideDirection.Right => currentPos + Vector2.right * distance,
                SlideDirection.Up => currentPos + Vector2.up * distance,
                SlideDirection.Down => currentPos + Vector2.down * distance,
                _ => currentPos
            };
        }

        // Scale Animations
        public static IEnumerator ScaleIn(Transform transform, float duration = -1f, EasingType easing = EasingType.EaseOut)
        {
            if (duration < 0) duration = NeuroReachVR.Utils.NeuroVRConstants.UI_SCALE_DURATION;
            yield return Scale(transform, Vector3.zero, Vector3.one, duration, easing);
        }

        public static IEnumerator ScaleOut(Transform transform, float duration = -1f, EasingType easing = EasingType.EaseIn)
        {
            if (duration < 0) duration = NeuroReachVR.Utils.NeuroVRConstants.UI_SCALE_DURATION;
            yield return Scale(transform, Vector3.one, Vector3.zero, duration, easing);
        }

        public static IEnumerator Pulse(Transform transform, float scaleMultiplier = 1.1f, float duration = 0.3f)
        {
            yield return Scale(transform, Vector3.one, Vector3.one * scaleMultiplier, duration * 0.5f, EasingType.EaseOut);
            yield return Scale(transform, Vector3.one * scaleMultiplier, Vector3.one, duration * 0.5f, EasingType.EaseIn);
        }

        private static IEnumerator Scale(Transform transform, Vector3 from, Vector3 to, float duration, EasingType easing)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(from, to, ApplyEasing(t, easing));
                yield return null;
            }
            transform.localScale = to;
        }

        // Combined Animations
        public static IEnumerator FadeAndSlideIn(GameObject target, SlideDirection direction, float slideDistance = 1000f)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();

            RectTransform rectTransform = target.GetComponent<RectTransform>();

            canvasGroup.alpha = 0f;
            target.SetActive(true);

            // Run both animations in parallel
            Coroutine fade = null, slide = null;
            MonoBehaviour runner = GetCoroutineRunner();

            fade = runner.StartCoroutine(FadeIn(canvasGroup));
            if (rectTransform != null)
                slide = runner.StartCoroutine(SlideIn(rectTransform, direction, slideDistance));

            yield return fade;
            if (slide != null) yield return slide;
        }

        public static IEnumerator FadeAndSlideOut(GameObject target, SlideDirection direction, float slideDistance = 1000f, bool deactivate = true)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();

            RectTransform rectTransform = target.GetComponent<RectTransform>();

            MonoBehaviour runner = GetCoroutineRunner();
            Coroutine fade = runner.StartCoroutine(FadeOut(canvasGroup));
            Coroutine slide = null;

            if (rectTransform != null)
                slide = runner.StartCoroutine(SlideOut(rectTransform, direction, slideDistance));

            yield return fade;
            if (slide != null) yield return slide;

            if (deactivate) target.SetActive(false);
        }

        public static IEnumerator FadeAndScaleIn(GameObject target)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            target.transform.localScale = Vector3.zero;
            target.SetActive(true);

            MonoBehaviour runner = GetCoroutineRunner();
            Coroutine fade = runner.StartCoroutine(FadeIn(canvasGroup));
            Coroutine scale = runner.StartCoroutine(ScaleIn(target.transform));

            yield return fade;
            yield return scale;
        }

        // Easing Functions
        private static float ApplyEasing(float t, EasingType easing)
        {
            return easing switch
            {
                EasingType.Linear => t,
                EasingType.EaseInOut => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f,
                EasingType.EaseOut => 1f - Mathf.Pow(1f - t, 2f),
                EasingType.EaseIn => t * t,
                _ => t
            };
        }

        // Utility: Get or create coroutine runner
        private static MonoBehaviour coroutineRunner;
        private static MonoBehaviour GetCoroutineRunner()
        {
            if (coroutineRunner == null)
            {
                GameObject runnerObj = new GameObject("[UIAnimator Runner]");
                coroutineRunner = runnerObj.AddComponent<UIAnimatorRunner>();
                Object.DontDestroyOnLoad(runnerObj);
            }
            return coroutineRunner;
        }

        private class UIAnimatorRunner : MonoBehaviour { }
    }
}
