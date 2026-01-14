using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NeuroReachVR.Input;
using NeuroReachVR.Core;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Path Task: User traces a visible path.
    /// Session: 5 consecutive paths.
    /// Mechanics: Press button to ink, Release button to finish path.
    /// </summary>
    public class PathTask : PathTracingTask
    {
        [Header("Path Task Settings")]
        [SerializeField] private int trialsPerSession = 5;
        [SerializeField] private float delayBetweenTrials = 1.0f;
        
        [Header("Table Calibration")]
        [SerializeField] private TableCalibrationManager calibrationManager;
        
        private int currentTrialIndex = 0;
        private bool isWaitingForNextTrial = false;
        private bool strokeStarted = false;

        protected override void Start()
        {
            base.Start();
            
            if (calibrationManager == null)
            {
                calibrationManager = FindFirstObjectByType<TableCalibrationManager>();
                if (calibrationManager == null)
                {
                    GameObject cmObj = new GameObject("TableCalibrationManager");
                    calibrationManager = cmObj.AddComponent<TableCalibrationManager>();
                }
            }
        }
        
        protected override void OnTaskStarted()
        {
            // Reset session
            currentTrialIndex = 0;
            pathsCompleted = 0;
            isWaitingForNextTrial = false;
            strokeStarted = false;
            
            // Allow base to cleanup old paths
            base.OnTaskStarted();

            // Check Calibration for Easy Mode
            CheckCalibration();
        }

        private void CheckCalibration()
        {
             if (adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy)
            {
                if (calibrationManager != null && !calibrationManager.IsCalibrated)
                {
                    Debug.Log("[PathTask] Easy mode detected but not calibrated. Starting calibration...");
                    calibrationManager.StartCalibration();
                    // We will generate the path AFTER calibration completes? 
                    // Or we can rely on Update to retry generation if calibration finishes?
                    // actually base.UpdateTask calls GenerateNewPath() if currentPath is null.
                    // If we are calibrating, we shouldn't generate a path yet.
                    return; 
                }
            }
        }

        protected override void UpdateTask()
        {
            // If calibrating, do nothing else
            if (calibrationManager != null && !calibrationManager.IsCalibrated && 
                adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy)
            {
                // Wait for user to finish calibration
                // Maybe show a hint "Please calibrate table"
                return;
            }

            // 1. Ensure path exists
            if (currentPath == null && !isWaitingForNextTrial)
            {
                if (currentTrialIndex < trialsPerSession)
                {
                    GenerateNewPath();
                }
                else
                {
                    // Session Complete
                    Debug.Log("[PathTask] Session Complete!");
                    // End the task or return to menu?
                    // For now, let's just stop generating.
                    return;
                }
            }
            
            if (currentPath == null) return;

            // 2. Custom Input Logic
            // Press -> Ink
            // Release -> Finish
            
            bool isPressed = inputHandler.IsStylusPressed || inputHandler.IsPinching;
            
            // Logic:
            // If Pressed:
            //   - If !strokeStarted: This is a NEW stroke. Start it.
            //   - Update Trace.
            // If Released:
            //   - If strokeStarted: The USER RELEASED. End the trial.
            
            if (isPressed)
            {
                if (!strokeStarted)
                {
                    Debug.Log("[PathTask] Stroke Started");
                    strokeStarted = true;
                    currentPath.StartNewStroke();
                }
                
                // Update Tracing
                 if (currentPath != null)
                {
                    currentPath.UpdateTracing(inputHandler.Position);
                }
            }
            else
            {
                // Not Pressed
                if (strokeStarted)
                {
                    // Released!
                    Debug.Log("[PathTask] Button Released - Finishing Trial");
                    strokeStarted = false;
                    OnPathCompleted();
                }
            }
        }

        protected override void GenerateNewPath()
        {
             if (pathPrefab == null)
            {
                Debug.LogError("[PathTask] Path prefab not assigned!");
                return;
            }
            
            // Mode Check
            bool isTableMode = adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy;
            
            // Positioning
            if (!isTableMode)
            {
                // Use closer interaction depth (0.4f) for closer reach
                PositionPathInFrontOfCamera(0.4f);
            }
            
            // Generate Points
            List<Vector3> pathPoints = GeneratePathPoints();
            
            // Instantiate
            GameObject pathObj = Instantiate(pathPrefab.gameObject);
            pathObj.transform.SetParent(null);
            
            currentPath = pathObj.GetComponent<TraceablePath>();
            if (currentPath == null)
            {
                Destroy(pathObj);
                return;
            }
            
            currentPath.InitializePath(pathPoints);
            currentPath.SetPathWidth(pathWidth);
            
            // Alignment
            if (isTableMode && calibrationManager != null && calibrationManager.IsCalibrated)
            {
                currentPath.SetAlignment(LineAlignment.TransformZ);
                Vector3 tableNormal = calibrationManager.PlaneRotation * Vector3.up;
                currentPath.transform.rotation = Quaternion.LookRotation(tableNormal, Vector3.forward);
            }
            else
            {
                currentPath.SetAlignment(LineAlignment.View);
                currentPath.transform.rotation = Quaternion.identity;
            }
            
            isTracing = true;
            pathStartTime = elapsedTime;
            
            Debug.Log($"[PathTask] Generated Trial {currentTrialIndex + 1}/{trialsPerSession}");
        }

         protected override List<Vector3> GeneratePathPoints()
        {
            bool isEasy = adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy;
            
            if (isEasy && calibrationManager != null && calibrationManager.IsCalibrated)
            {
                return GenerateTablePath();
            }
            
            return GenerateAirPath(); 
        }

        private List<Vector3> GenerateTablePath()
        {
            // Similar to Spiral but purely random/curved paths for this task? 
            // Or just a line/shape?
            // "The user sees a given path... and traces it"
            // Let's generate a random curve for variety.
            
            Vector3 center = calibrationManager.PlaneCenter;
            Quaternion rotation = calibrationManager.PlaneRotation;
            Vector2 zoneSize = calibrationManager.ZoneSize;
            
            // Generate a random Bezier curve or simple shape within bounds
            // Let's use a simple Sine wave or Curve for now.
            
            int segments = pathSegments;
            List<Vector3> points = new List<Vector3>();
            
            float width = zoneSize.x * 0.8f;
            float height = zoneSize.y * 0.8f;
            
            Vector3 startLocal = new Vector3(-width * 0.4f, 0, -height * 0.2f);
            Vector3 endLocal = new Vector3(width * 0.4f, 0, height * 0.2f);
            
            // Add some randomness based on trial index
            Random.InitState(currentTrialIndex * 1337); 
            
            Vector3 cp1 = new Vector3(Random.Range(-width*0.3f, width*0.3f), 0, Random.Range(-height*0.3f, height*0.3f));
            
            Vector3 planeRight = rotation * Vector3.right;
            Vector3 planeForward = rotation * Vector3.forward;
            
            // Simple Quadratic Bezier
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = CalculateBezierPoint(t, startLocal, cp1, endLocal);
                
                // Transform to World
                Vector3 worldP = center + (planeRight * p.x) + (planeForward * p.z);
                points.Add(worldP);
            }
            
            pathStart = points[0];
            pathEnd = points[points.Count - 1];
            
            return points;
        }

        private List<Vector3> GenerateAirPath()
        {
             // Use standard PathGenerator but maybe randomize it?
             // Taking logic from base but ensuring it's "Air" centered.
             // Base PositionPathInFrontOfCamera sets pathStart/End.
             
             // Let's just use a Curve.
             return PathGenerator.GenerateCurve(pathStart, pathEnd, 
                     (pathStart + pathEnd) * 0.5f + Vector3.up * 0.2f, pathSegments);
        }
        
        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            Vector3 p = uu * p0; 
            p += 2 * u * t * p1; 
            p += tt * p2; 
            return p;
        }

        protected override void OnPathCompleted()
        {
            // Check Accuracy
            float accuracy = currentPath.Accuracy;
            bool success = accuracy >= minAccuracy;
            float completionTime = elapsedTime - pathStartTime;
            
            Debug.Log($"[PathTask] Trial {currentTrialIndex + 1} Completed. Accuracy: {accuracy:P1}");
            
            // Feedback
            if (success)
            {
                feedback?.PlaySuccess(currentPath.transform.position);
                AddScore(Mathf.RoundToInt(accuracy * 100));
            }
            else
            {
                feedback?.PlayError(currentPath.transform.position);
                IncrementError(); // Or just count it as an attempt
            }
            
            // Report
            ReportAttempt(completionTime, success, accuracy);
            
            // Cleanup
            completedPaths.Add(currentPath);
            currentPath.gameObject.SetActive(false); // Hide immediately
            currentPath = null;
            isTracing = false;
            
            // Next Trial
            currentTrialIndex++;
            if (currentTrialIndex < trialsPerSession)
            {
                StartCoroutine(WaitAndNextTrial());
            }
            else
            {
                Debug.Log("[PathTask] All trials completed.");
                // Optional: Show Session Summary Interaction?
            }
        }
        
        private IEnumerator WaitAndNextTrial()
        {
            isWaitingForNextTrial = true;
            yield return new WaitForSeconds(delayBetweenTrials);
            isWaitingForNextTrial = false;
        }

        private void PositionPathInFrontOfCamera(float depth)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[PathTask] Main Camera is null! Cannot position path.");
                return;
            }

            Debug.Log($"[PathTask] Positioning path. Camera: {mainCam.name}, Pos: {mainCam.transform.position}, Fwd: {mainCam.transform.forward}");
            
            // Update class member variables, do not declare local ones
            pathStart = mainCam.transform.position + (mainCam.transform.forward * depth); 
            pathEnd = pathStart + (mainCam.transform.right * 0.3f); // 30cm wide path

            // Reset task transform to world zero so points are absolute
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            
            Debug.Log($"[PathTask] Positioned path using Transform: Start={pathStart}, End={pathEnd} (Depth {depth})");
        }
    }
}
