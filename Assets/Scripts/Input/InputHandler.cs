using UnityEngine;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// Unified input handler that abstracts input sources
    /// Provides single interface for tasks regardless of input method
    /// NOW WITH: XR Hands package support for accurate finger tracking
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Input Sources - XR Hands (Recommended)")]
        [SerializeField] private HandTrackingXRHands leftHandXR;
        [SerializeField] private HandTrackingXRHands rightHandXR;

        [Header("Input Sources - Legacy Fallback")]

        [Header("Simulator Input")]
        [SerializeField] private SimulatorInput simulatorInput;

        [Header("Stylus Input")]
        [SerializeField] private StylusInputManager stylus;

        [Header("Input Preferences")]
        [SerializeField] private InputMode preferredMode = InputMode.Auto;
        [SerializeField] private Handedness preferredHand = Handedness.Right;
        [SerializeField] private bool useXRHandsPackage = true;

        private IInputSource activeInput;
        private InputMode currentMode;

        public IInputSource ActiveInput => activeInput;
        public InputMode CurrentMode => currentMode;
        public bool HasValidInput => activeInput != null && activeInput.IsTracking;

        public Vector3 Position => HasValidInput ? activeInput.Position : Vector3.zero;
        public Quaternion Rotation => HasValidInput ? activeInput.Rotation : Quaternion.identity;
        public float Confidence => HasValidInput ? activeInput.Confidence : 0f;

        private static bool IsValidInput(IInputSource source) =>
            source != null && source.IsAvailable && source.IsTracking;

        // Hand-specific properties
        public bool IsPinching
        {
            get
            {
                // Check activeInput type directly (works regardless of how input was selected)
                if (activeInput is SimulatorInput sim)
                {
                    bool isPinching = sim.IsPinching;
                    // Log when pinching state changes or periodically
                    if (isPinching)
                    {
                        Debug.Log($"[InputHandler] IsPinching from SimulatorInput: {isPinching}");
                    }
                    return isPinching;
                }

                if (currentMode != InputMode.Hand) return false;

                if (activeInput is HandTrackingXRHands handSource)
                    return handSource.IsPinching;



                return false;
            }
        }

        public float PinchStrength
        {
            get
            {
                // Check activeInput type directly (works regardless of how input was selected)
                if (activeInput is SimulatorInput sim)
                    return sim.PinchStrength;

                if (currentMode != InputMode.Hand) return 0f;

                if (activeInput is HandTrackingXRHands handSource)
                    return handSource.PinchStrength;



                return 0f;
            }
        }

        // Stylus-specific properties
        public float Pressure => currentMode == InputMode.Stylus ?
            stylus?.Pressure ?? 0f : 0f;
        public bool IsStylusPressed
        {
            get
            {
                // Check activeInput type directly (works regardless of how input was selected)
                if (activeInput is SimulatorInput sim)
                    return sim.IsPressed;

                return currentMode == InputMode.Stylus && stylus?.IsPressed == true;
            }
        }

        private void Awake()
        {
            // Auto-find or create SimulatorInput in Editor
            #if UNITY_EDITOR
            if (simulatorInput == null)
            {
                simulatorInput = FindFirstObjectByType<SimulatorInput>();
                
                // If still null, create one
                if (simulatorInput == null)
                {
                    Debug.Log("[InputHandler] Creating SimulatorInput...");
                    GameObject simObj = new GameObject("SimulatorInput_AutoCreated");
                    simulatorInput = simObj.AddComponent<SimulatorInput>();
                }
                else
                {
                    Debug.Log("[InputHandler] Auto-found SimulatorInput");
                }
            }
            #endif
        }

        private void Start()
        {
            // Force Auto mode to ensure we check for VR devices first, overriding Inspector defaults
            preferredMode = InputMode.Auto;
            
            // Don't force Simulator in Editor anymore, allow VR input (Link/AirLink) to work
            SelectInputSource();
            
            Debug.Log($"[InputHandler] Started with mode: {currentMode}, HasValidInput: {HasValidInput}");
        }

        private void Update()
        {
            // Auto-switch if current input becomes unavailable
            if (!HasValidInput && preferredMode == InputMode.Auto)
                SelectInputSource();
            
            // RETRY VR DETECTION: If we are in Simulator mode (fallback), keep trying to find VR input
            // This handles cases where controllers wake up AFTER the game starts
            if (currentMode == InputMode.Simulator && preferredMode == InputMode.Auto && Time.frameCount % 60 == 0)
            {
                // Check if any VR device is now available
                if (IsInputAvailable(InputMode.Stylus) || IsInputAvailable(InputMode.Hand))
                {
                    Debug.Log("[InputHandler] VR Input detected! Switching from Simulator...");
                    SelectInputSource();
                }
            }
                
            // Debug: Log active input source periodically
            if (Time.frameCount % 300 == 0) // Reduced frequency to 5s
            {
                Debug.Log($"[InputHandler] Active Source: {activeInput?.GetType().Name ?? "None"}, Mode: {currentMode}, Pos: {Position}");
            }
        }

        private void SelectInputSource()
        {
            switch (preferredMode)
            {
                case InputMode.Hand:
                    activeInput = GetActiveHand();
                    currentMode = activeInput != null ? InputMode.Hand : InputMode.None;
                    break;

                case InputMode.Stylus:
                    activeInput = stylus;
                    currentMode = stylus != null && stylus.IsAvailable ? InputMode.Stylus : InputMode.None;
                    break;
                
                case InputMode.Simulator:
                    activeInput = simulatorInput;
                    currentMode = simulatorInput != null ? InputMode.Simulator : InputMode.None;
                    break;

                case InputMode.Auto:
                    // Priority: Stylus > Preferred Hand > Other Hand
                    if (IsValidInput(stylus))
                    {
                        activeInput = stylus;
                        currentMode = InputMode.Stylus;
                    }
                    else if (IsValidInput(GetActiveHand()))
                    {
                        activeInput = GetActiveHand();
                        currentMode = InputMode.Hand;
                    }
                    else if (IsValidInput(GetOtherHand()))
                    {
                        activeInput = GetOtherHand();
                        currentMode = InputMode.Hand;
                    }
                    // Fallback to Simulator if no VR input found (Last Resort)
                    #if UNITY_EDITOR
                    else if (IsValidInput(simulatorInput))
                    {
                        activeInput = simulatorInput;
                        currentMode = InputMode.Simulator;
                        Debug.LogWarning("[InputHandler] VR Input not found - Falling back to Simulator");
                    }
                    #endif
                    else
                    {
                        activeInput = null;
                        currentMode = InputMode.None;
                        
                        // Debug: List all available devices to help diagnose why VR is failing
                        if (Time.frameCount % 300 == 0)
                        {
                            var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                            UnityEngine.XR.InputDevices.GetDevices(devices);
                            string deviceList = string.Join(", ", devices.ConvertAll(d => $"{d.name} ({d.characteristics})"));
                            Debug.LogWarning($"[InputHandler] No valid input found! Available XR Devices: {deviceList}");
                        }
                    }
                    break;
            }
        }

        private IInputSource GetActiveHand()
        {
            if (useXRHandsPackage)
            {
                IInputSource xrHand = preferredHand == Handedness.Right ? rightHandXR : leftHandXR;
                if (xrHand != null && xrHand.IsAvailable)
                    return xrHand;
            }

            // Fallback to null since legacy is removed
            return null;
        }

        private IInputSource GetOtherHand()
        {
            if (useXRHandsPackage)
            {
                IInputSource xrHand = preferredHand == Handedness.Right ? leftHandXR : rightHandXR;
                if (xrHand != null && xrHand.IsAvailable)
                    return xrHand;
            }

            // Fallback to null since legacy is removed
            return null;
        }

        /// <summary>
        /// Force switch to specific input mode
        /// </summary>
        public void SetInputMode(InputMode mode)
        {
            preferredMode = mode;
            SelectInputSource();
        }

        /// <summary>
        /// Set preferred hand at runtime
        /// </summary>
        public void SetPreferredHand(Handedness hand)
        {
            preferredHand = hand;
            SelectInputSource();
        }

        /// <summary>
        /// Toggle between XR Hands and legacy hand tracking
        /// </summary>
        public void SetUseXRHands(bool useXRHands)
        {
            useXRHandsPackage = useXRHands;
            SelectInputSource();
        }

        /// <summary>
        /// Check if specific input type is available
        /// </summary>
        public bool IsInputAvailable(InputMode mode)
        {
            return mode switch
            {
                InputMode.Hand => GetActiveHand()?.IsAvailable == true || GetOtherHand()?.IsAvailable == true,
                InputMode.Stylus => stylus?.IsAvailable == true,
                _ => false
            };
        }

        /// <summary>
        /// Get specific finger tip position (XR Hands only)
        /// </summary>
        public Vector3 GetFingerTipPosition(FingerType finger)
        {
            if (currentMode != InputMode.Hand || !useXRHandsPackage)
                return Vector3.zero;

            var handXR = activeInput as HandTrackingXRHands;
            return handXR?.GetFingerTipPosition(finger) ?? Vector3.zero;
        }
    }

    public enum InputMode
    {
        Auto,
        Hand,
        Stylus,
        Simulator,
        None
    }
}
