using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using System.Collections.Generic;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// Handles Logitech MX Ink stylus input via OpenXR extension
    /// Properly integrates OpenXR Logitech MX Ink stylus interaction extension
    /// Tracks position, pressure, tilt, and button states
    /// </summary>
    public class StylusInputManager : MonoBehaviour, IInputSource
    {
        [Header("Stylus Configuration")]
        [SerializeField] private float minPressureThreshold = 0.1f;
        [SerializeField] private bool debugLogging = false;
        
        private const string LOGITECH_MANUFACTURER = "Logitech";
        private const string MX_INK_PRODUCT = "MX Ink";
        private const string STYLUS_DEVICE_NAME = "Logitech MX Ink";
        
        private XRController stylusController;
        private UnityEngine.XR.InputDevice xrDevice;
        private bool isInitialized;
        private float lastPressure;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        
        public bool IsAvailable => isInitialized && (stylusController != null || xrDevice.isValid);
        public bool IsTracking => GetIsTracking();
        public Vector3 Position => IsTracking ? GetStylusPosition() : Vector3.zero;
        public Quaternion Rotation => IsTracking ? GetStylusRotation() : Quaternion.identity;
        public float Confidence => IsTracking ? 1f : 0f;
        
        public float Pressure => IsTracking ? GetPressure() : 0f;
        public Vector2 Tilt => IsTracking ? GetTilt() : Vector2.zero;
        public bool IsPressed => Pressure >= minPressureThreshold;
        public bool IsButtonPressed => IsTracking && GetButtonState();
        
        private void Start()
        {
            InitializeStylus();
        }
        
        private void Update()
        {
            if (!isInitialized)
                TryInitializeStylus();
            
            if (IsTracking)
            {
                lastPosition = GetStylusPosition();
                lastRotation = GetStylusRotation();
                lastPressure = GetPressure();
            }
        }
        
        private void InitializeStylus()
        {
            TryInitializeStylus();
        }
        
        private void TryInitializeStylus()
        {
            // Method 1: Try Unity Input System XR Controller
            stylusController = InputSystem.GetDevice<XRController>();
            if (stylusController != null)
            {
                if (IsLogitechStylus(stylusController))
                {
                    isInitialized = true;
                    if (debugLogging)
                        Debug.Log("[StylusInput] Logitech MX Ink detected via Input System");
                    return;
                }
            }
            
            // Method 2: Try XR InputDevice (OpenXR)
            var inputDevices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevices(inputDevices);
            
            foreach (var device in inputDevices)
            {
                if (IsLogitechStylusDevice(device))
                {
                    xrDevice = device;
                    isInitialized = true;
                    if (debugLogging)
                        Debug.Log($"[StylusInput] Logitech MX Ink detected: {device.name}");
                    return;
                }
            }
            
            // Method 3: Try Input System devices
            foreach (var device in InputSystem.devices)
            {
                if (IsLogitechStylus(device))
                {
                    isInitialized = true;
                    if (debugLogging)
                        Debug.Log($"[StylusInput] Logitech MX Ink detected via Input System: {device.name}");
                    return;
                }
            }
        }
        
        private bool IsLogitechStylus(UnityEngine.InputSystem.InputDevice device)
        {
            if (device == null) return false;
            
            var description = device.description;
            
            // Guard against null description fields - these can be null for some devices
            string manufacturer = description.manufacturer ?? string.Empty;
            string product = description.product ?? string.Empty;
            string deviceClass = description.deviceClass ?? string.Empty;
            
            return manufacturer.Contains(LOGITECH_MANUFACTURER) && 
                   (product.Contains(MX_INK_PRODUCT) || 
                    product.Contains("Stylus") ||
                    deviceClass == "Stylus");
        }
        
        private bool IsLogitechStylusDevice(UnityEngine.XR.InputDevice device)
        {
            if (!device.isValid) return false;
            
            // Check device characteristics
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.TrackedDevice))
            {
                // Check name/role for stylus
                return device.name.Contains("Stylus") || 
                       device.name.Contains("MX Ink") ||
                       device.name.Contains("Logitech");
            }
            
            return false;
        }
        
        private bool GetIsTracking()
        {
            if (!IsAvailable) return false;
            
            // Try Input System first
            if (stylusController != null)
            {
                return stylusController.isTracked.isPressed;
            }
            
            // Try XR InputDevice
            if (xrDevice.isValid)
            {
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked))
                    return tracked;
            }
            
            return false;
        }
        
        private Vector3 GetStylusPosition()
        {
            // Try Input System
            if (stylusController != null)
            {
                try
                {
                    return stylusController.devicePosition.ReadValue();
                }
                catch { }
            }
            
            // Try XR InputDevice
            if (xrDevice.isValid)
            {
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 position))
                    return position;
                
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trackingState, out InputTrackingState state))
                {
                    if (state.HasFlag(InputTrackingState.Position))
                        return lastPosition;
                }
            }
            
            return lastPosition;
        }
        
        private Quaternion GetStylusRotation()
        {
            // Try Input System
            if (stylusController != null)
            {
                try
                {
                    return stylusController.deviceRotation.ReadValue();
                }
                catch { }
            }
            
            // Try XR InputDevice
            if (xrDevice.isValid)
            {
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rotation))
                    return rotation;
            }
            
            return lastRotation;
        }
        
        private float GetPressure()
        {
            // OpenXR Logitech MX Ink extension provides pressure via stylus force/pressure
            // Try multiple methods to get pressure data
            
            // Method 1: Try Input System pressure/force
            if (stylusController != null)
            {
                try
                {
                    // Try to get trigger value as pressure
                    var triggerControl = stylusController.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("trigger");
                    if (triggerControl != null)
                    {
                        return triggerControl.ReadValue();
                    }
                }
                catch { }
            }
            
            // Method 2: Try XR InputDevice pressure features
            if (xrDevice.isValid)
            {
                // Try trigger as pressure indicator
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float trigger))
                {
                    return trigger;
                }
                
                // Try grip as alternative
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float grip))
                {
                    return grip;
                }
            }
            
            return lastPressure;
        }
        
        private Vector2 GetTilt()
        {
            // Tilt derived from rotation Euler angles
            Vector3 euler = Rotation.eulerAngles;
            // Normalize to -180 to 180 range
            float x = euler.x > 180f ? euler.x - 360f : euler.x;
            float z = euler.z > 180f ? euler.z - 360f : euler.z;
            return new Vector2(x, z);
        }
        
        private bool GetButtonState()
        {
            // Check primary button (stylus tip contact)
            if (stylusController != null)
            {
                try
                {
                    var button = stylusController.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
                    if (button != null && button.isPressed)
                        return true;
                }
                catch { }
            }
            
            if (xrDevice.isValid)
            {
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool pressed))
                    return pressed;
            }
            
            // Pressure threshold also indicates button press
            return IsPressed;
        }
        
        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }
        
        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }
        
        private void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
            {
                if (IsLogitechStylus(device))
                {
                    if (debugLogging)
                        Debug.Log("[StylusInput] Logitech MX Ink device added");
                    TryInitializeStylus();
                }
            }
            else if (change == InputDeviceChange.Removed && isInitialized)
            {
                if (IsLogitechStylus(device))
                {
                    if (debugLogging)
                        Debug.Log("[StylusInput] Logitech MX Ink device removed");
                    isInitialized = false;
                    stylusController = null;
                }
            }
        }
    }
}

