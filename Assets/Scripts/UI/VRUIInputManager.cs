using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// VR UI Input Manager - Enables VR controller/trackpad interaction with UI buttons
    /// Uses XR Ray Interactor pattern compatible with Meta XR SDK
    /// </summary>
    public class VRUIInputManager : MonoBehaviour
    {
        [Header("VR Input Settings")]
        [SerializeField] private float raycastDistance = 10f;
        [SerializeField] private LayerMask uiLayerMask = -1;
        [SerializeField] private bool enableHandTracking = true;
        [SerializeField] private bool enableControllerTracking = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private LineRenderer pointerLine;
        [SerializeField] private GameObject pointerDot;
        [SerializeField] private Color pointerColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        
        private Camera mainCamera;
        private EventSystem eventSystem;
        private GraphicRaycaster graphicRaycaster;
        private Canvas worldCanvas;
        
        private bool isPointerActive;
        private Vector3 pointerStart;
        private Vector3 pointerEnd;
        private GameObject currentHoveredObject;
        
        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            
            eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject es = new GameObject("EventSystem");
                eventSystem = es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>(); // Use new Input System module
            }
            else
            {
                // Remove old StandaloneInputModule if present
                var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (oldModule != null)
                {
                    Destroy(oldModule);
                    if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                        eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
            
            // Find world space canvas
            worldCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (worldCanvas != null)
            {
                graphicRaycaster = worldCanvas.GetComponent<GraphicRaycaster>();
                if (graphicRaycaster == null)
                    graphicRaycaster = worldCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            
            SetupPointerVisual();
        }
        
        private void SetupPointerVisual()
        {
            if (pointerLine == null)
            {
                GameObject lineObj = new GameObject("VRPointerLine");
                lineObj.transform.SetParent(transform);
                pointerLine = lineObj.AddComponent<LineRenderer>();
                pointerLine.material = new Material(Shader.Find("Sprites/Default"));
                pointerLine.startColor = pointerColor;
                pointerLine.endColor = pointerColor;
                pointerLine.startWidth = 0.01f;
                pointerLine.endWidth = 0.005f;
                pointerLine.positionCount = 2;
                pointerLine.enabled = false;
            }
            
            if (pointerDot == null)
            {
                pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointerDot.name = "VRPointerDot";
                pointerDot.transform.SetParent(transform);
                pointerDot.transform.localScale = Vector3.one * 0.02f;
                pointerDot.GetComponent<Renderer>().material.color = pointerColor;
                pointerDot.SetActive(false);
                
                // Remove collider - it's just visual
                Destroy(pointerDot.GetComponent<Collider>());
            }
        }
        
        private void Update()
        {
            HandleVRInput();
            UpdatePointerVisual();
        }
        
        private void HandleVRInput()
        {
            Vector3 rayOrigin = GetRayOrigin();
            Vector3 rayDirection = GetRayDirection();
            
            if (rayDirection == Vector3.zero)
            {
                isPointerActive = false;
                return;
            }
            
            isPointerActive = true;
            pointerStart = rayOrigin;
            
            // Raycast against UI
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = new Vector2(Screen.width / 2, Screen.height / 2); // Center of screen
            
            var results = new System.Collections.Generic.List<RaycastResult>();
            if (graphicRaycaster != null)
            {
                graphicRaycaster.Raycast(pointerData, results);
            }
            
            // Also do physics raycast for world space UI
            Ray ray = new Ray(rayOrigin, rayDirection);
            RaycastHit hit;
            
            bool hitUI = false;
            GameObject hitObject = null;
            
            if (Physics.Raycast(ray, out hit, raycastDistance, uiLayerMask))
            {
                hitObject = hit.collider.gameObject;
                Button button = hitObject.GetComponent<Button>();
                if (button != null)
                {
                    hitUI = true;
                    pointerEnd = hit.point;
                }
            }
            
            // Check UI raycast results
            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    Button button = result.gameObject.GetComponent<Button>();
                    if (button != null)
                    {
                        hitUI = true;
                        hitObject = result.gameObject;
                        pointerEnd = result.worldPosition;
                        break;
                    }
                }
            }
            
            if (!hitUI)
            {
                pointerEnd = rayOrigin + rayDirection * raycastDistance;
            }
            
            // Handle hover
            if (hitObject != currentHoveredObject)
            {
                if (currentHoveredObject != null)
                {
                    OnPointerExit(currentHoveredObject);
                }
                
                currentHoveredObject = hitObject;
                
                if (currentHoveredObject != null)
                {
                    OnPointerEnter(currentHoveredObject);
                }
            }
            
            // Handle click/select
            if (hitUI && IsSelectPressed())
            {
                OnPointerClick(hitObject);
            }
        }
        
        private Vector3 GetRayOrigin()
        {
            // Try to get controller/hand position
            if (enableControllerTracking)
            {
                Vector3 controllerPos;
                if (TryGetControllerPosition(out controllerPos))
                    return controllerPos;
            }
            
            if (enableHandTracking)
            {
                Vector3 handPos;
                if (TryGetHandPosition(out handPos))
                    return handPos;
            }
            
            // Fallback to camera
            return mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        }
        
        private Vector3 GetRayDirection()
        {
            // Try to get controller/hand forward
            if (enableControllerTracking)
            {
                Quaternion controllerRot;
                if (TryGetControllerRotation(out controllerRot))
                    return controllerRot * Vector3.forward;
            }
            
            if (enableHandTracking)
            {
                Quaternion handRot;
                if (TryGetHandRotation(out handRot))
                    return handRot * Vector3.forward;
            }
            
            // Fallback to camera forward
            return mainCamera != null ? mainCamera.transform.forward : Vector3.forward;
        }
        
        private bool TryGetControllerPosition(out Vector3 position)
        {
            position = Vector3.zero;
            
            var inputDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, inputDevices);
            
            if (inputDevices.Count > 0)
            {
                if (inputDevices[0].TryGetFeatureValue(CommonUsages.devicePosition, out position))
                    return true;
            }
            
            return false;
        }
        
        private bool TryGetControllerRotation(out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            
            var inputDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, inputDevices);
            
            if (inputDevices.Count > 0)
            {
                if (inputDevices[0].TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
                    return true;
            }
            
            return false;
        }
        
        private bool TryGetHandPosition(out Vector3 position)
        {
            position = Vector3.zero;
            
            var inputDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right, inputDevices);
            
            if (inputDevices.Count > 0)
            {
                if (inputDevices[0].TryGetFeatureValue(CommonUsages.devicePosition, out position))
                    return true;
            }
            
            return false;
        }
        
        private bool TryGetHandRotation(out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            
            var inputDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right, inputDevices);
            
            if (inputDevices.Count > 0)
            {
                if (inputDevices[0].TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
                    return true;
            }
            
            return false;
        }
        
        private bool IsSelectPressed()
        {
            // Check for trigger/select button press
            var inputDevices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, inputDevices);
            
            foreach (var device in inputDevices)
            {
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                    return true;
                
                if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
                    return true;
            }
            
            // Check for hand pinch
            if (enableHandTracking)
            {
                // Simplified - would need actual hand tracking API
                // For now, use trigger as proxy
            }
            
            return false;
        }
        
        private void UpdatePointerVisual()
        {
            if (pointerLine != null)
            {
                pointerLine.enabled = isPointerActive;
                if (isPointerActive)
                {
                    pointerLine.SetPosition(0, pointerStart);
                    pointerLine.SetPosition(1, pointerEnd);
                }
            }
            
            if (pointerDot != null)
            {
                pointerDot.SetActive(isPointerActive);
                if (isPointerActive)
                {
                    pointerDot.transform.position = pointerEnd;
                }
            }
        }
        
        private void OnPointerEnter(GameObject obj)
        {
            Button button = obj.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                // Trigger hover state
                ExecuteEvents.Execute(obj, new PointerEventData(eventSystem), ExecuteEvents.pointerEnterHandler);
            }
        }
        
        private void OnPointerExit(GameObject obj)
        {
            Button button = obj.GetComponent<Button>();
            if (button != null)
            {
                ExecuteEvents.Execute(obj, new PointerEventData(eventSystem), ExecuteEvents.pointerExitHandler);
            }
        }
        
        private void OnPointerClick(GameObject obj)
        {
            Button button = obj.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                
                // Also trigger pointer click event
                ExecuteEvents.Execute(obj, new PointerEventData(eventSystem), ExecuteEvents.pointerClickHandler);
            }
        }
    }
}

