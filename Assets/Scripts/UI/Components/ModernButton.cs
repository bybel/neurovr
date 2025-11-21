using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Modern button with smooth hover and press animations
    /// Elegant visual feedback for better UX
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ModernButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Visual Settings - Modern Colors")]
        [SerializeField] private Color normalColor = new Color(0.25f, 0.45f, 0.85f, 0.9f); // Rich blue with transparency
        [SerializeField] private Color hoverColor = new Color(0.35f, 0.65f, 0.95f, 1f); // Bright cyan-blue
        [SerializeField] private Color pressedColor = new Color(0.15f, 0.35f, 0.75f, 1f); // Deeper blue
        [SerializeField] private Color textColor = new Color(0.95f, 0.95f, 0.98f); // Soft white text
        [SerializeField] private float animationDuration = 0.25f;
        
        [Header("Scale Animation")]
        [SerializeField] private bool scaleOnHover = true;
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.95f;
        
        private Button button;
        private Image buttonImage;
        private TextMeshProUGUI buttonText;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Color originalTextColor;
        
        private const float DEFAULT_SCALE = 1f;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
            
            originalScale = rectTransform.localScale;
            if (buttonText != null)
            {
                originalTextColor = buttonText.color;
                buttonText.color = textColor; // Apply modern text color
            }
            
            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
                // Add subtle glow effect
                Material mat = buttonImage.material;
                if (mat != null)
                {
                    mat.SetColor("_EmissionColor", normalColor * 0.3f);
                }
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            if (buttonImage != null)
                StartCoroutine(AnimateColor(buttonImage.color, hoverColor, animationDuration));
            
            if (scaleOnHover)
                StartCoroutine(AnimateScale(rectTransform.localScale, originalScale * hoverScale, animationDuration));
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            if (buttonImage != null)
                StartCoroutine(AnimateColor(buttonImage.color, normalColor, animationDuration));
            
            if (scaleOnHover)
                StartCoroutine(AnimateScale(rectTransform.localScale, originalScale, animationDuration));
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            if (buttonImage != null)
                buttonImage.color = pressedColor;
            
            if (scaleOnHover)
                StartCoroutine(AnimateScale(rectTransform.localScale, originalScale * pressScale, animationDuration * 0.5f));
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            if (buttonImage != null)
                StartCoroutine(AnimateColor(buttonImage.color, hoverColor, animationDuration));
            
            if (scaleOnHover)
                StartCoroutine(AnimateScale(rectTransform.localScale, originalScale * hoverScale, animationDuration));
        }
        
        private System.Collections.IEnumerator AnimateColor(Color from, Color to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (buttonImage != null)
                    buttonImage.color = Color.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            if (buttonImage != null)
                buttonImage.color = to;
        }
        
        private System.Collections.IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (rectTransform != null)
                    rectTransform.localScale = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            if (rectTransform != null)
                rectTransform.localScale = to;
        }
        
        private void OnDisable()
        {
            if (rectTransform != null)
                rectTransform.localScale = originalScale;
        }
    }
}

