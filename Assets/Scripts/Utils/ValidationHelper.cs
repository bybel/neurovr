using UnityEngine;
using System.Collections.Generic;

namespace NeuroReachVR.Utils
{
    /// <summary>
    /// Consolidates 63+ null check patterns across the codebase
    /// Provides clean validation with automatic error logging
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validate a single component reference
        /// </summary>
        public static bool ValidateComponent<T>(T component, string componentName, GameObject context = null) where T : Component
        {
            if (component == null)
            {
                string contextInfo = context != null ? $" on {context.name}" : "";
                Debug.LogError($"[Validation] Missing {componentName}{contextInfo}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validate multiple components at once
        /// </summary>
        public static bool ValidateComponents(params (Component component, string name)[] components)
        {
            bool allValid = true;
            foreach (var (component, name) in components)
            {
                if (component == null)
                {
                    Debug.LogError($"[Validation] Missing {name}");
                    allValid = false;
                }
            }
            return allValid;
        }

        /// <summary>
        /// Validate and get component, with automatic AddComponent fallback
        /// </summary>
        public static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
                Debug.LogWarning($"[Validation] Added missing {typeof(T).Name} to {gameObject.name}");
            }
            return component;
        }

        /// <summary>
        /// Validate GameObject reference
        /// </summary>
        public static bool ValidateGameObject(GameObject obj, string objectName)
        {
            if (obj == null)
            {
                Debug.LogError($"[Validation] Missing GameObject: {objectName}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validate list/collection is not null or empty
        /// </summary>
        public static bool ValidateCollection<T>(ICollection<T> collection, string collectionName)
        {
            if (collection == null || collection.Count == 0)
            {
                Debug.LogWarning($"[Validation] {collectionName} is null or empty");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validate numeric value is within range
        /// </summary>
        public static bool ValidateRange(float value, float min, float max, string valueName)
        {
            if (value < min || value > max)
            {
                Debug.LogWarning($"[Validation] {valueName} ({value}) is outside valid range [{min}, {max}]");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validate string is not null or empty
        /// </summary>
        public static bool ValidateString(string value, string fieldName)
        {
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogError($"[Validation] {fieldName} is null or empty");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Try to find component in scene with validation
        /// </summary>
        public static T FindAndValidate<T>(string componentName) where T : Component
        {
            T component = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (component == null)
            {
                Debug.LogError($"[Validation] Could not find {componentName} in scene");
            }
            return component;
        }

        /// <summary>
        /// Validate array index is in bounds
        /// </summary>
        public static bool ValidateIndex<T>(T[] array, int index, string arrayName)
        {
            if (array == null)
            {
                Debug.LogError($"[Validation] {arrayName} is null");
                return false;
            }
            if (index < 0 || index >= array.Length)
            {
                Debug.LogError($"[Validation] Index {index} is out of bounds for {arrayName} (length: {array.Length})");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validate all required fields in inspector
        /// Returns true if all valid, logs all errors at once
        /// </summary>
        public static bool ValidateRequiredFields(MonoBehaviour context, params (object field, string name)[] fields)
        {
            List<string> missingFields = new List<string>();

            foreach (var (field, name) in fields)
            {
                if (field == null || (field is Object unityObj && unityObj == null))
                {
                    missingFields.Add(name);
                }
            }

            if (missingFields.Count > 0)
            {
                Debug.LogError($"[Validation] {context.GetType().Name} on {context.gameObject.name} is missing required fields:\n" +
                    $"- {string.Join("\n- ", missingFields)}\n" +
                    $"Please assign these in the Inspector.", context);
                return false;
            }

            return true;
        }
    }
}
