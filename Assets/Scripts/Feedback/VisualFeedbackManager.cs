using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuroReachVR.Feedback
{
    /// <summary>
    /// Manages visual feedback: particles, colors, UI overlays, highlighting
    /// </summary>
    public class VisualFeedbackManager : MonoBehaviour
    {
        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem successParticles;
        [SerializeField] private ParticleSystem errorParticles;
        [SerializeField] private ParticleSystem warningParticles;
        
        [Header("Colors")]
        [SerializeField] private Color successColor = Color.green;
        [SerializeField] private Color errorColor = Color.red;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color guidanceColor = Color.cyan;
        
        [Header("UI Overlays")]
        [SerializeField] private Image progressBar;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private GameObject highlightPrefab;
        
        public void PlaySuccess(Vector3 position)
        {
            PlayParticles(successParticles, position, successColor);
        }
        
        public void PlayError(Vector3 position)
        {
            PlayParticles(errorParticles, position, errorColor);
        }
        
        public void PlayWarning(Vector3 position)
        {
            PlayParticles(warningParticles, position, warningColor);
        }
        
        public void UpdateProgress(float progress)
        {
            if (progressBar != null)
                progressBar.fillAmount = Mathf.Clamp01(progress);
        }
        
        public void UpdateAccuracy(float accuracy)
        {
            if (accuracyText != null)
                accuracyText.text = $"Accuracy: {accuracy:P0}";
        }
        
        public void HighlightObject(GameObject target, Color color)
        {
            if (target == null || highlightPrefab == null) return;
            
            GameObject highlight = Instantiate(highlightPrefab, target.transform);
            highlight.transform.localPosition = Vector3.zero;
            
            var renderer = highlight.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;
        }
        
        public void SetPathColor(LineRenderer pathRenderer, Color color)
        {
            if (pathRenderer != null)
                pathRenderer.material.color = color;
        }
        
        private void PlayParticles(ParticleSystem particles, Vector3 position, Color color)
        {
            if (particles == null) return;
            
            particles.transform.position = position;
            var main = particles.main;
            main.startColor = color;
            particles.Play();
        }
    }
}

