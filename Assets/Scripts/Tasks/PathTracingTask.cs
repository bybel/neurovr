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
        
        protected override void Start()
        {
            base.Start();
            completedPaths = new List<TraceablePath>();
        }
        
        protected override void UpdateTask()
        {
            if (!inputHandler.HasValidInput || inputHandler.CurrentMode != InputMode.Stylus)
            {
                Debug.LogWarning("[PathTracing] Stylus input required");
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
            List<Vector3> pathPoints = GeneratePathPoints();
            
            GameObject pathObj = Instantiate(pathPrefab.gameObject);
            currentPath = pathObj.GetComponent<TraceablePath>();
            currentPath.InitializePath(pathPoints);
            
            isTracing = true;
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
            if (!isTracing || !inputHandler.IsStylusPressed) return;
            
            Vector3 stylusPos = inputHandler.Position;
            currentPath.UpdateTracing(stylusPos);
        }
        
        protected virtual void OnPathCompleted()
        {
            float accuracy = currentPath.Accuracy;
            bool success = accuracy >= minAccuracy;
            float completionTime = elapsedTime - (currentPath != null ? currentPath.Progress * sessionDuration : 0f);
            
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

