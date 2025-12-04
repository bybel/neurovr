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
        [SerializeField] protected float minAccuracy = 0.3f; // Lowered from 0.7 for easier gameplay
        
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
            // 1. Ensure path exists (Visuals first!)
            if (currentPath == null)
            {
                GenerateNewPath();
                // Don't return here, allow input check to proceed
            }
            
            // 2. Check Input
            // Support all input modes: Stylus, Simulator (mouse), Hand tracking
            if (!inputHandler.HasValidInput)
            {
                // Only log warning periodically to avoid spam
                if (Time.frameCount % 300 == 0)
                    Debug.LogWarning("[PathTracing] No valid input detected - cannot trace");
                return;
            }
            
            if (currentPath == null) return; // Should be handled above, but safety check
            
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
            
            // Position the path in front of the camera at a comfortable height
            PositionPathInFrontOfCamera();
            
            List<Vector3> pathPoints = GeneratePathPoints();
            
            GameObject pathObj = Instantiate(pathPrefab.gameObject);
            
            // CRITICAL: Ensure path is in World Space and NOT parented to Camera or Task Manager
            pathObj.transform.SetParent(null);
            
            currentPath = pathObj.GetComponent<TraceablePath>();
            
            if (currentPath == null)
            {
                Debug.LogError("[PathTracingTask] Instantiated path prefab is missing TraceablePath component!");
                Destroy(pathObj);
                return;
            }
            
            currentPath.InitializePath(pathPoints);
            
            Debug.Log($"[PathTracingTask] Generated path from {pathStart} to {pathEnd} with {pathPoints.Count} points");
            
            isTracing = true;
            pathStartTime = elapsedTime; // Record when path tracing begins
        }
        
        /// <summary>
        /// Positions the path start and end points in front of the camera
        /// Uses the same projection method as SimulatorInput for accurate mouse interaction
        /// </summary>
        private void PositionPathInFrontOfCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;
            
            // Use the same interaction depth as SimulatorInput (1.5m)
            float interactionDepth = 1.5f;
            
            // Get the center point by projecting the screen center
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            Ray centerRay = mainCam.ScreenPointToRay(screenCenter);
            Vector3 pathCenter = centerRay.GetPoint(interactionDepth);
            
            // Get left and right points by projecting screen edges
            // Use 30% from center to edge for comfortable reach
            float screenOffset = Screen.width * 0.3f;
            
            Vector3 screenLeft = new Vector3(Screen.width * 0.5f - screenOffset, Screen.height * 0.5f, 0);
            Vector3 screenRight = new Vector3(Screen.width * 0.5f + screenOffset, Screen.height * 0.5f, 0);
            
            Ray leftRay = mainCam.ScreenPointToRay(screenLeft);
            Ray rightRay = mainCam.ScreenPointToRay(screenRight);
            
            pathStart = leftRay.GetPoint(interactionDepth);
            pathEnd = rightRay.GetPoint(interactionDepth);
            
            Debug.Log($"[PathTracingTask] Positioned path at center: {pathCenter}, start: {pathStart}, end: {pathEnd}");
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
            
            Debug.Log($"[PathTracingTask] Path completed! Accuracy: {accuracy:P1}, Required: {minAccuracy:P1}, Success: {success}");
            
            if (success)
            {
                pathsCompleted++;
                int scoreToAdd = Mathf.RoundToInt(accuracy * 100);
                AddScore(scoreToAdd);
                Debug.Log($"[PathTracingTask] Score added: {scoreToAdd}, Total paths completed: {pathsCompleted}");
                feedback?.PlaySuccess(currentPath.transform.position);
            }
            else
            {
                IncrementError();
                Debug.Log($"[PathTracingTask] Path failed - accuracy too low");
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

