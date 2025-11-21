using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using NeuroReachVR.Core;
using NeuroReachVR.Input;
using NeuroReachVR.Tasks;
using NeuroReachVR.Data;
using NeuroReachVR.Feedback;
using NeuroReachVR.UI;

namespace NeuroReachVR.Editor
{
    /// <summary>
    /// Pre-deployment verification script
    /// Checks that all required components are assigned before building
    /// Run via: Window > NeuroReach VR > Verify Deployment Readiness
    /// </summary>
    public class PreDeploymentVerification : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool allChecksPassed = true;

        [MenuItem("Window/NeuroReach VR/Verify Deployment Readiness")]
        public static void ShowWindow()
        {
            GetWindow<PreDeploymentVerification>("Deployment Verification");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Pre-Deployment Verification", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            allChecksPassed = true;

            // Scene Configuration
            EditorGUILayout.LabelField("Scene Configuration", EditorStyles.boldLabel);
            CheckBuildSettings();
            EditorGUILayout.Space();

            // Component Verification
            EditorGUILayout.LabelField("Component Assignment", EditorStyles.boldLabel);
            CheckGameManager();
            CheckInputHandler();
            CheckHUDManager();
            CheckTasks();
            CheckVRUIInputManager();
            EditorGUILayout.Space();

            // Build Configuration
            EditorGUILayout.LabelField("Build Configuration", EditorStyles.boldLabel);
            CheckPackageName();
            CheckVersion();
            CheckScriptingBackend();
            CheckAPICompatibility();
            CheckColorSpace();
            EditorGUILayout.Space();

            // Summary
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            if (allChecksPassed)
            {
                EditorGUILayout.HelpBox("✓ All checks passed! Ready for deployment.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("✗ Some checks failed. Please fix the issues above before building.", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        private void CheckBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            bool scene1Found = false;

            foreach (var scene in scenes)
            {
                if (scene.path.Contains("scene1.unity") && scene.enabled)
                {
                    scene1Found = true;
                    break;
                }
            }

            if (scene1Found)
            {
                EditorGUILayout.HelpBox("✓ scene1.unity is in build settings and enabled", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("✗ scene1.unity not found in build settings or not enabled", MessageType.Error);
                allChecksPassed = false;
            }
        }

        private void CheckGameManager()
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                EditorGUILayout.HelpBox("✗ GameManager not found in scene", MessageType.Error);
                allChecksPassed = false;
                return;
            }

            EditorGUILayout.LabelField("GameManager:", EditorStyles.miniLabel);
            CheckField(gameManager, "Input Handler", "inputHandler");
            CheckField(gameManager, "Adaptive Difficulty Controller", "adaptiveController");
            CheckField(gameManager, "Data Logger", "dataLogger");
            CheckField(gameManager, "Kinematic Data Collector", "kinematicCollector");
            CheckField(gameManager, "Multimodal Feedback", "feedback");
            CheckField(gameManager, "Balloon Pop Task", "balloonTask");
            CheckField(gameManager, "Path Tracing Task", "pathTask");
            CheckField(gameManager, "Spiral Tracing Task", "spiralTask");
            CheckField(gameManager, "HUD Manager", "hudManager");
        }

        private void CheckInputHandler()
        {
            var inputHandler = FindFirstObjectByType<InputHandler>();
            if (inputHandler == null)
            {
                EditorGUILayout.HelpBox("✗ InputHandler not found in scene", MessageType.Error);
                allChecksPassed = false;
                return;
            }

            EditorGUILayout.LabelField("InputHandler:", EditorStyles.miniLabel);
            var so = new SerializedObject(inputHandler);
            CheckField(so, "Left Hand XR", "leftHandXR");
            CheckField(so, "Right Hand XR", "rightHandXR");
            CheckField(so, "Stylus Input Manager", "stylus");
            
            var useXRHands = so.FindProperty("useXRHandsPackage");
            if (useXRHands != null && useXRHands.boolValue)
            {
                EditorGUILayout.HelpBox("  ✓ useXRHandsPackage is enabled", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("  ⚠ useXRHandsPackage is disabled (consider enabling)", MessageType.Warning);
            }
        }

        private void CheckHUDManager()
        {
            var hudManager = FindFirstObjectByType<HUDManager>();
            if (hudManager == null)
            {
                EditorGUILayout.HelpBox("✗ HUDManager not found in scene", MessageType.Error);
                allChecksPassed = false;
                return;
            }

            EditorGUILayout.LabelField("HUDManager:", EditorStyles.miniLabel);
            var so = new SerializedObject(hudManager);
            CheckField(so, "Main Menu", "mainMenu");
            CheckField(so, "Patient Login Menu", "patientLoginMenu");
            CheckField(so, "Select Task Menu", "selectTaskMenu");
            CheckField(so, "Difficulty Menu", "difficultyMenu");
            CheckField(so, "Start Trial Menu", "startTrialMenu");
            CheckField(so, "Select Task Button", "selectTaskButton");
            CheckField(so, "Patient Login Button", "patientLoginButton");
            CheckField(so, "Quit Button", "quitButton");
        }

        private void CheckTasks()
        {
            EditorGUILayout.LabelField("Tasks:", EditorStyles.miniLabel);
            
            var balloonTask = FindFirstObjectByType<BalloonPopTask>();
            if (balloonTask != null)
            {
                var so = new SerializedObject(balloonTask);
                CheckField(so, "Balloon Pop - Balloon Prefab", "balloonPrefab");
                CheckField(so, "Balloon Pop - Input Handler", "inputHandler");
            }
            else
            {
                EditorGUILayout.HelpBox("  ⚠ BalloonPopTask not found", MessageType.Warning);
            }

            var pathTask = FindFirstObjectByType<PathTracingTask>();
            if (pathTask != null)
            {
                var so = new SerializedObject(pathTask);
                CheckField(so, "Path Tracing - Path Prefab", "pathPrefab");
                CheckField(so, "Path Tracing - Input Handler", "inputHandler");
            }
            else
            {
                EditorGUILayout.HelpBox("  ⚠ PathTracingTask not found", MessageType.Warning);
            }

            var spiralTask = FindFirstObjectByType<SpiralTracingTask>();
            if (spiralTask != null)
            {
                var so = new SerializedObject(spiralTask);
                CheckField(so, "Spiral Tracing - Path Prefab", "pathPrefab");
                CheckField(so, "Spiral Tracing - Input Handler", "inputHandler");
            }
            else
            {
                EditorGUILayout.HelpBox("  ⚠ SpiralTracingTask not found", MessageType.Warning);
            }
        }

        private void CheckVRUIInputManager()
        {
            var vrUI = FindFirstObjectByType<VRUIInputManager>();
            if (vrUI == null)
            {
                EditorGUILayout.HelpBox("⚠ VRUIInputManager not found (VR button interaction may not work)", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("✓ VRUIInputManager found", MessageType.Info);
            }
        }

        private void CheckPackageName()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
#pragma warning restore CS0618
            if (packageName.Contains("template") || packageName.Contains("UnityTechnologies"))
            {
                EditorGUILayout.HelpBox($"✗ Package Name still uses template: {packageName}", MessageType.Error);
                allChecksPassed = false;
            }
            else if (packageName == "com.elsiga.neuroreachvr")
            {
                EditorGUILayout.HelpBox($"✓ Package Name: {packageName}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠ Package Name: {packageName} (verify it's correct)", MessageType.Warning);
            }
        }

        private void CheckVersion()
        {
            var version = PlayerSettings.bundleVersion;
            if (string.IsNullOrEmpty(version) || version == "0.1")
            {
                EditorGUILayout.HelpBox($"⚠ Version: {version} (consider setting to 1.0.0)", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"✓ Version: {version}", MessageType.Info);
            }
        }

        private void CheckScriptingBackend()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
#pragma warning restore CS0618
            if (backend == ScriptingImplementation.IL2CPP)
            {
                EditorGUILayout.HelpBox("✓ Scripting Backend: IL2CPP", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"✗ Scripting Backend: {backend} (should be IL2CPP)", MessageType.Error);
                allChecksPassed = false;
            }
        }

        private void CheckAPICompatibility()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var apiLevel = PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android);
#pragma warning restore CS0618
            string apiLevelName = apiLevel.ToString();

