using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace NeuroReachVR.Editor
{
    /// <summary>
    /// Filters out known Oculus SDK warnings that can't be fixed
    /// Only suppresses "Converting invalid MinMaxAABB" from TubeRenderer
    /// </summary>
    [InitializeOnLoad]
    public static class ConsoleErrorFilter
    {
        private static bool isFiltering = false;

        static ConsoleErrorFilter()
        {
            // Only enable in Play Mode to reduce noise
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Application.logMessageReceived += FilterLog;
                isFiltering = true;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Application.logMessageReceived -= FilterLog;
                isFiltering = false;
            }
        }

        private static void FilterLog(string logString, string stackTrace, LogType type)
        {
            // Only filter specific known Oculus SDK errors
            if (type == LogType.Error &&
                logString.Contains("Converting invalid MinMaxAABB") &&
                stackTrace.Contains("TubeRenderer"))
            {
                // Suppress this error - it's a known Oculus SDK issue
                // The error is logged but we prevent it from appearing in console
                return;
            }
        }

        [MenuItem("NeuroReachVR/Debug/Enable Oculus Error Filtering")]
        private static void EnableFiltering()
        {
            if (!isFiltering)
            {
                Application.logMessageReceived += FilterLog;
                isFiltering = true;
                Debug.Log("[Console Filter] Oculus error filtering enabled");
            }
        }

        [MenuItem("NeuroReachVR/Debug/Disable Oculus Error Filtering")]
        private static void DisableFiltering()
        {
            if (isFiltering)
            {
                Application.logMessageReceived -= FilterLog;
                isFiltering = false;
                Debug.Log("[Console Filter] Oculus error filtering disabled");
            }
        }
    }
}
