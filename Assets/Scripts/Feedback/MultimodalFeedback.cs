using UnityEngine;
using NeuroReachVR.Input;

namespace NeuroReachVR.Feedback
{
    /// <summary>
    /// Coordinates all feedback types for cohesive multimodal experience
    /// Single entry point for all feedback operations
    /// </summary>
    public class MultimodalFeedback : MonoBehaviour
    {
        [Header("Feedback Components")]
        [SerializeField] private HapticFeedbackManager hapticManager;
        [SerializeField] private VisualFeedbackManager visualManager;
        [SerializeField] private AudioFeedbackManager audioManager;
        
        [Header("Settings")]
        [SerializeField] private bool hapticEnabled = true;
        [SerializeField] private bool visualEnabled = true;
        [SerializeField] private bool audioEnabled = true;
        
        private void Awake()
        {
            if (hapticManager == null)
                hapticManager = GetComponent<HapticFeedbackManager>();
            
            if (visualManager == null)
                visualManager = GetComponent<VisualFeedbackManager>();
            
            if (audioManager == null)
                audioManager = GetComponent<AudioFeedbackManager>();
        }
        
        public void PlaySuccess(Vector3 position)
        {
            if (hapticEnabled && hapticManager != null)
                hapticManager.PlaySuccess();
            
            if (visualEnabled && visualManager != null)
                visualManager.PlaySuccess(position);
            
            if (audioEnabled && audioManager != null)
                audioManager.PlaySuccess();
        }
        
        public void PlayError(Vector3 position)
        {
            if (hapticEnabled && hapticManager != null)
                hapticManager.PlayError();
            
            if (visualEnabled && visualManager != null)
                visualManager.PlayError(position);
            
            if (audioEnabled && audioManager != null)
                audioManager.PlayError();
        }
        
        public void PlayWarning(Vector3 position)
        {
            if (hapticEnabled && hapticManager != null)
                hapticManager.PlayWarning();
            
            if (visualEnabled && visualManager != null)
                visualManager.PlayWarning(position);
            
            if (audioEnabled && audioManager != null)
                audioManager.PlayWarning();
        }
        
        public void PlayGuidance(Vector3 position)
        {
            if (hapticEnabled && hapticManager != null)
                hapticManager.PlayGuidance();
            
            if (visualEnabled && visualManager != null)
                visualManager.PlayWarning(position); // Reuse warning visual
            
            if (audioEnabled && audioManager != null)
                audioManager.PlayGuidance();
        }
        
        public void UpdateProgress(float progress)
        {
            if (visualEnabled && visualManager != null)
                visualManager.UpdateProgress(progress);
        }
        
        public void UpdateAccuracy(float accuracy)
        {
            if (visualEnabled && visualManager != null)
                visualManager.UpdateAccuracy(accuracy);
        }
        
        public void HighlightObject(GameObject target, FeedbackType type = FeedbackType.Success)
        {
            if (visualEnabled && visualManager != null)
            {
                Color color = GetColorForType(type);
                visualManager.HighlightObject(target, color);
            }
        }
        
        public void SetPathColor(LineRenderer pathRenderer, FeedbackType type)
        {
            if (visualEnabled && visualManager != null)
            {
                Color color = GetColorForType(type);
                visualManager.SetPathColor(pathRenderer, color);
            }
        }
        
        public void SetFeedbackEnabled(FeedbackChannel channel, bool enabled)
        {
            switch (channel)
            {
                case FeedbackChannel.Haptic:
                    hapticEnabled = enabled;
                    if (hapticManager != null)
                        hapticManager.SetEnabled(enabled);
                    break;
                case FeedbackChannel.Visual:
                    visualEnabled = enabled;
                    break;
                case FeedbackChannel.Audio:
                    audioEnabled = enabled;
                    if (audioManager != null)
                        audioManager.SetMusicEnabled(enabled);
                    break;
            }
        }
        
        private static readonly Color SUCCESS_COLOR = Color.green;
        private static readonly Color ERROR_COLOR = Color.red;
        private static readonly Color WARNING_COLOR = Color.yellow;
        private static readonly Color GUIDANCE_COLOR = Color.cyan;
        
        private Color GetColorForType(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.Success => SUCCESS_COLOR,
                FeedbackType.Error => ERROR_COLOR,
                FeedbackType.Warning => WARNING_COLOR,
                FeedbackType.Guidance => GUIDANCE_COLOR,
                _ => Color.white
            };
        }
    }
    
    public enum FeedbackType
    {
        Success,
        Error,
        Warning,
        Guidance
    }
    
    public enum FeedbackChannel
    {
        Haptic,
        Visual,
        Audio
    }
}

