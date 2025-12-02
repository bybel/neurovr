using UnityEngine;
using UnityEngine.InputSystem;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// Input source for simulating hand/stylus input with a mouse in the Unity Editor.
    /// Enables testing balloon pop and other tasks without VR hardware.
    /// Projects mouse position onto a plane at specified depth from camera.
    /// </summary>
    public class SimulatorInput : MonoBehaviour, IInputSource
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        
        [Header("Simulation Settings")]
        [Tooltip("Distance from camera where mouse position is projected (should match task spawn distance)")]
        [SerializeField] private float interactionDepth = 1.5f;
        [SerializeField] private bool showDebugInfo = true;
        
        private Vector3 currentPosition;
        private float lastLogTime;
        
        /// <summary>
        /// Always available in Editor with mouse
        /// </summary>
        public bool IsAvailable => true;
        
        /// <summary>
        /// Always tracking in Editor
        /// </summary>
        public bool IsTracking => mainCamera != null;

        public Vector3 Position => currentPosition;

        public Quaternion Rotation => Quaternion.identity;
        public float Confidence => 1.0f;

        /// <summary>
        /// Check for pinch input using multiple methods:
        /// 1. VR Controller trigger button (works with Meta XR Simulator)
        /// 2. Mouse left button (may not work when Game View doesn't have focus)
        /// 3. Keyboard Space bar (reliable fallback for testing)
        /// 4. Keyboard F key (alternative)
        /// </summary>
        public bool IsPinching
        {
            get
            {
                // Method 1: VR Controller trigger (works even when Game View doesn't have focus)
                bool vrTriggerPressed = CheckVRControllerTrigger();
                
                // Method 2: Mouse left button (only works when Game View has focus)
                bool mousePressed = false;
                if (Mouse.current != null)
                {
                    mousePressed = Mouse.current.leftButton.isPressed || 
                                   Mouse.current.leftButton.wasPressedThisFrame ||
                                   Mouse.current.leftButton.ReadValue() > 0.5f;
                }
                
                // Method 3: Keyboard Space bar (reliable fallback when Meta XR Simulator intercepts mouse)
                bool spacePressed = false;
                if (Keyboard.current != null)
                {
                    spacePressed = Keyboard.current.spaceKey.isPressed;
                }
                
                // Method 4: Keyboard F key (alternative trigger)
                bool fKeyPressed = false;
                if (Keyboard.current != null)
                {
                    fKeyPressed = Keyboard.current.fKey.isPressed;
                }
                
                return vrTriggerPressed || mousePressed || spacePressed || fKeyPressed;
            }
        }
        
        // Cached list to avoid GC allocations
        private readonly System.Collections.Generic.List<UnityEngine.XR.InputDevice> cachedControllers = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        
        /// <summary>
        /// Check if VR controller trigger is pressed (works with Meta XR Simulator)
        /// </summary>
        private bool CheckVRControllerTrigger()
        {
            cachedControllers.Clear();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Controller, 
                cachedControllers);
            
            foreach (var device in cachedControllers)
            {
                // Check trigger button
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                    return true;
                
                // Check trigger value (analog)
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue) && triggerValue > 0.5f)
                    return true;
                    
                // Check primary button (A/X button)
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
                    return true;
            }
            
            return false;
        }
        
        public float PinchStrength => IsPinching ? 1.0f : 0.0f;
        public bool IsPressed => IsPinching;

        void Awake()
        {
            FindCamera();
        }
        
        void Start()
        {
            FindCamera();
            Debug.Log($"[SimulatorInput] Started - Camera: {(mainCamera != null ? mainCamera.name : "NULL")}, Mouse available: {Mouse.current != null}");
        }
        
        void Update()
        {
            // Ensure camera reference is valid
            if (mainCamera == null)
                FindCamera();
            
            // Update position every frame
            UpdatePosition();
            
            // VERBOSE DEBUG: Log input state every second
            if (showDebugInfo && Time.time - lastLogTime > 1.0f)
            {
                bool mouseExists = Mouse.current != null;
                bool keyboardExists = Keyboard.current != null;
                bool mousePressed = Mouse.current?.leftButton.isPressed ?? false;
                bool spacePressed = Keyboard.current?.spaceKey.isPressed ?? false;
                bool fKeyPressed = Keyboard.current?.fKey.isPressed ?? false;
                bool vrTrigger = CheckVRControllerTrigger();
                
                Debug.Log($"[SimulatorInput] Input - VRTrigger: {vrTrigger}, Mouse: {mousePressed}, Space: {spacePressed}, F: {fKeyPressed}, IsPinching: {IsPinching}, Position: {currentPosition}");
                
                // Extra debug: Check if Game view has focus
                #if UNITY_EDITOR
                bool hasFocus = UnityEditor.EditorWindow.focusedWindow != null && 
                               UnityEditor.EditorWindow.focusedWindow.GetType().Name == "GameView";
                Debug.Log($"[SimulatorInput] Game View Focus: {hasFocus}");
                #endif
                
                lastLogTime = Time.time;
            }
            
            // Debug logging when clicking
            if (showDebugInfo && IsPinching)
            {
                Debug.Log($"[SimulatorInput] CLICK DETECTED at position: {currentPosition}");
            }
        }
        
        private void UpdatePosition()
        {
            if (mainCamera == null || Mouse.current == null)
                return;
            
            // Get mouse position in screen coordinates
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            
            // Create a ray from camera through mouse position
            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
            
            // Project to a point at the interaction depth
            currentPosition = ray.GetPoint(interactionDepth);
        }
        
        private void FindCamera()
        {
            if (mainCamera != null) return;
            
            mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                // Find any camera in scene
                Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (var cam in cameras)
                {
                    if (cam.gameObject.activeInHierarchy)
                    {
                        mainCamera = cam;
                        break;
                    }
                }
            }
            
            if (mainCamera != null)
                Debug.Log($"[SimulatorInput] Using camera: {mainCamera.name}");
            else
                Debug.LogError("[SimulatorInput] No camera found!");
        }
        
        /// <summary>
        /// Draw debug visualization
        /// </summary>
        void OnDrawGizmos()
        {
            if (!showDebugInfo || mainCamera == null) return;
            
            // Draw current position
            Gizmos.color = IsPinching ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(currentPosition, 0.3f);
            
            if (IsPinching)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(currentPosition, 0.15f);
            }
        }
        
        /// <summary>
        /// On-screen debug display
        /// </summary>
        void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 12;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.cyan;
            
            string cameraStatus = mainCamera != null ? mainCamera.name : "NULL";
            bool vrTrigger = CheckVRControllerTrigger();
            bool mousePressed = Mouse.current?.leftButton.isPressed ?? false;
            
            GUI.Box(new Rect(10, Screen.height - 120, 300, 110), "");
            GUI.Label(new Rect(15, Screen.height - 115, 290, 105),
                $"=== Simulator Input ===\n" +
                $"Camera: {cameraStatus}\n" +
                $"Position: {currentPosition:F2}\n" +
                $"VR Trigger: {vrTrigger} | Mouse: {mousePressed}\n" +
                $"IsPinching: {IsPinching}\n" +
                $"Press VR Trigger or Space to pop!", style);
        }
    }
}
