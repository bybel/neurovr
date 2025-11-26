using UnityEngine;
using System.Collections.Generic;
using NeuroReachVR.Input;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Path tracing exercise for fine motor control training
    /// Uses stylus to trace generated paths with real-time accuracy feedback
    /// </summary>
    public class PathTracingTask : BaseTask
    {
        [Header("Path Generation")]
        [SerializeField] protected TraceablePath pathPrefab; // Protected so SpiralTracingTask can access it
        [SerializeField] protected PathType pathType = PathType.Line;
        [SerializeField] protected Vector3 pathStart = new Vector3(-0.5f, 1f, 1f);
        [SerializeField] protected Vector3 pathEnd = new Vector3(0.5f, 1f, 1f);
        [SerializeField] private float pathLength = 1f;
        
        [Header("Tracing Settings")]
        [SerializeField] private float pathWidth = 0.1f;
        [SerializeField] protected int pathSegments = 50;
        [SerializeField] protected float minAccuracy = 0.7f;
        
        [Header("Feedback")]
        [SerializeField] protected TaskFeedback feedback;
        
        protected TraceablePath currentPath;
        protected List<TraceablePath> completedPaths;
        protected bool isTracing;
        protected int pathsCompleted;
        protected float pathStartTime; // Time when current path tracing began
        
        protected override void Start()
        {
            base.Start();
            completedPaths = new List<TraceablePath>();
            
            // Auto-find pathPrefab if not assigned
            if (pathPrefab == null)
            {
                // Try to find TraceablePath in scene (might be a template object)
                pathPrefab = FindFirstObjectByType<TraceablePath>(FindObjectsInactive.Include);
                
                // Try to load from Resources folder
                if (pathPrefab == null)
                {
                    var prefabObj = Resources.Load<GameObject>("TraceablePath");
                    if (prefabObj != null)
                        pathPrefab = prefabObj.GetComponent<TraceablePath>();
                }
                
                if (pathPrefab != null)
                    Debug.Log($"[{GetType().Name}] Auto-found TraceablePath prefab");
                else
                    Debug.LogError($"[{GetType().Name}] TraceablePath prefab not assigned! Please assign in Inspector or place in Resources folder.");
            }
        }
        
        protected override void UpdateTask()
        {
            // Support all input modes: Stylus, Simulator (mouse), Hand tracking
            if (!inputHandler.HasValidInput)
            {
                Debug.LogWarning("[PathTracing] No valid input detected");
                return;
            }
            
            if (currentPath == null)
            {
                GenerateNewPath();
                return;
            }
            
            if (currentPath.IsComplete)
            {
                OnPathCompleted();
                return;
            }
            
            UpdateTracing();
        }
        
        private void GenerateNewPath()
        {
            if (pathPrefab == null)
            {
                Debug.LogError("[PathTracingTask] Path prefab not assigned! Please assign a TraceablePath prefab in the Inspector.");
                return;
            }
            
            List<Vector3> pathPoints = GeneratePathPoints();
            
            GameObject pathObj = Instantiate(pathPrefab.gameObject);
            currentPath = pathObj.GetComponent<TraceablePath>();
            
            if (currentPath == null)
            {
                Debug.LogError("[PathTracingTask] Instantiated path prefab is missing TraceablePath component!");
                Destroy(pathObj);
                return;
            }
            
            currentPath.InitializePath(pathPoints);
            
            isTracing = true;
            pathStartTime = elapsedTime; // Record when path tracing begins
        }
        
        protected virtual List<Vector3> GeneratePathPoints()
        {
            return pathType switch
            {
                PathType.Line => PathGenerator.GenerateLine(pathStart, pathEnd, pathSegments),
                PathType.Curve => PathGenerator.GenerateCurve(pathStart, pathEnd, 
                    (pathStart + pathEnd) * 0.5f + Vector3.up * 0.3f, pathSegments),
                PathType.Circle => PathGenerator.GenerateCircle((pathStart + pathEnd) * 0.5f, 
                    pathLength * 0.5f, pathSegments),
                PathType.Square => PathGenerator.GenerateSquare((pathStart + pathEnd) * 0.5f, 
                    pathLength, pathSegments / 4),
                _ => PathGenerator.GenerateLine(pathStart, pathEnd, pathSegments)
            };
        }
        
        private void UpdateTracing()
        {
            if (!isTracing) return;
            
            // Support all input modes: Stylus press, Mouse click, or Hand pinch
            bool isPressed = inputHandler.IsStylusPressed || inputHandler.IsPinching;
            if (!isPressed) return;
            
            Vector3 inputPos = inputHandler.Position;
            currentPath.UpdateTracing(inputPos);
        }
        
        protected virtual void OnPathCompleted()
        {
            float accuracy = currentPath.Accuracy;
            bool success = accuracy >= minAccuracy;
            float completionTime = elapsedTime - pathStartTime; // Actual time spent tracing this path
            
            if (success)
            {
                pathsCompleted++;
                AddScore(Mathf.RoundToInt(accuracy * 100));
                feedback?.PlaySuccess(currentPath.transform.position);
            }
            else
            {
                IncrementError();
                feedback?.PlayError(currentPath.transform.position);
            }
            
            // Report attempt to adaptive difficulty system
            ReportAttempt(completionTime, success, accuracy);
            
            completedPaths.Add(currentPath);
            currentPath = null;
            isTracing = false;
        }
        
        protected override void OnTaskStarted()
        {
            pathsCompleted = 0;
            if (currentPath != null)
            {
                Destroy(currentPath.gameObject);
                currentPath = null;
            }
        }
        
        protected override void OnTaskEnded()
        {
            if (currentPath != null)
            {
                Destroy(currentPath.gameObject);
                currentPath = null;
            }
            
            foreach (var path in completedPaths)
                Destroy(path.gameObject);
            completedPaths.Clear();
        }
        
        public void SetPathType(PathType type)
        {
            pathType = type;
        }
        
        public void SetDifficulty(float width, float requiredAccuracy)
        {
            pathWidth = width;
            minAccuracy = requiredAccuracy;
        }
    }
    
    public enum PathType
    {
        Line,
        Curve,
        Circle,
        Square
    }
}

