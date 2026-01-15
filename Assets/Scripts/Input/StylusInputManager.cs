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
        
        // Calibration Offsets (Applied to raw device data)
        private Vector3 calibrationPositionOffset = Vector3.zero;
        private Vector3 calibrationRotationOffset = Vector3.zero;
        
        private XRController stylusController;
        private UnityEngine.XR.InputDevice xrDevice;
        private bool isInitialized;
        private float lastPressure;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        
        // Sticky Availability Logic to prevent "flapping"
        private float lastAvailableTime = 0f;
        private const float STICKY_TIMEOUT = 3.0f; // Keep "Available" for 3s after loss
        
        private StylusInputActionsManager actionsManager;

        public void SetCalibration(Vector3 posOffset, Vector3 rotOffset)
        {
            Debug.Log($"[StylusInputManager] SetCalibration called: Pos={posOffset}, Rot={rotOffset}");
            calibrationPositionOffset = posOffset;
            calibrationRotationOffset = rotOffset;
        }
        
        public bool IsAvailable 
        {
            get
            {
                 bool rawAvailable = (actionsManager != null && actionsManager.IsAvailable) || (isInitialized && (stylusController != null || xrDevice.isValid));
                 if (rawAvailable) lastAvailableTime = Time.time; // Update heartbeat
                 return rawAvailable || (Time.time - lastAvailableTime < STICKY_TIMEOUT);
            }
        }
        public bool IsTracking => (actionsManager != null && actionsManager.IsTracking) || GetIsTracking();
        
        public Vector3 Position 
        {
            get
            {
                // Prioritize ActionsManager IF it has valid data
                if (actionsManager != null && actionsManager.IsTracking)
                {
                    Vector3 actionPos = actionsManager.Position;
                    if (actionPos != Vector3.zero) // Simple validation: don't accept zero if likely invalid
                        return actionPos + (actionsManager.Rotation * calibrationPositionOffset);
                }
                
                // Fallback to internal methods (XR Device / Legacy)
                return IsTracking ? GetStylusPosition() : Vector3.zero;
            }
        }

        public Quaternion Rotation 
        {
            get
            {
                if (actionsManager != null && actionsManager.IsTracking)
                    return actionsManager.Rotation * Quaternion.Euler(calibrationRotationOffset);
                return IsTracking ? GetStylusRotation() : Quaternion.identity;
            }
        }

        public float Confidence => IsTracking ? 1f : 0f;
        
        public Vector3 RawPosition 
        {
            get
            {
                // Bypass IsTracking check to return whatever the driver says
                if (actionsManager != null)
                {
                    Vector3 p = actionsManager.Position;
                    if (p != Vector3.zero) return p + (actionsManager.Rotation * calibrationPositionOffset);
                }
                return GetStylusPosition();
            }
        }

        public Quaternion RawRotation 
        {
            get
            {
                // Bypass IsTracking check
                if (actionsManager != null)
                {
                    Quaternion r = actionsManager.Rotation;
                    // Check validity roughly
                    if (r.w != 0 || r.x != 0 || r.y != 0 || r.z != 0) 
                        return r * Quaternion.Euler(calibrationRotationOffset);
                }
                return GetStylusRotation();
            }
        }
        
        public float Pressure 
        {
            get
            {
                if (actionsManager != null && actionsManager.IsTracking) return actionsManager.Pressure;
                return IsTracking ? GetPressure() : 0f;
            }
        }
        
        public Vector2 Tilt => IsTracking ? GetTilt() : Vector2.zero; // ActionsManager has Tilt too, but let's stick to this for now unless needed
        
        public bool IsPressed => Pressure >= minPressureThreshold;
        
        public bool IsButtonPressed 
        {
            get
            {
                if (actionsManager != null && actionsManager.IsTracking && actionsManager.IsButtonPressed) return true;
                return IsTracking && GetButtonState();
            }
        }
        
        private void Start()
        {
            // Initialize Actions Manager (Robust Fallback/Primary)
            actionsManager = GetComponent<StylusInputActionsManager>();
            if (actionsManager == null)
            {
                actionsManager = gameObject.AddComponent<StylusInputActionsManager>();
                Debug.Log("[StylusInputManager] Added StylusInputActionsManager dynamically.");
            }
            
            var assets = Resources.Load<InputActionAsset>("NeuroInputActions");
            
            #if UNITY_EDITOR
            if (assets == null)
            {
                // Fallback: Try to find "NeuroInputActions.inputactions" anywhere in the project
                string[] guids = UnityEditor.AssetDatabase.FindAssets("NeuroInputActions t:InputActionAsset");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    // Prioritize the user's custom asset in Assets folder
                    if (!path.Contains("Package") && path.EndsWith("NeuroInputActions.inputactions"))
                    {
                        assets = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
                        Debug.Log($"[StylusInputManager] Found Custom InputActions at: {path}");
                        break;
                    }
                }
                
                // If still null, just take the first one (but log warning)
                if (assets == null && guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    assets = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
                    Debug.LogWarning($"[StylusInputManager] Warning: Using fallback InputActions found at: {path}");
                }
            }
            #endif

            if (assets != null)
            {
                actionsManager.Setup(assets);
                Debug.Log("[StylusInputManager] Configured InputActions.");
            }
            else
            {
                Debug.LogWarning("[StylusInputManager] Could not find 'InputActions' in Resources or Project! Stylus input will fail.");
            }

            InitializeStylus();
            debugLogging = true; // FORCE DEBUG LOGGING
            Debug.Log($"[StylusInputManager] Initialization Complete. IsTracking: {IsTracking}, ActionsAvailable: {actionsManager?.IsAvailable}");
        }
        
        private float nextDeviceCheckTime = 0f;

        private void Update()
        {
            if (isInitialized)
            {
               bool currentlyAvailable = (stylusController != null && stylusController.enabled) || xrDevice.isValid;
               
               if (currentlyAvailable)
               {
                   lastAvailableTime = Time.time;
               }
               else
               {
                   // If we get here, device became invalid/disconnected
                   // Only reset isInitialized if we exceeded the sticky timeout
                   if (Time.time - lastAvailableTime > STICKY_TIMEOUT)
                   {
                        Debug.LogWarning($"[StylusInput] Stylus device lost (Timeout {STICKY_TIMEOUT}s).");
                        isInitialized = false;
                        xrDevice = default;
                        stylusController = null;
                   }
               }
            }

            // Only try to initialize periodically to avoid spamming and overhead
            if ((!isInitialized) && Time.time > nextDeviceCheckTime)
            {
                nextDeviceCheckTime = Time.time + 1.5f; // Check every 1.5 seconds
                TryInitializeStylus();
                if (isInitialized) lastAvailableTime = Time.time;
            }
        }
        
        private void InitializeStylus()
        {
            TryInitializeStylus();
        }
        
        private bool IsLogitechStylus(UnityEngine.InputSystem.InputDevice device)
        {
            if (device == null) return false;
            
            // Check for explicit Logitech product names
            string productName = device.description.product;
            string deviceName = device.name;
            string manufacturer = device.description.manufacturer;
            
            bool isLogitech = (manufacturer != null && manufacturer.Contains(LOGITECH_MANUFACTURER)) ||
                              (productName != null && (productName.Contains(MX_INK_PRODUCT) || productName.Contains("VR Ink") || productName.Contains("Stylus")));

            if (isLogitech) return true;
                
            // FALLBACK: Accept "OpenXR" generic controllers if no explicit match
            // This allows the system to pick up the device even if recognized as a generic controller by OpenXR
            if (deviceName.Contains("OpenXR") || deviceName.Contains("XRController"))
            {
                // We'll try to use it if it's not a Headset
                bool isHeadset = false;
                foreach (var usage in device.usages)
                {
                    if (usage.ToString() == "LeftEye" || usage.ToString() == "RightEye")
                    {
                        isHeadset = true;
                        break;
                    }
                }

                if (!isHeadset)
                {
                     return true; 
                }
            }
            
            return false;
        }
        private void TryInitializeStylus()
        {
            // Method 1: Try Unity Input System XR Controller (Explicit Logitech)
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
            
            // Method 2: Try XR InputDevice (OpenXR) - Generic Scan
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
            
            // Method 3: Try Input System devices (Generic Scan)
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

            // Method 4: Fallback to Right Controller (Generic) if no specific Stylus found
            // This is critical for stabilizing input if the specific driver isn't working
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);
            if (rightHandDevices.Count > 0)
            {
                xrDevice = rightHandDevices[0];
                isInitialized = true;
                if (debugLogging)
                    Debug.Log($"[StylusInput] Fallback: Using Right Controller as Stylus: {xrDevice.name}");
                return;
            }
            
            if (debugLogging && inputDevices.Count == 0)
            {
                 // Only log this if we really found nothing at all
                 // Debug.LogWarning("[StylusInput] No XR InputDevices found yet.");
            }
        }
        

        
        private bool IsLogitechStylusDevice(UnityEngine.XR.InputDevice device)
        {
            if (!device.isValid) return false;
            
            // Check device characteristics
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.TrackedDevice))
            {
                // Relaxed check: Accept anything with "Stylus" or "Pen"
                if (device.name.Contains("Stylus") || 
                    device.name.Contains("MX Ink") ||
                    device.name.Contains("Logitech") ||
                    device.name.Contains("Pen"))
                {
                    return true;
                }
                // If we want to be SURE, let's check for Right Controller here too if the name is ambiguous.
            }
            
            return false;
        }
        
        private bool GetIsTracking()
        {
            if (!IsAvailable) return false;
            
            // Try Input System first
            if (stylusController != null)
            {
                try
                {
                    // Safety check: Ensure device is actually added to the system
                    if (!stylusController.added) return false;

                    var isTrackedControl = stylusController.isTracked;
                    if (isTrackedControl != null)
                    {
                        return isTrackedControl.isPressed;
                    }
                }
                catch (System.Exception)
                {
                    // Suppress errors if device is not ready
                    return false;
                }
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
            Vector3 rawPos = lastPosition;
            Quaternion rawRot = lastRotation; // Need rot for offset calculation
            bool successPos = false;

            // Try Input System
            if (stylusController != null)
            {
                try { 
                    Vector3 tempPos = stylusController.devicePosition.ReadValue(); 
                    // Only accept if non-zero
                    if (tempPos != Vector3.zero)
                    {
                        rawPos = tempPos;
                        successPos = true;
                    }
                } catch { }
            }
            
            // Try XR InputDevice (fallback if InputSystem failed or returned zero)
            if (!successPos && xrDevice.isValid)
            {
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 p)) 
                {
                    // Only accept if non-zero
                    if (p != Vector3.zero)
                    {
                        rawPos = p;
                        successPos = true;
                    }
                }
            }

            if (debugLogging && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[StylusInput] GetPos: Success={successPos}, RawPos={rawPos}, ValidDevice={xrDevice.isValid||stylusController!=null}");
            }
            
            // Apply Calibration (Tip Position = RawPos + (RawRot * Offset))
            // Note: We use the helper GetStylusRotation() to ensure we get the calibrated rotation
            return rawPos + (GetStylusRotation() * calibrationPositionOffset);
        }
        
        private Quaternion GetStylusRotation()
        {
            Quaternion rawRot = lastRotation;
            bool successRot = false;

            // Try Input System
            if (stylusController != null)
            {
                try { 
                    Quaternion tempRot = stylusController.deviceRotation.ReadValue(); 
                    // Only accept if valid (w can't be 0 if normalized) and not identity? IDENTITY IS VALID though.
                    // But if it's (0,0,0,0), it's invalid.
                    if (tempRot.w != 0 || tempRot.x != 0 || tempRot.y != 0 || tempRot.z != 0) 
                    {
                         rawRot = tempRot;
                         successRot = true;
                    }
                } catch { }
            }
            
            // Try XR InputDevice
            if (!successRot && xrDevice.isValid)
            {
               if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion r)) 
               {
                   rawRot = r;
                   successRot = true;
               }
            }

            if (debugLogging && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[StylusInput] GetRot: Success={successRot}, RawRot={rawRot.eulerAngles}");
            }

            return rawRot * Quaternion.Euler(calibrationRotationOffset);
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
            // Check primary button (stylus tip contact or front button)
            if (stylusController != null)
            {
                try
                {
                    var primary = stylusController.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
                    if (primary != null && primary.isPressed) return true;
                    
                    var secondary = stylusController.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("secondaryButton");
                    if (secondary != null && secondary.isPressed) return true;
                    
                    var grip = stylusController.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("gripButton");
                    if (grip != null && grip.isPressed) return true;
                }
                catch { }
            }
            
            if (xrDevice.isValid)
            {
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primary) && primary) return true;
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool secondary) && secondary) return true;
                if (xrDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool grip) && grip) return true;
            }
            
            // FORCE DEBUG: If pressure is high, we should return true?
            // Pressure threshold also indicates button press
            // Debug.Log($"[StylusInput] Pressure: {Pressure}, IsPressed: {IsPressed}");
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

