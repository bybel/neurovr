using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Enhanced button with accessibility features
    /// Larger hit area, visual feedback, error tolerance
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AccessibleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float DEFAULT_HIT_AREA_MULTIPLIER = 1.5f;
        private const float ERROR_TOLERANCE_MULTIPLIER = 1.2f;
        
        [Header("Accessibility")]
        [SerializeField] private float hitAreaMultiplier = DEFAULT_HIT_AREA_MULTIPLIER;
        [SerializeField] private bool enlargedHitArea = true;
        [SerializeField] private Color highlightColor = Color.cyan;
        // highlightDuration reserved for future animation implementation
        #pragma warning disable CS0414
        [SerializeField] private float highlightDuration = 0.3f;
        #pragma warning restore CS0414
        
        private Button button;
        private Image buttonImage;
        private TextMeshProUGUI buttonText;
        private Color originalColor;
        private RectTransform rectTransform;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
            
            if (buttonImage != null)
                originalColor = buttonImage.color;
            
            SetupHitArea();
        }
        
        private void SetupHitArea()
        {
            if (!enlargedHitArea || rectTransform == null) return;
            
            // Create invisible larger hit area
            GameObject hitArea = new GameObject("HitArea");
            hitArea.transform.SetParent(transform, false);
            hitArea.transform.localPosition = Vector3.zero;
            hitArea.transform.localScale = Vector3.one;
            
            RectTransform hitRect = hitArea.AddComponent<RectTransform>();
            hitRect.anchorMin = Vector2.zero;
            hitRect.anchorMax = Vector2.one;
            hitRect.sizeDelta = rectTransform.sizeDelta * hitAreaMultiplier;
            hitRect.anchoredPosition = Vector3.zero;
            
            Image hitImage = hitArea.AddComponent<Image>();
            hitImage.color = new Color(0, 0, 0, 0); // Transparent
            
            Button hitButton = hitArea.AddComponent<Button>();
            hitButton.onClick.AddListener(() => button.onClick.Invoke());
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (buttonImage != null)
            {
                buttonImage.color = highlightColor;
            }
            
            if (buttonText != null)
            {
                buttonText.fontStyle = FontStyles.Bold;
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            if (buttonImage != null)
            {
                buttonImage.color = originalColor;
            }
            
            if (buttonText != null)
            {
                buttonText.fontStyle = FontStyles.Normal;
            }
        }
        
        public void SetErrorTolerance(bool enabled)
        {
            // Forgiving input detection - larger hit area already helps
            if (enabled && rectTransform != null)
            {
                rectTransform.sizeDelta *= ERROR_TOLERANCE_MULTIPLIER;
            }
        }
    }
}

