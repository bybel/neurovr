using UnityEngine;
using System;
using System.Collections.Generic;

namespace NeuroReachVR.Core
{
    /// <summary>
    /// Lightweight service locator for dependency management
    /// Eliminates FindFirstObjectByType calls and improves testability
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator instance;
        private readonly Dictionary<Type, Component> services = new();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Register<T>(T service) where T : Component
        {
            if (instance == null) CreateInstance();

            var type = typeof(T);
            if (instance.services.ContainsKey(type))
                instance.services[type] = service;
            else
                instance.services.Add(type, service);
        }

        public static T Get<T>() where T : Component
        {
            if (instance == null) CreateInstance();

            var type = typeof(T);
            if (instance.services.TryGetValue(type, out var service))
                return service as T;

            // Fallback to Find if not registered
            var found = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (found != null)
            {
                Register(found);
                return found;
            }

            Debug.LogWarning($"[ServiceLocator] Service {typeof(T).Name} not found");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : Component
        {
            service = Get<T>();
            return service != null;
        }

        private static void CreateInstance()
        {
            var go = new GameObject("[ServiceLocator]");
            instance = go.AddComponent<ServiceLocator>();
            DontDestroyOnLoad(go);
        }

        public static void Clear() => instance?.services.Clear();
    }
}
