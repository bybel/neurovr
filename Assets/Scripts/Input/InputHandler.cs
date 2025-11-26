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
        [SerializeField] private HandTrackingManager leftHandLegacy;
        [SerializeField] private HandTrackingManager rightHandLegacy;

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
                    return sim.IsPinching;

                if (currentMode != InputMode.Hand) return false;

                if (activeInput is HandTrackingXRHands handSource)
                    return handSource.IsPinching;

                if (activeInput is HandTrackingManager legacyHand)
                    return legacyHand.IsPinching;

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

                if (activeInput is HandTrackingManager legacyHand)
                    return legacyHand.PinchStrength;

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

        private void Start()
        {
            SelectInputSource();
        }

        private void Update()
        {
            // Auto-switch if current input becomes unavailable
            if (!HasValidInput && preferredMode == InputMode.Auto)
                SelectInputSource();
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
                    #if UNITY_EDITOR
                    else if (IsValidInput(simulatorInput))
                    {
                        activeInput = simulatorInput;
                        currentMode = InputMode.Simulator;
                    }
                    #endif
                    else
                    {
                        activeInput = null;
                        currentMode = InputMode.None;
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

            // Fallback to legacy
            return preferredHand == Handedness.Right ? rightHandLegacy : leftHandLegacy;
        }

        private IInputSource GetOtherHand()
        {
            if (useXRHandsPackage)
            {
                IInputSource xrHand = preferredHand == Handedness.Right ? leftHandXR : rightHandXR;
                if (xrHand != null && xrHand.IsAvailable)
                    return xrHand;
            }

            // Fallback to legacy
            return preferredHand == Handedness.Right ? leftHandLegacy : rightHandLegacy;
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
