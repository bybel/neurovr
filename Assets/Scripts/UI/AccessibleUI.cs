using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Accessibility manager for UI enhancements
    /// Provides large text, high contrast, voice prompts, simplified controls
    /// </summary>
    public class AccessibleUI : MonoBehaviour
    {
        [Header("Text Size Settings")]
        [SerializeField] private FontSize fontSize = FontSize.Medium;
        [SerializeField] private float smallFontSize = 24f;
        [SerializeField] private float mediumFontSize = 32f;
        [SerializeField] private float largeFontSize = 48f;
        
        [Header("High Contrast")]
        [SerializeField] private bool highContrastEnabled = false;
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color highContrastTextColor = Color.yellow;
        [SerializeField] private Color normalBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color highContrastBackgroundColor = Color.black;
        
        [Header("Button Settings")]
        [SerializeField] private float minButtonSize = 100f;
        // Reserved for future layout system implementation
        #pragma warning disable CS0414
        [SerializeField] private float buttonPadding = 20f;
        #pragma warning restore CS0414
        
        [Header("Voice Prompts")]
        [SerializeField] private bool voicePromptsEnabled = true;
        // Reserved for future voice prompt timing implementation
        #pragma warning disable CS0414
        [SerializeField] private float voicePromptDelay = 2f;
        #pragma warning restore CS0414
        
        [Header("Timeout Settings")]
        [SerializeField] private float inactivityTimeout = 30f;
        [SerializeField] private GameObject helpPromptPrefab;
        
        private const float TIMEOUT_CHECK_INTERVAL = 5f;
        private const float HELP_PROMPT_DURATION = 5f;
        
        private List<TextMeshProUGUI> allTextElements;
        private List<Button> allButtons;
        private float lastInteractionTime;
        private Coroutine timeoutCoroutine;
        
        public FontSize CurrentFontSize => fontSize;
        public bool IsHighContrast => highContrastEnabled;
        
        private void Awake()
        {
            CollectUIElements();
            ApplyAccessibilitySettings();
        }
        
        private void Start()
        {
            if (inactivityTimeout > 0f)
                StartTimeoutMonitoring();
        }
        
        private void CollectUIElements()
        {
            allTextElements = new List<TextMeshProUGUI>(FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None));
            allButtons = new List<Button>(FindObjectsByType<Button>(FindObjectsSortMode.None));
        }
        
        public void SetFontSize(FontSize size)
        {
            fontSize = size;
            ApplyFontSizes();
        }
        
        public void SetHighContrast(bool enabled)
        {
            highContrastEnabled = enabled;
            ApplyColors();
        }
        
        public void SetVoicePrompts(bool enabled)
        {
            voicePromptsEnabled = enabled;
        }
        
        public void SpeakText(string text)
        {
            if (!voicePromptsEnabled) return;
            
            // Unity doesn't have built-in TTS, use platform-specific or third-party solution
            // For now, log the text (can be replaced with TTS plugin)
            Debug.Log($"[Voice Prompt] {text}");
            
            // Example: Could use Android TTS or iOS AVSpeechSynthesizer
            // For VR, consider using Meta's voice SDK or similar
        }
        
        public void ShowHelpPrompt(string message)
        {
            if (helpPromptPrefab != null)
            {
                GameObject prompt = Instantiate(helpPromptPrefab);
                var text = prompt.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = message;
                
                SpeakText(message);
                
                Destroy(prompt, HELP_PROMPT_DURATION);
            }
            else
            {
                SpeakText(message);
            }
        }
        
        private void ApplyAccessibilitySettings()
        {
            ApplyFontSizes();
            ApplyButtonSizes();
            ApplyColors();
            AddVisualFeedback();
        }
        
        private void ApplyFontSizes()
        {
            float targetSize = fontSize switch
            {
                FontSize.Small => smallFontSize,
                FontSize.Medium => mediumFontSize,
                FontSize.Large => largeFontSize,
                _ => mediumFontSize
            };
            
            foreach (var text in allTextElements)
            {
                if (text != null)
                    text.fontSize = targetSize;
            }
        }
        
        private void ApplyButtonSizes()
        {
            foreach (var button in allButtons)
            {
                if (button == null) continue;
                
                RectTransform rect = button.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 size = rect.sizeDelta;
                    if (size.x < minButtonSize)
                        size.x = minButtonSize;
                    if (size.y < minButtonSize)
                        size.y = minButtonSize;
                    rect.sizeDelta = size;
                }
                
                // Add padding
                var layoutElement = button.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = button.gameObject.AddComponent<LayoutElement>();
                
                layoutElement.minWidth = minButtonSize;
                layoutElement.minHeight = minButtonSize;
            }
        }
        
        private void ApplyColors()
        {
            Color textColor = highContrastEnabled ? highContrastTextColor : normalTextColor;
            Color bgColor = highContrastEnabled ? highContrastBackgroundColor : normalBackgroundColor;
            
            foreach (var text in allTextElements)
            {
                if (text != null)
                    text.color = textColor;
            }
            
            foreach (var button in allButtons)
            {
                if (button == null) continue;
                
                var colors = button.colors;
                colors.normalColor = highContrastEnabled ? Color.white : colors.normalColor;
                colors.highlightedColor = highContrastEnabled ? Color.yellow : colors.highlightedColor;
                button.colors = colors;
            }
        }
        
        private void AddVisualFeedback()
        {
            foreach (var button in allButtons)
            {
                if (button == null) continue;
                
                // Ensure button has visual feedback
                var colors = button.colors;
                colors.highlightedColor = Color.cyan;
                colors.pressedColor = Color.blue;
                colors.selectedColor = Color.green;
                button.colors = colors;
                
                // Add hover effect listener
                button.onClick.AddListener(() => OnButtonClicked(button));
            }
        }
        
        private void OnButtonClicked(Button button)
        {
            lastInteractionTime = Time.time;
            
            // Visual feedback
            if (button != null)
            {
                StartCoroutine(ButtonClickFeedback(button));
            }
            
            // Voice feedback
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null && voicePromptsEnabled)
                SpeakText(text.text);
        }
        
        private IEnumerator ButtonClickFeedback(Button button)
        {
            var originalColor = button.colors.normalColor;
            var colors = button.colors;
            colors.normalColor = Color.green;
            button.colors = colors;
            
            yield return new WaitForSeconds(0.2f);
            
            colors.normalColor = originalColor;
            button.colors = colors;
        }
        
        private void StartTimeoutMonitoring()
        {
            lastInteractionTime = Time.time;
            timeoutCoroutine = StartCoroutine(MonitorInactivity());
        }
        
        private IEnumerator MonitorInactivity()
        {
            while (true)
            {
                yield return new WaitForSeconds(TIMEOUT_CHECK_INTERVAL);
                
                if (Time.time - lastInteractionTime >= inactivityTimeout)
                {
                    ShowHelpPrompt("No input detected. Would you like help?");
                    lastInteractionTime = Time.time; // Reset to prevent spam
                }
            }
        }
        
        public void RefreshUI()
        {
            CollectUIElements();
            ApplyAccessibilitySettings();
        }
        
        public void RegisterButton(Button button)
        {
            if (button != null && !allButtons.Contains(button))
            {
                allButtons.Add(button);
                ApplyButtonSizes();
                AddVisualFeedback();
            }
        }
        
        public void RegisterText(TextMeshProUGUI text)
        {
            if (text != null && !allTextElements.Contains(text))
            {
                allTextElements.Add(text);
                ApplyFontSizes();
                ApplyColors();
            }
        }
    }
    
    public enum FontSize
    {
        Small,   // 24pt
        Medium,  // 32pt
        Large    // 48pt
    }
}

