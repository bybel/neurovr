using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Manages smooth UI animations and transitions
    /// Provides elegant fade, scale, and slide animations
    /// </summary>
    public class UIAnimationManager : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float transitionDuration = 0.3f;
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        private const float DEFAULT_DURATION = 0.3f;
        private const float FADE_ALPHA_HIDDEN = 0f;
        private const float FADE_ALPHA_VISIBLE = 1f;
        
        private Dictionary<GameObject, Coroutine> activeAnimations = new Dictionary<GameObject, Coroutine>();
        
        public void FadeIn(GameObject target, float duration = -1f)
        {
            if (target == null) return;
            duration = duration < 0 ? transitionDuration : duration;
            
            StopAnimation(target);
            activeAnimations[target] = StartCoroutine(FadeCoroutine(target, FADE_ALPHA_HIDDEN, FADE_ALPHA_VISIBLE, duration));
        }
        
        public void FadeOut(GameObject target, float duration = -1f)
        {
            if (target == null) return;
            duration = duration < 0 ? transitionDuration : duration;
            
            StopAnimation(target);
            activeAnimations[target] = StartCoroutine(FadeCoroutine(target, FADE_ALPHA_VISIBLE, FADE_ALPHA_HIDDEN, duration));
        }
        
        public void ScaleIn(GameObject target, float duration = -1f)
        {
            if (target == null) return;
            duration = duration < 0 ? transitionDuration : duration;
            
            StopAnimation(target);
            activeAnimations[target] = StartCoroutine(ScaleCoroutine(target, Vector3.zero, Vector3.one, duration));
        }
        
        public void ScaleOut(GameObject target, float duration = -1f)
        {
            if (target == null) return;
            duration = duration < 0 ? transitionDuration : duration;
            
            StopAnimation(target);
            activeAnimations[target] = StartCoroutine(ScaleCoroutine(target, Vector3.one, Vector3.zero, duration));
        }
        
        public void SlideIn(GameObject target, Vector3 fromOffset, float duration = -1f)
        {
            if (target == null) return;
            duration = duration < 0 ? transitionDuration : duration;
            
            RectTransform rt = target.GetComponent<RectTransform>();
            if (rt == null) return;
            
            Vector2 startPos = rt.anchoredPosition + new Vector2(fromOffset.x, fromOffset.y);
            Vector2 endPos = rt.anchoredPosition;
            
            StopAnimation(target);
            activeAnimations[target] = StartCoroutine(SlideCoroutine(rt, startPos, endPos, duration));
        }
        
        public void SlideOut(GameObject target, Vector3 toOffset, float duration = -1f)
        {
            if (target == null) return;
            duration = duration < 0 ? transitionDuration : duration;
            
            RectTransform rt = target.GetComponent<RectTransform>();
            if (rt == null) return;
            
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos = rt.anchoredPosition + new Vector2(toOffset.x, toOffset.y);
            
            StopAnimation(target);
            activeAnimations[target] = StartCoroutine(SlideCoroutine(rt, startPos, endPos, duration));
        }
        
        public void TransitionMenus(GameObject hideMenu, GameObject showMenu, float duration = -1f)
        {
            duration = duration < 0 ? transitionDuration : duration;
            
            if (hideMenu != null)
            {
                FadeOut(hideMenu, duration);
                StartCoroutine(DeactivateAfterDelay(hideMenu, duration));
            }
            
            if (showMenu != null)
            {
                showMenu.SetActive(true);
                FadeIn(showMenu, duration);
            }
        }
        
        private IEnumerator FadeCoroutine(GameObject target, float startAlpha, float endAlpha, float duration)
        {
            CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }
            
            canvasGroup.alpha = endAlpha;
            activeAnimations.Remove(target);
        }
        
        private IEnumerator ScaleCoroutine(GameObject target, Vector3 startScale, Vector3 endScale, float duration)
        {
            RectTransform rt = target.GetComponent<RectTransform>();
            if (rt == null) yield break;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);
                rt.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            
            rt.localScale = endScale;
            activeAnimations.Remove(target);
        }
        
        private IEnumerator SlideCoroutine(RectTransform rt, Vector2 startPos, Vector2 endPos, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = easeCurve.Evaluate(elapsed / duration);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
            
            rt.anchoredPosition = endPos;
            activeAnimations.Remove(rt.gameObject);
        }
        
        private IEnumerator DeactivateAfterDelay(GameObject target, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (target != null)
                target.SetActive(false);
        }
        
        private CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            CanvasGroup cg = target.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = target.AddComponent<CanvasGroup>();
            return cg;
        }
        
        private void StopAnimation(GameObject target)
        {
            if (activeAnimations.TryGetValue(target, out Coroutine coroutine))
            {
                StopCoroutine(coroutine);
                activeAnimations.Remove(target);
            }
        }
        
        public void StopAllAnimations()
        {
            foreach (var kvp in activeAnimations)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }
            activeAnimations.Clear();
        }
    }
}

