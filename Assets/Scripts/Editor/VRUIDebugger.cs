using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;
using System.Collections.Generic;
using System.Text;

namespace NeuroReachVR.Editor
{
    /// <summary>
    /// Comprehensive VR UI diagnostic and auto-fix tool
    /// Identifies and fixes common VR button interaction issues
    /// </summary>
    public class VRUIDebugger : EditorWindow
    {
        private Vector2 scrollPosition;
        private StringBuilder report;
        private int issuesFound = 0;
        private int issuesFixed = 0;

        [MenuItem("NeuroReachVR/Debug VR UI/Run Full Diagnostic")]
        public static void ShowWindow()
        {
            var window = GetWindow<VRUIDebugger>("VR UI Debugger");
            window.minSize = new Vector2(600, 500);
            window.RunDiagnostic();
        }

        [MenuItem("NeuroReachVR/Debug VR UI/Quick Fix All Issues")]
        public static void QuickFixAll()
        {
            var window = GetWindow<VRUIDebugger>("VR UI Debugger");
            window.RunDiagnostic();
            window.AutoFixAllIssues();
        }

        [MenuItem("NeuroReachVR/Debug VR UI/Enable Debug Logging")]
        public static void EnableDebugLogging()
        {
            // Add VR input debug component at runtime
            EditorUtility.DisplayDialog(
                "Debug Logging",
                "When you enter Play Mode, VR input events will be logged to console.\n\n" +
                "Look for:\n" +
                "• [VRUIInputManager] messages\n" +
                "• [HUDManager] button click messages\n" +
                "• Button component onClick events",
                "OK"
            );
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("VR UI Diagnostic Report", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Run Diagnostic", GUILayout.Height(30)))
            {
                RunDiagnostic();
            }

            if (issuesFound > 0)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Auto-Fix All Issues", GUILayout.Height(30)))
                {
                    AutoFixAllIssues();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Issues Found: {issuesFound}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Issues Fixed: {issuesFixed}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (report != null)
            {
                EditorGUILayout.TextArea(report.ToString(), EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunDiagnostic()
        {
            report = new StringBuilder();
            issuesFound = 0;
            issuesFixed = 0;

            report.AppendLine("=== VR UI DIAGNOSTIC REPORT ===");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine();

            CheckEventSystem();
            CheckVRUIInputManager();
            CheckCanvas();
            CheckButtons();
            CheckVRDevices();
            CheckInputSystemConfiguration();

            report.AppendLine();
            report.AppendLine("=== DIAGNOSTIC COMPLETE ===");
            report.AppendLine($"Total Issues Found: {issuesFound}");

            Repaint();
        }

        private void CheckEventSystem()
        {
            report.AppendLine("--- 1. EventSystem Check ---");

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                report.AppendLine("✗ ISSUE: No EventSystem found in scene!");
                report.AppendLine("  Fix: EventSystem is required for UI interaction");
                issuesFound++;
                return;
            }

            report.AppendLine("✓ EventSystem exists");

            // Check for old StandaloneInputModule
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null)
            {
                report.AppendLine("✗ ISSUE: EventSystem using OLD StandaloneInputModule");
                report.AppendLine("  This is incompatible with new Input System!");
                report.AppendLine("  Fix: Replace with InputSystemUIInputModule");
                issuesFound++;
            }

            // Check for new InputSystemUIInputModule
            var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                report.AppendLine("✗ ISSUE: InputSystemUIInputModule missing");
                report.AppendLine("  Fix: Add InputSystemUIInputModule to EventSystem");
                issuesFound++;
            }
            else
            {
                report.AppendLine("✓ InputSystemUIInputModule configured correctly");
            }

            report.AppendLine();
        }

        private void CheckVRUIInputManager()
        {
            report.AppendLine("--- 2. VRUIInputManager Check ---");

            var vrUIManager = Object.FindFirstObjectByType<UI.VRUIInputManager>();
            if (vrUIManager == null)
            {
                report.AppendLine("✗ CRITICAL ISSUE: VRUIInputManager not found in scene!");
                report.AppendLine("  VR button interaction WILL NOT WORK without this component!");
                report.AppendLine("  Fix: Add VRUIInputManager to scene (e.g., on Main Camera or XR Rig)");
                issuesFound++;
            }
            else
            {
                report.AppendLine("✓ VRUIInputManager exists");

                var so = new SerializedObject(vrUIManager);
                var raycastDistance = so.FindProperty("raycastDistance");
                var enableHandTracking = so.FindProperty("enableHandTracking");
                var enableControllerTracking = so.FindProperty("enableControllerTracking");

                if (raycastDistance != null)
                {
                    report.AppendLine($"  • Raycast Distance: {raycastDistance.floatValue}m");
                    if (raycastDistance.floatValue < 2f)
                    {
                        report.AppendLine("    ⚠ Warning: Raycast distance might be too short for UI interaction");
                        issuesFound++;
                    }
                }

                if (enableHandTracking != null && enableControllerTracking != null)
                {
                    report.AppendLine($"  • Hand Tracking: {(enableHandTracking.boolValue ? "Enabled" : "Disabled")}");
                    report.AppendLine($"  • Controller Tracking: {(enableControllerTracking.boolValue ? "Enabled" : "Disabled")}");

                    if (!enableHandTracking.boolValue && !enableControllerTracking.boolValue)
                    {
                        report.AppendLine("  ✗ ISSUE: Both hand and controller tracking disabled!");
                        issuesFound++;
                    }
                }
            }

            report.AppendLine();
        }

        private void CheckCanvas()
        {
            report.AppendLine("--- 3. Canvas Check ---");

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            if (canvases.Length == 0)
            {
                report.AppendLine("✗ ISSUE: No Canvas found in scene!");
                issuesFound++;
                return;
            }

            report.AppendLine($"Found {canvases.Length} Canvas(es)");

            foreach (Canvas canvas in canvases)
            {
                report.AppendLine($"\nCanvas: {canvas.gameObject.name}");

                // Check render mode
                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    report.AppendLine($"  ✗ ISSUE: Render Mode is {canvas.renderMode}");
                    report.AppendLine("    VR UI requires World Space render mode!");
                    issuesFound++;
                }
                else
                {
                    report.AppendLine("  ✓ Render Mode: World Space");
                }

                // Check GraphicRaycaster
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    report.AppendLine("  ✗ ISSUE: GraphicRaycaster component missing!");
                    report.AppendLine("    Required for VR UI interaction");
                    issuesFound++;
                }
                else
                {
                    report.AppendLine("  ✓ GraphicRaycaster present");
                }

                // Check Canvas distance from camera
                Camera mainCam = Camera.main;
                if (mainCam != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    float distance = Vector3.Distance(canvas.transform.position, mainCam.transform.position);
                    report.AppendLine($"  • Distance from camera: {distance:F2}m");

                    if (distance > 10f)
                    {
                        report.AppendLine("    ⚠ Warning: Canvas very far from camera (might be hard to reach)");
                    }
                    else if (distance < 0.5f)
                    {
                        report.AppendLine("    ⚠ Warning: Canvas very close to camera (might clip)");
                    }
                }
            }

            report.AppendLine();
        }

