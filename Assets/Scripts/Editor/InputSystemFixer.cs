using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

namespace NeuroReachVR.Editor
{
    /// <summary>
    /// Fixes Input System compatibility issues
    /// Replaces old StandaloneInputModule with new InputSystemUIInputModule
    /// </summary>
    public static class InputSystemFixer
    {
        [MenuItem("NeuroReachVR/Fix Input System/Replace EventSystem Input Module")]
        public static void FixEventSystemInputModule()
        {
            // Find EventSystem in scene
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                EditorUtility.DisplayDialog(
                    "EventSystem Not Found",
                    "No EventSystem found in the scene.\n\n" +
                    "Create one: GameObject → UI → Event System",
                    "OK"
                );
                return;
            }

            // Check if it has StandaloneInputModule
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();

            if (standaloneModule != null)
            {
                // Remove old module
                Undo.DestroyObjectImmediate(standaloneModule);
                Debug.Log("[Input System Fixer] Removed StandaloneInputModule");
            }

            // Check if InputSystemUIInputModule already exists
            var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            if (inputSystemModule == null)
            {
                // Add new Input System module
                Undo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(eventSystem.gameObject);
                Debug.Log("[Input System Fixer] Added InputSystemUIInputModule");

                EditorUtility.DisplayDialog(
                    "Fixed!",
                    "EventSystem has been updated to use the new Input System.\n\n" +
                    "✓ Removed: StandaloneInputModule\n" +
                    "✓ Added: InputSystemUIInputModule\n\n" +
                    "UI input should now work correctly!",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Already Fixed",
                    "EventSystem is already using InputSystemUIInputModule.\n\n" +
                    "No changes needed!",
                    "OK"
                );
            }

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }

        [MenuItem("NeuroReachVR/Fix Input System/Check EventSystem Status")]
        public static void CheckEventSystemStatus()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                EditorUtility.DisplayDialog(
                    "No EventSystem",
                    "No EventSystem found in scene.\n\n" +
                    "Create one: GameObject → UI → Event System",
                    "OK"
                );
                return;
            }

            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            string status = "EventSystem Status:\n\n";

            if (standaloneModule != null)
            {
                status += "✗ Using OLD StandaloneInputModule (INCOMPATIBLE)\n";
                status += "  This will cause Input System errors!\n\n";
                status += "Fix: NeuroReachVR → Fix Input System → Replace EventSystem Input Module";
            }
            else if (inputSystemModule != null)
            {
                status += "✓ Using NEW InputSystemUIInputModule (CORRECT)\n";
                status += "  Input System configured correctly!";
            }
            else
            {
                status += "⚠ No Input Module found\n";
                status += "  Add InputSystemUIInputModule component";
            }

            EditorUtility.DisplayDialog("EventSystem Status", status, "OK");
        }

        [MenuItem("NeuroReachVR/Fix Input System/Set Player Settings to Input System")]
        public static void SetPlayerSettingsToInputSystem()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            EditorUtility.DisplayDialog(
                "Already Configured",
                "Player Settings already set to:\n" +
                "Active Input Handling: Input System Package (New)\n\n" +
                "This is correct for your project!",
                "OK"
            );
#else
            bool confirm = EditorUtility.DisplayDialog(
                "Change Input Handling?",
                "Current setting uses Legacy Input or Both.\n\n" +
                "Change to Input System Package (New)?\n\n" +
                "Note: This requires Unity restart.",
                "Yes, Change",
                "Cancel"
            );

            if (confirm)
            {
                // This will require restart
                EditorUtility.DisplayDialog(
                    "Manual Configuration Required",
                    "Please configure manually:\n\n" +
                    "1. Edit → Project Settings → Player\n" +
                    "2. Other Settings → Configuration\n" +
                    "3. Active Input Handling: Input System Package (New)\n" +
                    "4. Restart Unity when prompted",
                    "OK"
                );
            }
#endif
        }
    }
}
