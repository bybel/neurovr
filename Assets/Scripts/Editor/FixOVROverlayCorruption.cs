using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace NeuroReachVR.Editor
{
    /// <summary>
    /// Editor tool to fix OVROverlayCanvas corruption issues
    /// The magenta/purple rendering artifacts are caused by OVROverlayCanvas components
    /// </summary>
    public class FixOVROverlayCorruption : EditorWindow
    {
        [MenuItem("NeuroVR/Fix UI Corruption (Remove OVROverlayCanvas)")]
        public static void ShowWindow()
        {
            GetWindow<FixOVROverlayCorruption>("Fix UI Corruption");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "PROBLEM: Purple/Magenta rendering corruption and non-responsive UI\n\n" +
                "CAUSE: OVROverlayCanvas components are causing shader/rendering issues\n\n" +
                "SOLUTION: Remove all OVROverlayCanvas components and use standard Unity Canvas",
                MessageType.Warning
            );

            EditorGUILayout.Space(10);

            if (GUILayout.Button("🔧 FIX NOW - Remove All OVROverlayCanvas", GUILayout.Height(40)))
            {
                FixAllCanvases();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will:\n" +
                "1. Find all OVROverlayCanvas components in the scene\n" +
                "2. Remove them\n" +
                "3. Ensure proper Canvas and GraphicRaycaster components\n" +
                "4. Set correct render mode (WorldSpace) for VR\n" +
                "5. Fix shader issues",
                MessageType.Info
            );
        }

        private static void FixAllCanvases()
        {
            int fixedCount = 0;

            // Find all Canvas objects in the scene
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            foreach (Canvas canvas in allCanvases)
            {
                GameObject canvasObj = canvas.gameObject;

                // Remove OVROverlayCanvas if present
                Component ovrOverlay = canvasObj.GetComponent("OVROverlayCanvas");
                if (ovrOverlay != null)
                {
                    Debug.Log($"[FixOVROverlay] Removing OVROverlayCanvas from {canvasObj.name}");
                    DestroyImmediate(ovrOverlay);
                    fixedCount++;
                }

                // Ensure proper Canvas setup
                canvas.renderMode = RenderMode.WorldSpace;
                
                // Ensure GraphicRaycaster for interaction
                if (canvasObj.GetComponent<GraphicRaycaster>() == null)
                {
                    canvasObj.AddComponent<GraphicRaycaster>();
                    Debug.Log($"[FixOVROverlay] Added GraphicRaycaster to {canvasObj.name}");
                }

                // Fix materials on all UI graphics
                Graphic[] graphics = canvasObj.GetComponentsInChildren<Graphic>(true);
                foreach (Graphic graphic in graphics)
                {
                    if (graphic.material == null || graphic.material.shader.name.Contains("Hidden"))
                    {
                        // Reset to default UI material
                        graphic.material = null;
                        Debug.Log($"[FixOVROverlay] Reset material on {graphic.gameObject.name}");
                    }
                }

                // Set proper layer
                SetLayerRecursively(canvasObj, 5); // UI layer
            }

            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog(
                    "UI Corruption Fixed!",
                    $"Removed {fixedCount} OVROverlayCanvas component(s).\n\n" +
                    "The purple/magenta corruption should be gone.\n\n" +
                    "Save your scene and test in Play mode.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "No Issues Found",
                    "No OVROverlayCanvas components found.\n\n" +
                    "If you still see purple corruption, check:\n" +
                    "1. Missing shader references\n" +
                    "2. Missing material assets\n" +
                    "3. Project Settings > Graphics",
                    "OK"
                );
            }

            // Mark scene as dirty so Unity saves the changes
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
        }

        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, newLayer);
                }
            }
        }
    }
}
