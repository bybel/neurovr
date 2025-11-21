using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Manages visual hierarchy for clear UI structure
    /// Ensures important elements stand out
    /// </summary>
    public static class VisualHierarchy
    {
        private const float PRIMARY_SCALE = 1.2f;
        private const float SECONDARY_SCALE = 1.0f;
        private const float TERTIARY_SCALE = 0.9f;
        
        /// <summary>
        /// Apply visual hierarchy to UI elements
        /// </summary>
        public static void ApplyHierarchy(GameObject primaryElement, GameObject[] secondaryElements, GameObject[] tertiaryElements)
        {
            if (primaryElement != null)
                SetElementScale(primaryElement, PRIMARY_SCALE);
            
            if (secondaryElements != null)
            {
                foreach (var element in secondaryElements)
                    SetElementScale(element, SECONDARY_SCALE);
            }
            
            if (tertiaryElements != null)
            {
                foreach (var element in tertiaryElements)
                    SetElementScale(element, TERTIARY_SCALE);
            }
        }
        
        private static void SetElementScale(GameObject element, float scale)
        {
            RectTransform rect = element.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one * scale;
            }
        }
        
        /// <summary>
        /// Add visual indicator (icon, border, etc.)
        /// </summary>
        public static void AddIndicator(GameObject element, Color indicatorColor)
        {
            if (element == null) return;
            
            Image image = element.GetComponent<Image>();
            if (image != null)
            {
                image.color = indicatorColor;
            }
            
            // Add outline/border
            Outline outline = element.GetComponent<Outline>();
            if (outline == null)
                outline = element.AddComponent<Outline>();
            
            outline.effectColor = indicatorColor;
            outline.effectDistance = new Vector2(2, 2);
        }
        
        /// <summary>
        /// Highlight progress/status with color
        /// </summary>
        public static void SetProgressIndicator(Image progressBar, float progress, Color color)
        {
            if (progressBar == null) return;
            
            progressBar.fillAmount = Mathf.Clamp01(progress);
            progressBar.color = color;
        }
    }
}

