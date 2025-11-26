using UnityEngine;
using UnityEngine.InputSystem;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// Input source for simulating hand/stylus input with a mouse in the Unity Editor.
    /// </summary>
    public class SimulatorInput : MonoBehaviour, IInputSource
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float depth = 1.0f;

        /// <summary>
        /// Returns true only if a mouse device is currently available.
        /// Will be false on VR devices without a mouse (like Meta Quest).
        /// </summary>
        public bool IsAvailable => Mouse.current != null;
        
        /// <summary>
        /// Returns true only if both mouse and camera are available for tracking.
        /// </summary>
        public bool IsTracking => Mouse.current != null && mainCamera != null;

        public Vector3 Position
        {
            get
            {
                if (mainCamera == null || Mouse.current == null)
                {
                    return Vector3.zero;
                }
                Vector3 mousePos = Mouse.current.position.ReadValue();
                mousePos.z = depth;
                return mainCamera.ScreenToWorldPoint(mousePos);
            }
        }

        public Quaternion Rotation => Quaternion.identity;
        public float Confidence => Mouse.current != null ? 1.0f : 0.0f;

        public bool IsPinching => Mouse.current?.leftButton.isPressed ?? false;
        public float PinchStrength => Mouse.current?.leftButton.isPressed == true ? 1.0f : 0.0f;
        public bool IsPressed => Mouse.current?.leftButton.isPressed ?? false;

        void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }
    }
}
