using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Manages UI theme and styling for consistent, modern appearance
    /// Centralized color scheme and visual design
    /// </summary>
    public class UIThemeManager : MonoBehaviour
    {
        [Header("Color Scheme - Modern & Elegant")]
        [SerializeField] private Color primaryColor = new Color(0.25f, 0.45f, 0.85f); // Rich blue
        [SerializeField] private Color secondaryColor = new Color(0.35f, 0.65f, 0.95f); // Bright cyan-blue
        [SerializeField] private Color accentColor = new Color(0.15f, 0.85f, 0.65f); // Vibrant teal
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.92f); // Deep dark blue-gray
        [SerializeField] private Color textColor = new Color(0.95f, 0.95f, 0.98f); // Soft white
        [SerializeField] private Color errorColor = new Color(0.95f, 0.25f, 0.35f); // Soft red
        [SerializeField] private Color successColor = new Color(0.2f, 0.85f, 0.5f); // Fresh green
        [SerializeField] private Color hoverGlowColor = new Color(0.4f, 0.7f, 1f, 0.6f); // Glowing blue
        
        [Header("Typography")]
        [SerializeField] private TMP_FontAsset primaryFont;
        [SerializeField] private float baseFontSize = 32f;
        
        [Header("Spacing")]
        // Reserved for future layout system implementation
        #pragma warning disable CS0414
        [SerializeField] private float buttonSpacing = 20f;
        [SerializeField] private float menuPadding = 40f;
        #pragma warning restore CS0414
        
        public static UIThemeManager Instance { get; private set; }
        
        public Color PrimaryColor => primaryColor;
        public Color SecondaryColor => secondaryColor;
        public Color AccentColor => accentColor;
        public Color BackgroundColor => backgroundColor;
        public Color TextColor => textColor;
        public Color ErrorColor => errorColor;
        public Color SuccessColor => successColor;
        public Color HoverGlowColor => hoverGlowColor;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        public void ApplyThemeToButton(Button button)
        {
            if (button == null) return;
            
            ColorBlock colors = button.colors;
            colors.normalColor = primaryColor;
            colors.highlightedColor = secondaryColor;
            colors.pressedColor = accentColor;
            colors.selectedColor = secondaryColor;
            button.colors = colors;
        }
        
        public void ApplyThemeToText(TextMeshProUGUI text)
        {
            if (text == null) return;
            
            text.color = textColor;
            if (primaryFont != null)
                text.font = primaryFont;
            text.fontSize = baseFontSize;
        }
        
        public void ApplyThemeToPanel(Image panel)
        {
            if (panel == null) return;
            
            panel.color = backgroundColor;
        }
    }
}

