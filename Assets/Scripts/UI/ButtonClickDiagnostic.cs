using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Diagnostic script to check why buttons aren't responding
/// Add this temporarily to HUDManager GameObject to debug
/// </summary>
public class ButtonClickDiagnostic : MonoBehaviour
{
    [Header("Test Buttons")]
    [SerializeField] private Button selectTaskButton;
    [SerializeField] private Button patientLoginButton;
    [SerializeField] private Button loginButton;

    private void Start()
    {
        Debug.Log("=== BUTTON CLICK DIAGNOSTIC ===");
        
        // Check EventSystem
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[DIAGNOSTIC] ❌ EventSystem NOT FOUND! Buttons won't work!");
        }
        else
        {
            Debug.Log($"[DIAGNOSTIC] ✓ EventSystem found: {eventSystem.name}");
            
            // Check StandaloneInputModule
            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                Debug.LogWarning("[DIAGNOSTIC] ⚠ StandaloneInputModule missing! Adding it...");
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
            else
            {
                Debug.Log($"[DIAGNOSTIC] ✓ StandaloneInputModule found");
            }
        }
        
        // Check buttons
        CheckButton("Select Task Button", selectTaskButton);
        CheckButton("Patient Login Button", patientLoginButton);
        CheckButton("Login Button", loginButton);
        
        // Test direct button click
        if (selectTaskButton != null)
        {
            Debug.Log("[DIAGNOSTIC] Testing direct button click...");
            selectTaskButton.onClick.AddListener(() => Debug.Log("[DIAGNOSTIC] ✓ Select Task Button clicked!"));
        }
    }
    
    private void CheckButton(string buttonName, Button button)
    {
        if (button == null)
        {
            Debug.LogError($"[DIAGNOSTIC] ❌ {buttonName} is NULL - Not assigned!");
            return;
        }
        
        Debug.Log($"[DIAGNOSTIC] ✓ {buttonName} found: {button.name}");
        
        if (!button.interactable)
        {
            Debug.LogWarning($"[DIAGNOSTIC] ⚠ {buttonName} is NOT INTERACTABLE!");
        }
        else
        {
            Debug.Log($"[DIAGNOSTIC] ✓ {buttonName} is interactable");
        }
        
        if (button.onClick == null)
        {
            Debug.LogError($"[DIAGNOSTIC] ❌ {buttonName} onClick is NULL!");
        }
        else
        {
            int listenerCount = button.onClick.GetPersistentEventCount();
            Debug.Log($"[DIAGNOSTIC] ✓ {buttonName} has {listenerCount} listener(s)");
        }
    }
    
    private void Update()
    {
        // Check for mouse clicks
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[DIAGNOSTIC] Mouse clicked at: {Input.mousePosition}");
        }
    }
}