        private void CheckButtons()
        {
            report.AppendLine("--- 4. Button Check ---");

            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            if (buttons.Length == 0)
            {
                report.AppendLine("✗ ISSUE: No buttons found in scene!");
                issuesFound++;
                return;
            }

            report.AppendLine($"Found {buttons.Length} button(s)");

            int buttonIssues = 0;
            foreach (Button button in buttons)
            {
                bool buttonHasIssue = false;

                // Check if button is interactable
                if (!button.interactable)
                {
                    report.AppendLine($"\n  Button: {button.gameObject.name}");
                    report.AppendLine("    ⚠ Not interactable (disabled)");
                    buttonHasIssue = true;
                }

                // Check for collider (needed for Physics.Raycast in VRUIInputManager)
                var collider = button.GetComponent<Collider>();
                if (collider == null)
                {
                    if (!buttonHasIssue)
                    {
                        report.AppendLine($"\n  Button: {button.gameObject.name}");
                    }
                    report.AppendLine("    ✗ ISSUE: No Collider component!");
                    report.AppendLine("      VRUIInputManager uses Physics.Raycast which requires colliders");
                    buttonIssues++;
                    buttonHasIssue = true;
                }

                // Check onClick listeners
                if (button.onClick.GetPersistentEventCount() == 0)
                {
                    if (!buttonHasIssue)
                    {
                        report.AppendLine($"\n  Button: {button.gameObject.name}");
                    }
                    report.AppendLine("    ⚠ Warning: No persistent onClick listeners");
                    report.AppendLine("      (Runtime listeners might be added in code)");
                }
            }

            if (buttonIssues > 0)
            {
                report.AppendLine($"\n  Total button issues: {buttonIssues}");
                issuesFound += buttonIssues;
            }
            else
            {
                report.AppendLine("\n  ✓ All buttons appear correctly configured");
            }

            report.AppendLine();
        }

