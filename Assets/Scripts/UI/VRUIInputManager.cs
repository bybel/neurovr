using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Clean VR UI Input Manager for Meta Quest.
    /// Supports: Logitech MX Ink (Stylus), Touch Controllers, Hand Tracking.
    /// NO Gaze fallback. NO Simulator hacks.
    /// </summary>
    public class VRUIInputManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float raycastDistance = 10f;
        [SerializeField] private LayerMask uiLayerMask = -1;
        [SerializeField] private Color pointerColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        [SerializeField] private Vector3 pointerRotationOffset = new Vector3(60, 0, 0); // Default offset for Stylus
        [SerializeField] private Vector3 pointerPositionOffset = new Vector3(0, 0.03f, 0.095f); // User-calibrated offset
        
        public Vector3 PointerRotationOffset => pointerRotationOffset;
        public Vector3 PointerPositionOffset => pointerPositionOffset;
        
        [Header("Visuals")]
        [SerializeField] private LineRenderer pointerLine;
        [SerializeField] private GameObject pointerDot;

        [Header("Calibration")]
        [SerializeField] private bool forceRecommendedOffsets = true;
        
        private Camera mainCamera;
        private EventSystem eventSystem;
        private GraphicRaycaster graphicRaycaster;
        private Canvas worldCanvas;
        
        // State
        private bool isPointerActive;
        private Vector3 pointerStart;
        private Vector3 pointerEnd;
        private GameObject currentHoveredObject;
        private bool pointerAllowed = true; // Default to true
        
        // Caching
        private readonly List<RaycastResult> cachedRaycastResults = new List<RaycastResult>();
        private readonly List<UnityEngine.XR.InputDevice> cachedInputDevices = new List<UnityEngine.XR.InputDevice>();
        private PointerEventData cachedPointerEventData;
        private GraphicRaycaster[] cachedRaycasters;
        private float nextRaycasterRefreshTime;

        private void Awake()
        {
            // Force user-calibrated offsets if enabled
            if (forceRecommendedOffsets)
            {
                pointerRotationOffset = new Vector3(60, 0, 0);
                pointerPositionOffset = new Vector3(0, 0.03f, 0.095f);
            }

            mainCamera = Camera.main;
            SetupEventSystem();
            SetupVisuals();
            
            cachedPointerEventData = new PointerEventData(eventSystem);
        }

        private void SetupEventSystem()
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject es = new GameObject("EventSystem");
                eventSystem = es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                // Ensure we use the new Input System module
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                {
                    // If old module exists, warn or replace? For now, just add new one if missing.
                    // Ideally user should have set this up, but we ensure it works.
                    var old = eventSystem.GetComponent<StandaloneInputModule>();
                    if (old != null) Destroy(old);
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
        }

        private void SetupVisuals()
        {
            if (pointerLine == null)
            {
                GameObject lineObj = new GameObject("VRPointerLine");
                lineObj.transform.SetParent(transform);
                pointerLine = lineObj.AddComponent<LineRenderer>();
                // Force Mobile/Particles/Alpha Blended for robust VR visibility
                Shader shader = Shader.Find("Mobile/Particles/Alpha Blended");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                
                Material mat = new Material(shader);
                mat.renderQueue = 4000; // Overlay
                mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                pointerLine.material = mat;
                pointerLine.startColor = pointerColor;
                pointerLine.endColor = pointerColor;
                pointerLine.startWidth = 0.005f;
                pointerLine.endWidth = 0.002f;
                pointerLine.positionCount = 2;
                pointerLine.enabled = false;
            }

            if (pointerDot == null)
            {
                pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointerDot.name = "VRPointerDot";
                pointerDot.transform.SetParent(transform);
                pointerDot.transform.localScale = Vector3.one * 0.015f;
                var rend = pointerDot.GetComponent<Renderer>();
                if (rend) rend.material.color = pointerColor;
                Destroy(pointerDot.GetComponent<Collider>());
                pointerDot.SetActive(false);
            }
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            HandleInput();
            UpdateVisuals();
        }

        private void HandleInput()
        {
            // 1. Determine Ray Origin & Direction based on Priority
            // Priority: Stylus -> Right Controller -> Left Controller -> Hands
            
            Vector3 origin = Vector3.zero;
            Vector3 direction = Vector3.zero;
            bool foundDevice = false;

            // Check Devices
            if (TryGetDevice(InputDeviceCharacteristics.Controller, out origin, out direction)) // Generic Controller (covers Stylus/Right)
            {
                foundDevice = true;
            }
            else if (TryGetDevice(InputDeviceCharacteristics.HandTracking, out origin, out direction)) // Hands
            {
                foundDevice = true;
            }

            if (!foundDevice)
            {
                isPointerActive = false;
                return;
            }

            if (!foundDevice)
            {
                // If force-disabled externally, stay disabled.
                // Otherwise, it just means no device found this frame.
                if (isPointerActive) 
                {
                    // check if we should auto-disable? 
                    // No, let's keep isPointerActive as the "intent" flag?
                    // actually HandleInput recalculates isPointerActive every frame based on device presence.
                    // We need a separate flag for "AllowPointer".
                }
                return;
            }

            // 1b. Update Pointer Start Position (Crucial Fix)
            pointerStart = origin;

            // 2. Perform Raycast (Logic)
            // We do this regardless of pointerAllowed to check if we are hovering UI
            bool hitSomethingUpdated = PerformRaycastLogic(origin, direction);

            // 3. Determine Final State
            if (pointerAllowed)
            {
                // Normal mode: Always active
                isPointerActive = true;
            }
            else
            {
                // Smart mode: Active only if hovering UI
                isPointerActive = hitSomethingUpdated && IsHoveringUI();
            }
            
            // 4. Update Interaction based on Final State
            if (isPointerActive)
            {
                HandleInteraction();
            }
            else
            {
                // Force clear hover if we became inactive
                if (currentHoveredObject != null) HandleHover(null);
            }
        }
        
        private bool IsHoveringUI()
        {
            return currentHoveredObject != null && 
                   (currentHoveredObject.GetComponent<Button>() != null || 
                    currentHoveredObject.GetComponent<Toggle>() != null ||
                    currentHoveredObject.GetComponent<Slider>() != null);
        }

        /// <summary>
        /// Explicitly enable/disable the pointer ray (e.g. for tasks that don't want it)
        /// </summary>
        public void SetPointerActive(bool active)
        {
            pointerAllowed = active;
            
            // If we are disabling, force state immediately
            if (!active)
            {
                isPointerActive = false;
                UpdateVisuals();
                if (currentHoveredObject != null) HandleHover(null);
            }
            
            Debug.Log($"[VRUIInputManager] Pointer allowed set to: {active}");
        }

        private bool TryGetDevice(InputDeviceCharacteristics characteristics, out Vector3 pos, out Vector3 dir)
        {
            pos = Vector3.zero;
            dir = Vector3.zero;
            
            cachedInputDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(characteristics, cachedInputDevices);

            foreach (var device in cachedInputDevices)
            {
                // IGNORE Headset
                if ((device.characteristics & InputDeviceCharacteristics.HeadMounted) != 0) continue;

                // Check for Position & Rotation
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 p) &&
                    device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion r))
                {
                    // If it's a controller, we might want to prioritize Right/Stylus over Left?
                    // For simplicity, we take the first valid one, but we could sort.
                    // Usually Right controller comes first or we can check characteristics.
                    
                    // Apply rotation offset
                    Quaternion rot = r * Quaternion.Euler(pointerRotationOffset);
                    
                    // Apply position offset (rotated by device rotation)
                    // If offset is local to the stylus (e.g. tip is forward), we rotate it
                    pos = p + (rot * pointerPositionOffset);
                    
                    dir = rot * Vector3.forward;
                    return true;
                }
            }
            return false;
        }

        private bool PerformRaycastLogic(Vector3 origin, Vector3 direction)
        {
            cachedPointerEventData.Reset();
            cachedPointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2);

            // UI Raycast
            cachedRaycastResults.Clear();
            if (Time.time >= nextRaycasterRefreshTime)
            {
                cachedRaycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
                nextRaycasterRefreshTime = Time.time + 1f;
            }
        
            if (cachedRaycasters != null)
            {
                foreach (var caster in cachedRaycasters)
                {
                    if (caster && caster.gameObject.activeInHierarchy)
                        caster.Raycast(cachedPointerEventData, cachedRaycastResults);
                }
            }

            // Physics Raycast
            Ray ray = new Ray(origin, direction);
            RaycastHit hit;
            bool hitPhysics = Physics.Raycast(ray, out hit, raycastDistance, uiLayerMask);

            // Determine Hit
            GameObject hitObj = null;
            Vector3 hitPoint = origin + direction * raycastDistance;
            bool didHit = false;

            // Check UI results first
            foreach (var result in cachedRaycastResults)
            {
                if (result.gameObject.GetComponent<Button>() || result.gameObject.GetComponent<Toggle>() || result.gameObject.GetComponent<Slider>())
                {
                    hitObj = result.gameObject;
                    hitPoint = result.worldPosition;
                    didHit = true;
                    break;
                }
            }

            // If no UI hit, check Physics (only if not strictly UI mode?) 
            // For now, let's allow physics hits too, but IsHoveringUI will filter later
            if (!didHit && hitPhysics)
            {
                var btn = hit.collider.GetComponent<Button>();
                if (btn)
                {
                    hitObj = hit.collider.gameObject;
                    hitPoint = hit.point;
                    didHit = true;
                }
            }

            pointerEnd = hitPoint;
            
            // Just update hover object, don't click yet
            HandleHover(hitObj);
            
            return didHit;
        }

        private void HandleInteraction()
        {
            GameObject hitObj = currentHoveredObject;
            bool didHit = hitObj != null;
            
            bool selectPressed = IsSelectPressed();
            if (didHit && selectPressed)
            {
                if (!wasSelectPressed) // Only click on down
                {
                    Debug.Log($"[VRUIInputManager] Clicked on {hitObj.name}");
                    OnPointerClick(hitObj);
                }
            }
            wasSelectPressed = selectPressed;
        }

        private bool wasSelectPressed = false;

        private void HandleHover(GameObject hitObj)
        {
            if (currentHoveredObject != hitObj)
            {
                if (currentHoveredObject != null) ExecuteEvents.Execute(currentHoveredObject, cachedPointerEventData, ExecuteEvents.pointerExitHandler);
                currentHoveredObject = hitObj;
                if (currentHoveredObject != null) ExecuteEvents.Execute(currentHoveredObject, cachedPointerEventData, ExecuteEvents.pointerEnterHandler);
            }
        }

        private void OnPointerClick(GameObject hitObj)
        {
            if (hitObj != null)
            {
                ExecuteEvents.Execute(hitObj, cachedPointerEventData, ExecuteEvents.pointerClickHandler);
                // Also try submit for buttons
                ExecuteEvents.Execute(hitObj, cachedPointerEventData, ExecuteEvents.submitHandler);
            }
        }

        private bool IsSelectPressed()
        {
            // Check all devices for Trigger/Select
            cachedInputDevices.Clear();
            InputDevices.GetDevices(cachedInputDevices);
            foreach (var device in cachedInputDevices)
            {
                if ((device.characteristics & InputDeviceCharacteristics.HeadMounted) != 0) continue;

                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool pressed) && pressed) return true;
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primary) && primary) return true;
            }
            return false;
        }

        private void UpdateVisuals()
        {
            if (pointerLine)
            {
                pointerLine.enabled = isPointerActive;
                if (isPointerActive)
                {
                    pointerLine.SetPosition(0, pointerStart);
                    pointerLine.SetPosition(1, pointerEnd);
                }
            }

            if (pointerDot)
            {
                pointerDot.SetActive(isPointerActive);
                if (isPointerActive)
                {
                    pointerDot.transform.position = pointerEnd;
                }
            }
        }
    }
}
