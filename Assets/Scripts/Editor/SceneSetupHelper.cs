using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using NeuroReachVR.Core;
using NeuroReachVR.Input;
using NeuroReachVR.Data;
using NeuroReachVR.Feedback;

namespace NeuroReachVR.Editor
{
    /// <summary>
    /// Helper tool to quickly set up scene with all required components
    /// </summary>
    public class SceneSetupHelper : EditorWindow
    {
        private bool showCreateOptions = false;

        [MenuItem("NeuroReachVR/Scene Setup Helper")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneSetupHelper>("Scene Setup Helper");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("NeuroReach VR - Scene Setup Helper", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool helps verify and create missing core components in your scene.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Check Status
            EditorGUILayout.LabelField("Scene Component Status:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawComponentStatus<GameManager>("GameManager");
            DrawComponentStatus<InputHandler>("InputHandler");
            DrawComponentStatus<HUDManager>("HUDManager");
            DrawComponentStatus<NeuroReachVR.Data.DataLogger>("DataLogger");
            DrawComponentStatus<NeuroReachVR.Data.KinematicDataCollector>("KinematicDataCollector");
            DrawComponentStatus<NeuroReachVR.Feedback.MultimodalFeedback>("MultimodalFeedback");
            DrawComponentStatus<NeuroReachVR.Core.AdaptiveDifficultyController>("AdaptiveDifficultyController");

            EditorGUILayout.Space(20);

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            showCreateOptions = EditorGUILayout.Foldout(showCreateOptions, "Create Missing Components");

            if (showCreateOptions)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Create Core Systems GameObject"))
                {
                    CreateCoreSystemsGameObject();
                }

                EditorGUILayout.HelpBox(
                    "This will create a '[Core Systems]' GameObject with all essential components.",
                    MessageType.Info
                );

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Select GameManager (if exists)"))
            {
                SelectComponent<GameManager>();
            }

            if (GUILayout.Button("Select HUDManager (if exists)"))
            {
                SelectComponent<HUDManager>();
            }

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Open Pre-Deployment Checklist", GUILayout.Height(30)))
            {
                PreDeploymentVerification.ShowWindow();
            }
        }

        private void DrawComponentStatus<T>(string displayName) where T : Component
        {
            var component = FindFirstObjectByType<T>(FindObjectsInactive.Include);

            EditorGUILayout.BeginHorizontal();

            if (component != null)
            {
                var originalColor = GUI.color;
                GUI.color = Color.green;
                GUILayout.Label("✓", GUILayout.Width(20));
                GUI.color = originalColor;

                GUILayout.Label(displayName, GUILayout.Width(200));

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = component.gameObject;
                    EditorGUIUtility.PingObject(component.gameObject);
                }
            }
            else
            {
                var originalColor = GUI.color;
                GUI.color = new Color(1f, 0.5f, 0f);
                GUILayout.Label("✗", GUILayout.Width(20));
                GUI.color = originalColor;

                GUILayout.Label(displayName + " - NOT FOUND", GUILayout.Width(200));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SelectComponent<T>() where T : Component
        {
            var component = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (component != null)
            {
                Selection.activeGameObject = component.gameObject;
                EditorGUIUtility.PingObject(component.gameObject);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Component Not Found",
                    $"{typeof(T).Name} not found in scene.",
                    "OK"
                );
            }
        }

        private void CreateCoreSystemsGameObject()
        {
            if (EditorUtility.DisplayDialog(
                "Create Core Systems",
                "This will create a '[Core Systems]' GameObject with all essential components. Continue?",
                "Yes",
                "Cancel"))
            {
                // Create root GameObject
                GameObject coreSystemsRoot = new GameObject("[Core Systems]");
                coreSystemsRoot.transform.position = Vector3.zero;

                // Create child GameObjects with components
                CreateChildWithComponent<GameManager>(coreSystemsRoot, "GameManager");
                CreateChildWithComponent<NeuroReachVR.Data.DataLogger>(coreSystemsRoot, "DataLogger");
                CreateChildWithComponent<NeuroReachVR.Data.KinematicDataCollector>(coreSystemsRoot, "KinematicDataCollector");
                CreateChildWithComponent<NeuroReachVR.Core.AdaptiveDifficultyController>(coreSystemsRoot, "AdaptiveDifficultyController");
                CreateChildWithComponent<NeuroReachVR.Feedback.MultimodalFeedback>(coreSystemsRoot, "MultimodalFeedback");

                // Mark scene dirty
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                // Select the root
                Selection.activeGameObject = coreSystemsRoot;

                Debug.Log("[Scene Setup] Created Core Systems GameObject with all essential components");

                EditorUtility.DisplayDialog(
                    "Core Systems Created",
                    "Core Systems GameObject created successfully!\n\n" +
                    "Next steps:\n" +
                    "1. Create InputHandler GameObject separately\n" +
                    "2. Create HUDManager GameObject separately\n" +
                    "3. Assign references in GameManager Inspector\n" +
                    "4. Run Pre-Deployment Checklist to verify",
                    "OK"
                );
            }
        }

        private GameObject CreateChildWithComponent<T>(GameObject parent, string name) where T : Component
        {
            // Check if component already exists
            var existing = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Debug.LogWarning($"[Scene Setup] {typeof(T).Name} already exists in scene, skipping creation");
                return existing.gameObject;
            }

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            child.AddComponent<T>();

            Debug.Log($"[Scene Setup] Created {name} with {typeof(T).Name} component");

            return child;
        }
    }

