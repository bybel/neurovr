using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// Improved stylus input using Unity Input System with Input Actions
    /// Properly integrates Logitech MX Ink pressure sensor via OpenXR
    /// </summary>
    public class StylusInputActionsManager : MonoBehaviour, IInputSource
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string stylusMapName = "Stylus";
        [SerializeField] private string positionActionName = "Position";
        [SerializeField] private string rotationActionName = "Rotation";
        [SerializeField] private string pressureActionName = "Pressure";
        [SerializeField] private string buttonActionName = "Button";

        [Header("Settings")]
        [SerializeField] private float minPressureThreshold = 0.1f;
        [SerializeField] private bool debugLogging = false;

        private InputAction positionAction;
        private InputAction rotationAction;
        private InputAction pressureAction;
        private InputAction buttonAction;
        private bool isInitialized;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private float lastPressure;

        public bool IsAvailable => isInitialized && positionAction != null;
        public bool IsTracking => IsAvailable && positionAction.phase == InputActionPhase.Performed;
        public Vector3 Position => IsTracking ? positionAction.ReadValue<Vector3>() : lastPosition;
        public Quaternion Rotation => IsTracking ? rotationAction.ReadValue<Quaternion>() : lastRotation;
        public float Confidence => IsTracking ? 1f : 0f;

        public float Pressure => IsTracking && pressureAction != null ? pressureAction.ReadValue<float>() : lastPressure;
        public Vector2 Tilt => CalculateTilt();
        public bool IsPressed => Pressure >= minPressureThreshold;
        public bool IsButtonPressed => IsTracking && buttonAction != null && buttonAction.ReadValue<float>() > 0.5f;

        private void OnEnable()
        {
            InitializeInputActions();
            EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        private void Update()
        {
            if (IsTracking)
            {
                lastPosition = Position;
                lastRotation = Rotation;
                lastPressure = Pressure;
            }
        }

        private void InitializeInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("[StylusInputActions] Input Actions asset not assigned! Please assign 'Input Actions' in inspector.");
                return;
            }

            var stylusMap = inputActions.FindActionMap(stylusMapName);
            if (stylusMap == null)
            {
                Debug.LogError($"[StylusInputActions] Action map '{stylusMapName}' not found. Please create it in Input Actions asset.");
                return;
            }

            positionAction = stylusMap.FindAction(positionActionName);
            rotationAction = stylusMap.FindAction(rotationActionName);
            pressureAction = stylusMap.FindAction(pressureActionName);
            buttonAction = stylusMap.FindAction(buttonActionName);

            if (positionAction == null)
                Debug.LogWarning($"[StylusInputActions] '{positionActionName}' action not found");
            if (rotationAction == null)
                Debug.LogWarning($"[StylusInputActions] '{rotationActionName}' action not found");
            if (pressureAction == null)
                Debug.LogWarning($"[StylusInputActions] '{pressureActionName}' action not found - pressure sensing disabled");
            if (buttonAction == null)
                Debug.LogWarning($"[StylusInputActions] '{buttonActionName}' action not found");

            isInitialized = positionAction != null;

            if (isInitialized && debugLogging)
                Debug.Log("[StylusInputActions] Initialized successfully with Input Actions");
        }

        private void EnableActions()
        {
            positionAction?.Enable();
            rotationAction?.Enable();
            pressureAction?.Enable();
            buttonAction?.Enable();
        }

        private void DisableActions()
        {
            positionAction?.Disable();
            rotationAction?.Disable();
            pressureAction?.Disable();
            buttonAction?.Disable();
        }

        private Vector2 CalculateTilt()
        {
            // Tilt derived from rotation Euler angles
            Vector3 euler = Rotation.eulerAngles;
            // Normalize to -180 to 180 range
            float x = euler.x > 180f ? euler.x - 360f : euler.x;
            float z = euler.z > 180f ? euler.z - 360f : euler.z;
            return new Vector2(x, z);
        }

        public void SetMinPressureThreshold(float threshold)
        {
            minPressureThreshold = Mathf.Clamp01(threshold);
        }

        public void Setup(InputActionAsset actions)
        {
            if (actions == null) return;
            this.inputActions = actions;
            InitializeInputActions();
            EnableActions();
        }
    }
}