        private void CheckVRDevices()
        {
            report.AppendLine("--- 5. VR Device Detection (Editor Simulation) ---");

            // Note: In editor, VR devices may not be detected
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevices(inputDevices);

            report.AppendLine($"Detected {inputDevices.Count} input device(s) in editor");

            if (inputDevices.Count == 0)
            {
                report.AppendLine("  ⚠ No VR devices detected (expected in Editor)");
                report.AppendLine("    Make sure to test in VR headset!");
                report.AppendLine("    VR controllers/hands will be detected at runtime on device");
            }
            else
            {
                foreach (var device in inputDevices)
                {
                    report.AppendLine($"  • {device.name} - {device.characteristics}");
                }
            }

            // Check if XR is enabled
            bool xrEnabled = XRSettings.enabled;
            report.AppendLine($"\n  XR Settings Enabled: {xrEnabled}");

            if (!xrEnabled)
            {
                report.AppendLine("    ⚠ XR not enabled in editor (normal - will be enabled on device)");
            }

            report.AppendLine();
        }

        private void CheckInputSystemConfiguration()
        {
            report.AppendLine("--- 6. Input System Configuration ---");

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            report.AppendLine("✓ Input System Package (New) is active");
            report.AppendLine("  This is correct for VR UI interaction!");
#elif ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            report.AppendLine("⚠ Both Input System modes active");
            report.AppendLine("  Recommended: Use 'Input System Package (New)' only");
            report.AppendLine("  Location: Edit → Project Settings → Player → Other Settings → Active Input Handling");
#else
            report.AppendLine("✗ ISSUE: Legacy Input Manager active");
            report.AppendLine("  VR UI requires new Input System Package!");
            report.AppendLine("  Fix: Edit → Project Settings → Player → Other Settings");
            report.AppendLine("       Set 'Active Input Handling' to 'Input System Package (New)'");
            issuesFound++;
#endif

            report.AppendLine();
        }

        private void AutoFixAllIssues()
        {
            issuesFixed = 0;
            report.AppendLine("\n=== AUTO-FIX APPLIED ===\n");

            FixEventSystem();
            FixCanvas();
            FixButtons();

            report.AppendLine($"\nTotal Issues Fixed: {issuesFixed}");
            report.AppendLine("\n⚠ Some issues require manual action:");
            report.AppendLine("  • Add VRUIInputManager to scene if missing");
            report.AppendLine("  • Test in VR headset to verify controller/hand tracking");
            report.AppendLine("  • Check Project Settings if Input System issues remain");

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Repaint();
        }

        private void FixEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                GameObject es = new GameObject("EventSystem");
                eventSystem = es.AddComponent<EventSystem>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
                report.AppendLine("✓ Created EventSystem");
                issuesFixed++;
            }

            // Remove old module
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null)
            {
                Undo.DestroyObjectImmediate(standaloneModule);
                report.AppendLine("✓ Removed StandaloneInputModule");
                issuesFixed++;
            }

            // Add new module
            var inputSystemModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                Undo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(eventSystem.gameObject);
                report.AppendLine("✓ Added InputSystemUIInputModule");
                issuesFixed++;
            }
        }

        private void FixCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            foreach (Canvas canvas in canvases)
            {
                bool canvasFixed = false;

                // Fix render mode
                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    Undo.RecordObject(canvas, "Fix Canvas Render Mode");
                    canvas.renderMode = RenderMode.WorldSpace;

                    // Set reasonable default transform
                    if (Camera.main != null)
                    {
                        canvas.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2f;
                        canvas.transform.LookAt(Camera.main.transform);
                        canvas.transform.Rotate(0, 180, 0); // Face the camera
                    }

                    report.AppendLine($"✓ Fixed Canvas '{canvas.gameObject.name}' render mode to World Space");
                    canvasFixed = true;
                    issuesFixed++;
                }

                // Add GraphicRaycaster
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
                    report.AppendLine($"✓ Added GraphicRaycaster to Canvas '{canvas.gameObject.name}'");
                    canvasFixed = true;
                    issuesFixed++;
                }

                if (canvasFixed)
                {
                    EditorUtility.SetDirty(canvas);
                }
            }
        }

        private void FixButtons()
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            int buttonsFixed = 0;

            foreach (Button button in buttons)
            {
                // Add BoxCollider if missing
                var collider = button.GetComponent<Collider>();
                if (collider == null)
                {
                    var boxCollider = Undo.AddComponent<BoxCollider>(button.gameObject);

                    // Size collider to match RectTransform
                    RectTransform rectTransform = button.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        boxCollider.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 1f);
                    }

                    buttonsFixed++;
                }
            }

            if (buttonsFixed > 0)
            {
                report.AppendLine($"✓ Added colliders to {buttonsFixed} button(s)");
                issuesFixed += buttonsFixed;
            }
        }
    }
}
