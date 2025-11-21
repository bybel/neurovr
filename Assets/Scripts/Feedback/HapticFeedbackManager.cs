using UnityEngine;
using UnityEngine.XR;
using NeuroReachVR.Input;
using static NeuroReachVR.Input.InputHandler;

namespace NeuroReachVR.Feedback
{
    /// <summary>
    /// Manages haptic feedback for hand tracking and controllers
    /// Supports Meta XR SDK and OpenXR haptic APIs
    /// </summary>
    public class HapticFeedbackManager : MonoBehaviour
    {
        [Header("Haptic Settings")]
        [SerializeField] private bool hapticEnabled = true;
        [SerializeField] private Handedness hapticHand = Handedness.Right;
        // defaultDuration reserved for future custom haptic implementation
        #pragma warning disable CS0414
        [SerializeField] private float defaultDuration = 0.1f;
        #pragma warning restore CS0414
        
        [Header("Feedback Presets")]
        [SerializeField] private HapticPreset successPreset = new HapticPreset(0.1f, 0.5f);
        [SerializeField] private HapticPreset errorPreset = new HapticPreset(0.2f, 0.8f);
        [SerializeField] private HapticPreset warningPreset = new HapticPreset(0.15f, 0.6f);
        [SerializeField] private HapticPreset guidancePreset = new HapticPreset(0.05f, 0.3f);
        
        private InputHandler inputHandler;
        
        private void Awake()
        {
            inputHandler = FindFirstObjectByType<InputHandler>();
            ValidatePresets();
        }
        
        private void ValidatePresets()
        {
            ValidatePreset(successPreset, "Success");
            ValidatePreset(errorPreset, "Error");
            ValidatePreset(warningPreset, "Warning");
            ValidatePreset(guidancePreset, "Guidance");
        }
        
        private void ValidatePreset(HapticPreset preset, string name)
        {
            if (preset.duration <= 0 || preset.amplitude < 0 || preset.amplitude > 1)
                Debug.LogWarning($"[HapticFeedback] Invalid {name} preset: duration={preset.duration}, amplitude={preset.amplitude}");
        }
        
        public void PlaySuccess()
        {
            if (!hapticEnabled) return;
            TriggerHaptic(successPreset);
        }
        
        public void PlayError()
        {
            if (!hapticEnabled) return;
            TriggerHaptic(errorPreset);
        }
        
        public void PlayWarning()
        {
            if (!hapticEnabled) return;
            TriggerHaptic(warningPreset);
        }
        
        public void PlayGuidance()
        {
            if (!hapticEnabled) return;
            TriggerHaptic(guidancePreset);
        }
        
        public void PlayCustom(float duration, float amplitude)
        {
            if (!hapticEnabled) return;
            TriggerHaptic(new HapticPreset(duration, amplitude));
        }
        
        private void TriggerHaptic(HapticPreset preset)
        {
            if (inputHandler == null || !inputHandler.HasValidInput) return;
            
            InputDevice device = GetInputDevice();
            if (device.isValid)
            {
                SendOpenXRHaptic(device, preset);
            }
        }
        
        private InputDevice GetInputDevice()
        {
            XRNode node = hapticHand == Handedness.Right ? XRNode.RightHand : XRNode.LeftHand;

            if (inputHandler.CurrentMode == InputMode.Hand)
            {
                // Use configured hand for haptics
                return InputDevices.GetDeviceAtXRNode(node);
            }
            else if (inputHandler.CurrentMode == InputMode.Stylus)
            {
                // Stylus typically held in dominant hand
                return InputDevices.GetDeviceAtXRNode(node);
            }

            return InputDevices.GetDeviceAtXRNode(node);
        }

        public void SetHapticHand(Handedness hand)
        {
            hapticHand = hand;
        }
        
        private void SendOpenXRHaptic(InputDevice device, HapticPreset preset)
        {
            if (!device.isValid) return;
            
            // Method 1: Use Unity XR haptic API (works with OpenXR)
            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, preset.amplitude, preset.duration);
                return;
            }
            
            // Method 2: Try OpenXR haptic extension APIs
            // Note: OpenXR haptics require action handles - would need Input Action setup
            // For now, Unity XR API should work with OpenXR backend
        }
        
        public void SetEnabled(bool enabled)
        {
            hapticEnabled = enabled;
        }
    }
    
    [System.Serializable]
    public struct HapticPreset
    {
        public float duration;
        public float amplitude;
        
        public HapticPreset(float duration, float amplitude)
        {
            this.duration = duration;
            this.amplitude = amplitude;
        }
    }
}

