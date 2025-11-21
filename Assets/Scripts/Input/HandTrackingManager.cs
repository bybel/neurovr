using UnityEngine;
using UnityEngine.XR;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// LEGACY: Basic hand tracking using XR InputDevice (controller-based API)
    /// NOTE: Use HandTrackingXRHands for proper finger joint tracking
    /// This is kept for backward compatibility and as fallback
    /// </summary>
    public class HandTrackingManager : MonoBehaviour, IInputSource
    {
        [Header("Hand Configuration")]
        [SerializeField] private Handedness handType = Handedness.Right;
        [SerializeField] private float minConfidence = 0.5f;
        
        [Header("Pinch Detection")]
        [SerializeField] private float pinchThreshold = 0.7f;
        
        private const float MAX_PINCH_DISTANCE = 0.05f; // 5cm maximum distance for pinch detection
        
        private InputDevice handDevice;
        private bool isInitialized;
        
        public bool IsAvailable => isInitialized && handDevice.isValid;
        public bool IsTracking => IsAvailable && GetIsTracked() && Confidence >= minConfidence;
        public Vector3 Position => IsTracking ? GetHandPosition() : Vector3.zero;
        public Quaternion Rotation => IsTracking ? GetHandRotation() : Quaternion.identity;
        public float Confidence => IsAvailable ? GetHandConfidence() : 0f;
        
        public bool IsPinching => IsTracking && GetPinchStrength() >= pinchThreshold;
        public float PinchStrength => IsTracking ? GetPinchStrength() : 0f;
        
        private void Start()
        {
            InitializeHandTracking();
        }
        
        private void Update()
        {
            if (!isInitialized)
                InitializeHandTracking();
        }
        
        private void InitializeHandTracking()
        {
            var inputDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevices(inputDevices);
            
            InputDeviceCharacteristics characteristics = handType == Handedness.Left 
                ? InputDeviceCharacteristics.Left | InputDeviceCharacteristics.HandTracking
                : InputDeviceCharacteristics.Right | InputDeviceCharacteristics.HandTracking;
            
            foreach (var device in inputDevices)
            {
                if ((device.characteristics & characteristics) == characteristics)
                {
                    handDevice = device;
                    isInitialized = true;
                    Debug.Log($"[HandTracking] Initialized for {handType} hand");
                    return;
                }
            }
            
            if (!isInitialized)
                Debug.LogWarning($"[HandTracking] Hand tracking device not available for {handType}");
        }
        
        private bool GetIsTracked()
        {
            if (!handDevice.isValid) return false;
            return handDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;
        }
        
        private Vector3 GetHandPosition()
        {
            if (handDevice.isValid && handDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
                return position;
            
            return Vector3.zero;
        }
        
        private Quaternion GetHandRotation()
        {
            if (handDevice.isValid && handDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                return rotation;
            
            return Quaternion.identity;
        }
        
        private float GetHandConfidence()
        {
            if (!GetIsTracked()) return 0f;
            // Return 1.0 if tracked (confidence can be enhanced with XR_FB_hand_tracking_confidence extension)
            return 1f;
        }
        
        private float GetPinchStrength()
        {
            // Simplified pinch detection using grip value or trigger
            // Full hand joint tracking requires XR Hands package
            if (handDevice.isValid)
            {
                if (handDevice.TryGetFeatureValue(CommonUsages.grip, out float grip))
                    return grip;
                
                if (handDevice.TryGetFeatureValue(CommonUsages.trigger, out float trigger))
                    return trigger;
            }
            
            return 0f;
        }
    }
    
    public enum Handedness { Left, Right }
}

