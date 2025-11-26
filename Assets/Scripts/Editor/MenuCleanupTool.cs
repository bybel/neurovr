using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuCleanupTool : EditorWindow
{
    [MenuItem("NeuroVR/Cleanup Duplicate UI")]
    public static void CleanupDuplicates()
    {
        HUDManager hud = Object.FindFirstObjectByType<HUDManager>();
        if (hud == null)
        {
            Debug.LogError("No HUDManager found in scene! Cannot determine which buttons are valid.");
            return;
        }

        SerializedObject so = new SerializedObject(hud);
        HashSet<GameObject> validObjects = new HashSet<GameObject>();

        // 1. Identify all objects (Buttons, Menus, Inputs) currently referenced by HUDManager
        SerializedProperty prop = so.GetIterator();
        while (prop.NextVisible(true))
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (prop.objectReferenceValue is GameObject go)
                    validObjects.Add(go);
                else if (prop.objectReferenceValue is Component comp)
                    validObjects.Add(comp.gameObject);
            }
        }

        Debug.Log($"[MenuCleanup] Found {validObjects.Count} valid referenced UI objects.");

        // 2. Scan the entire scene for Buttons that are NOT in the valid list
        Button[] allButtons = Object.FindObjectsOfType<Button>(true); // Include inactive
        int deletedCount = 0;

        foreach (var btn in allButtons)
        {
            // If this button is NOT referenced by HUDManager
            if (!validObjects.Contains(btn.gameObject))
            {
                // Check if it shares a name with a valid object (strong indicator it's a duplicate)
                foreach (var valid in validObjects)
                {
                    if (btn.name == valid.name)
                    {
                        Debug.LogWarning($"[MenuCleanup] Deleting duplicate: '{btn.name}' (Parent: {btn.transform.parent?.name ?? "root"})");
                        
                        // Support Undo so you can Ctrl+Z if it deletes something wrong
                        Undo.DestroyObjectImmediate(btn.gameObject);
                        deletedCount++;
                        break;
                    }
                }
            }
        }

        Debug.Log($"[MenuCleanup] Operation Complete. Deleted {deletedCount} duplicate buttons.");
    }
}
