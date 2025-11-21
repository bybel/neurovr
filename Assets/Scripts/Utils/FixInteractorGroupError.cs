using UnityEngine;

/// <summary>
/// Fixes NullReferenceException in Oculus InteractorGroup components
/// Run this once to disable problematic InteractorGroup components
/// </summary>
public class FixInteractorGroupError : MonoBehaviour
{
    [ContextMenu("Fix InteractorGroup Errors")]
    public void FixInteractorGroupErrors()
    {
        // Find all InteractorGroup components
        var interactorGroups = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        int fixedCount = 0;
        int disabledCount = 0;

        foreach (var obj in interactorGroups)
        {
            if (obj == null) continue;

            // Check if it's an InteractorGroup (using reflection or type name)
            string typeName = obj.GetType().Name;
            
            if (typeName.Contains("InteractorGroup") || 
                typeName.Contains("BestHoverInteractorGroup"))
            {
                Debug.Log($"[FixInteractorGroup] Found {typeName} on {obj.gameObject.name}");
                
                // Option 1: Disable the component
                if (obj is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                    disabledCount++;
                    Debug.Log($"[FixInteractorGroup] Disabled {typeName} on {obj.gameObject.name}");
                }
                
                // Option 2: Disable the GameObject if it's not essential
                // Uncomment if you want to disable the entire GameObject:
                // obj.gameObject.SetActive(false);
                // disabledCount++;
            }
        }

        Debug.Log($"[FixInteractorGroup] Fixed {fixedCount} components, Disabled {disabledCount} components");
    }

    private void Start()
    {
        // Auto-fix on start (optional - comment out if you don't want auto-fix)
        // FixInteractorGroupErrors();
    }
}