    /// <summary>
    /// Quick menu items for common tasks
    /// </summary>
    public static class QuickMenuItems
    {
        [MenuItem("NeuroReachVR/Quick Setup/1. Add Scene to Build Settings")]
        public static void AddSceneToBuildSettings()
        {
            var currentScene = EditorSceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(currentScene.path))
            {
                EditorUtility.DisplayDialog(
                    "Scene Not Saved",
                    "Please save the scene first before adding to build settings.",
                    "OK"
                );
                return;
            }

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Check if scene already in build
            bool alreadyInBuild = false;
            foreach (var scene in scenes)
            {
                if (scene.path == currentScene.path)
                {
                    alreadyInBuild = true;
                    break;
                }
            }

            if (!alreadyInBuild)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(currentScene.path, true));
                EditorBuildSettings.scenes = scenes.ToArray();

                Debug.Log($"[Quick Setup] Added {currentScene.name} to build settings at index 0");

                EditorUtility.DisplayDialog(
                    "Scene Added",
                    $"Scene '{currentScene.name}' added to build settings at index 0.",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Scene Already Added",
                    $"Scene '{currentScene.name}' is already in build settings.",
                    "OK"
                );
            }
        }

        [MenuItem("NeuroReachVR/Quick Setup/2. Configure for Android Build")]
        public static void ConfigureForAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                bool switchPlatform = EditorUtility.DisplayDialog(
                    "Switch to Android",
                    "Current platform is not Android. Switch to Android platform?\n\n" +
                    "This may take several minutes.",
                    "Yes",
                    "Cancel"
                );

                if (switchPlatform)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Android,
                        BuildTarget.Android
                    );
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Already Configured",
                    "Build platform is already set to Android.",
                    "OK"
                );
            }
        }

        [MenuItem("NeuroReachVR/Quick Setup/3. Open Build Settings")]
        public static void OpenBuildSettings()
        {
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }

        [MenuItem("NeuroReachVR/Documentation/Open Deployment Guide")]
        public static void OpenDeploymentGuide()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "../DEPLOYMENT_GUIDE.md");
            if (System.IO.File.Exists(path))
            {
                Application.OpenURL("file://" + path);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "File Not Found",
                    "DEPLOYMENT_GUIDE.md not found in project root.",
                    "OK"
                );
            }
        }

        [MenuItem("NeuroReachVR/Documentation/Open Quick Setup")]
        public static void OpenQuickSetup()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "../QUICK_SETUP.md");
            if (System.IO.File.Exists(path))
            {
                Application.OpenURL("file://" + path);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "File Not Found",
                    "QUICK_SETUP.md not found in project root.",
                    "OK"
                );
            }
        }
    }
}