            // Check if using .NET Standard 2.0, 2.1, or 4.x
            if (apiLevelName.Contains("NET_Standard") || apiLevelName.Contains("NET_4") || apiLevelName.Contains("NET_Unity"))
            {
                EditorGUILayout.HelpBox($"✓ API Compatibility: {apiLevelName}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠ API Compatibility: {apiLevelName} (recommended: .NET Standard 2.0 or higher)", MessageType.Warning);
            }
        }

        private void CheckColorSpace()
        {
            var colorSpace = PlayerSettings.colorSpace;
            if (colorSpace == ColorSpace.Linear)
            {
                EditorGUILayout.HelpBox("✓ Color Space: Linear (recommended for VR)", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠ Color Space: {colorSpace} (recommended: Linear for VR)", MessageType.Warning);
            }
        }

        private void CheckField(Component component, string displayName, string fieldName)
        {
            var so = new SerializedObject(component);
            CheckField(so, displayName, fieldName);
        }

        private void CheckField(SerializedObject so, string displayName, string fieldName)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                EditorGUILayout.HelpBox($"  ⚠ {displayName}: Property '{fieldName}' not found", MessageType.Warning);
                return;
            }

            bool isValid = false;
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                isValid = prop.objectReferenceValue != null;
            }
            else if (prop.propertyType == SerializedPropertyType.String)
            {
                isValid = !string.IsNullOrEmpty(prop.stringValue);
            }
            else if (prop.propertyType == SerializedPropertyType.Boolean)
            {
                isValid = true; // Booleans always have a value
            }

            if (isValid)
            {
                EditorGUILayout.HelpBox($"  ✓ {displayName}: Assigned", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"  ✗ {displayName}: NOT ASSIGNED", MessageType.Error);
                allChecksPassed = false;
            }
        }
    }
}

