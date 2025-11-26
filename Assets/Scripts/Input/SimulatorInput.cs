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

        public bool IsAvailable => true;
        public bool IsTracking => true;

        public Vector3 Position
        {
            get
            {
                if (mainCamera == null)
                {
                    return Vector3.zero;
                }
                Vector3 mousePos = Mouse.current.position.ReadValue();
                mousePos.z = depth;
                return mainCamera.ScreenToWorldPoint(mousePos);
            }
        }

        public Quaternion Rotation => Quaternion.identity;
        public float Confidence => 1.0f;

        public bool IsPinching => Mouse.current.leftButton.isPressed;
        public float PinchStrength => Mouse.current.leftButton.isPressed ? 1.0f : 0.0f;
        public bool IsPressed => Mouse.current.leftButton.isPressed;

        void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }
    }
}
