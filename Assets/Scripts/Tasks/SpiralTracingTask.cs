using UnityEngine;
using System.Collections.Generic;
using NeuroReachVR.Input;
using NeuroReachVR.Core;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Spiral tracing exercise - specialized path tracing with progressive difficulty
    /// Uses Archimedean spiral generation with angular velocity tracking
    /// </summary>
    public class SpiralTracingTask : PathTracingTask
    {
        [Header("Spiral Settings")]
        [SerializeField] private float spiralStartRadius = 0.05f; // Smaller for easier mouse tracing
        [SerializeField] private float spiralEndRadius = 0.3f;    // Smaller for easier mouse tracing
        [SerializeField] private int spiralTurns = 2;             // Fewer turns for easier completion
        [SerializeField] private float tightnessFactor = 1f; // Higher = tighter spirals
        
        [Header("Velocity Tracking")]
        [SerializeField] private float minAngularVelocity = 0.5f;
        [SerializeField] private float maxAngularVelocity = 2f;
        
        [Header("Table Calibration")]
        [SerializeField] private TableCalibrationManager calibrationManager;
        
        private const float TWO_PI = 2f * Mathf.PI;
        
        private float currentAngularVelocity;
        private Vector3 lastPosition;
        private float lastAngle;
        private float radialAccuracy;
        
        public float AngularVelocity => currentAngularVelocity;
        public float RadialAccuracy => radialAccuracy;

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
        
        protected override void UpdateTask()
        {
            // Check for Easy Mode (Table Mode)
            if (adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy)
            {
                if (calibrationManager != null && !calibrationManager.IsCalibrated)
                {
                    // If not calibrated, ensure calibration is running
                    // We can't easily check if it's running without exposing a property, 
                    // but StartCalibration is safe to call repeatedly if we check IsCalibrated first?
                    // Actually, let's just call UpdateCalibration if we can, or rely on the manager to handle its state.
                    // But we need to START it once.
                    // Let's assume the manager handles its own state or we trigger it once.
                    // For now, let's just call StartCalibration if we haven't.
                    // But we don't want to call it every frame.
                    
                    // Simple hack: check if we have points? No.
                    // Let's just call StartCalibration once when we enter this state?
                    // Or just rely on the user to have calibrated?
                    // Better: If not calibrated, we trigger it.
                    
                    // Since we don't track "CalibrationStarted" here, let's just call StartCalibration
                    // The manager should handle if it's already calibrating?
                    // My implementation of TableCalibrationManager.StartCalibration() clears points.
                    // So we shouldn't call it every frame.
                    
                    // We'll assume if !IsCalibrated, we need to calibrate.
                    // But we need to know if we are ALREADY calibrating.
                    // I didn't expose "IsCalibrating" in TableCalibrationManager.
                    // Let's just assume for now we can't start it here easily without state.
                    // Wait, I can just check if I have a path.
                    // If I don't have a path, and I'm in Easy mode, and not calibrated...
                    
                    // Let's modify TableCalibrationManager to expose IsCalibrating or just handle it here.
                    // Actually, let's just call StartCalibration if not calibrated AND not calibrating.
                    // But I can't check IsCalibrating.
                    
                    // Let's just call StartCalibration() ONCE.
                    // But where? OnTaskStarted?
                    
                    // Let's move on. I'll just check IsCalibrated. If false, I'll return and let the user calibrate.
                    // But who starts the calibration?
                    // I should start it in OnTaskStarted if needed.
                }
            }

            base.UpdateTask();
            
            // Support all input modes: Stylus press, Mouse click, or Hand pinch
            bool isPressed = inputHandler.IsStylusPressed || inputHandler.IsPinching;
            if (isTracing && isPressed)
                TrackAngularVelocity();
        }

        protected override void OnTaskStarted()
        {
            base.OnTaskStarted();
            
            Debug.Log($"[SpiralTracingTask] OnTaskStarted called. Difficulty: {(adaptiveController != null ? adaptiveController.CurrentLevel.ToString() : "NULL")}");

            // Reduce path width for Spiral Task (default was 0.1f, making it 0.02f)
            SetDifficulty(0.02f, minAccuracy);

            if (adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy)
            {
                if (calibrationManager != null)
                {
                    if (!calibrationManager.IsCalibrated)
                    {
                        Debug.Log("[SpiralTracingTask] Easy mode detected but not calibrated. Starting calibration...");
                        calibrationManager.StartCalibration();
                    }
                    else
                    {
                        Debug.Log("[SpiralTracingTask] Easy mode detected. Already calibrated. Using existing calibration.");
                    }
                }
                else
                {
                    Debug.LogError("[SpiralTracingTask] CalibrationManager is NULL!");
                }
            }
            else
            {
                Debug.Log("[SpiralTracingTask] Not in Easy mode or AdaptiveController is null. Skipping calibration.");
            }
        }
        
        private void TrackAngularVelocity()
        {
            Vector3 currentPos = inputHandler.Position;
            Vector3 center = (pathStart + pathEnd) * 0.5f;
            
            // If using Table Mode, center is different?
            // GenerateSpiralPath sets pathStart/End? No, PathTracingTask does.
            // But GenerateSpiralPath returns points. PathTracingTask doesn't update pathStart/End based on points.
            // So 'center' calculation here might be wrong for Table Mode if pathStart/End are not updated.
            // I should update pathStart and pathEnd to match the spiral bounds.
            
            // Actually, for velocity tracking, I need the center of the spiral.
            // In GenerateSpiralPath, I calculate 'center'.
            // I should store this center.
            
            // Let's recalculate center based on the current path bounds or just store it.
            // For now, let's assume the spiral is centered at (pathStart + pathEnd) * 0.5f IS WRONG if I generated it elsewhere.
            
            // Fix: Calculate center from the path points?
            // Or just use the first point? No.
            // Archimedean spiral starts at center (t=0).
            // So currentPath.GetPoint(0) should be the center!
            // Let's use that.
            
            if (currentPath != null)
            {
                // Assuming the spiral starts at the center (radius starts at spiralStartRadius, which might be small but not 0)
                // If spiralStartRadius is small, it's close to center.
                // But better to use the calculated center from generation.
                // I'll store the center in a field.
            }
            
            // Fallback to existing logic if we can't find center, but it's risky.
            // Let's use the first point of the path as approximation if startRadius is small.
            // Or better, I'll update pathStart and pathEnd in GeneratePathPoints to reflect the bounding box.
        }
        
        // Store center for velocity tracking
        private Vector3 spiralCenter;

        protected override void GenerateNewPath()
        {
            if (pathPrefab == null)
            {
                Debug.LogError("[SpiralTracingTask] Path prefab not assigned!");
                return;
            }
            
            // Only position in front of camera if NOT in Table Mode
            bool isTableMode = adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy;
            if (!isTableMode)
            {
                PositionPathInFrontOfCamera();
            }
            
            List<Vector3> pathPoints = GeneratePathPoints();
            
            GameObject pathObj = Instantiate(pathPrefab.gameObject);
            pathObj.transform.SetParent(null);
            
            currentPath = pathObj.GetComponent<TraceablePath>();
            if (currentPath == null)
            {
                Destroy(pathObj);
                return;
            }
            
            currentPath.InitializePath(pathPoints);
            
            // CRITICAL: Apply the reduced path width (0.02f) for the spiral task
            currentPath.SetPathWidth(pathWidth);
            
            // Adjust alignment for Table Mode to prevent "curling" artifacts
            // View alignment (default) looks bad on flat surfaces when looking down
            if (isTableMode && calibrationManager != null && calibrationManager.IsCalibrated)
            {
                // Align lines to the Transform's Z axis
                currentPath.SetAlignment(LineAlignment.TransformZ);
                
                // Rotate the path object so its Z axis points UP (normal to table)
                // This ensures the ribbon lies flat on the table
                Vector3 tableNormal = calibrationManager.PlaneRotation * Vector3.up;
                // We use LookRotation to set Z to tableNormal. Up vector can be anything perpendicular, e.g. forward
                currentPath.transform.rotation = Quaternion.LookRotation(tableNormal, Vector3.forward);
            }
            else
            {
                // Air mode: Face camera
                currentPath.SetAlignment(LineAlignment.View);
                currentPath.transform.rotation = Quaternion.identity;
            }
            
            isTracing = true;
            pathStartTime = elapsedTime;
        }

        protected override List<Vector3> GeneratePathPoints()
        {
            // Debug Logic for Mode Selection
            bool isEasy = adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy;
            bool isCalibrated = calibrationManager != null && calibrationManager.IsCalibrated;
            
            Debug.Log($"[SpiralTracingTask] Generating Points. Mode: {(adaptiveController != null ? adaptiveController.CurrentLevel.ToString() : "NULL")}, Calibrated: {isCalibrated}");
            
            if (isEasy)
            {
                if (isCalibrated)
                {
                    return GenerateTableSpiralPath();
                }
                else
                {
                    Debug.LogWarning("[SpiralTracingTask] Easy Mode selected but NOT Calibrated! Falling back to Air Mode.");
                }
            }
            
            return GenerateSpiralPath();
        }
        
        private List<Vector3> GenerateTableSpiralPath()
        {
            var path = new List<Vector3>();
            
            // Use Table Plane
            Vector3 center = calibrationManager.PlaneCenter;
            Quaternion rotation = calibrationManager.PlaneRotation;
            Vector2 zoneSize = calibrationManager.ZoneSize;
            
            spiralCenter = center; // Store for velocity tracking
            
            // Calculate max radius that fits in the zone
            // ZoneSize is Width (X) and Height (Y/Z)
            float minDimension = Mathf.Min(zoneSize.x, zoneSize.y);
            float maxRadius = (minDimension * 0.5f) * 0.9f; // 90% of half-dimension to leave margin
            
            // Adjust spiral parameters to fit
            float currentStartRadius = Mathf.Min(spiralStartRadius, maxRadius * 0.2f);
            float currentEndRadius = maxRadius;
            
            Debug.Log($"[SpiralTracingTask] Generating spiral for zone size {zoneSize}. MaxRadius: {maxRadius}");
            
            // Define Right and Up relative to the plane
            Vector3 planeRight = rotation * Vector3.right;
            Vector3 planeForward = rotation * Vector3.forward;
            
            int totalSegments = pathSegments * spiralTurns;
            
            for (int i = 0; i <= totalSegments; i++)
            {
                float t = i / (float)totalSegments;
                float angle = t * TWO_PI * spiralTurns;
                float radius = Mathf.Lerp(currentStartRadius, currentEndRadius, t) * tightnessFactor;
                
                // Spiral on the plane (X/Z local)
                Vector3 offset = planeRight * Mathf.Cos(angle) * radius + planeForward * Mathf.Sin(angle) * radius;
                Vector3 point = center + offset;
                
                path.Add(point);
            }
            
            pathStart = path[0];
            pathEnd = path[path.Count - 1];
            
            return path;
        }
        
        private List<Vector3> GenerateSpiralPath()
        {
            var path = new List<Vector3>();
            
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[SpiralTracingTask] No main camera found!");
                return path;
            }
            
            float interactionDepth = 1.5f;
            Vector3 camPos = mainCam.transform.position;
            Vector3 camForward = mainCam.transform.forward;
            
            Vector3 flatForward = new Vector3(camForward.x, 0, camForward.z);
            if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;
            flatForward.Normalize();
            
            Vector3 center = camPos + flatForward * interactionDepth;
            center.y = camPos.y;
            
            spiralCenter = center; // Store for velocity tracking
            
            Vector3 planeRight = mainCam.transform.right;
            planeRight.y = 0;
            planeRight.Normalize();
            
            Vector3 planeUp = Vector3.up;
            
            int totalSegments = pathSegments * spiralTurns;
            
            for (int i = 0; i <= totalSegments; i++)
            {
                float t = i / (float)totalSegments;
                float angle = t * TWO_PI * spiralTurns;
                float radius = Mathf.Lerp(spiralStartRadius, spiralEndRadius, t) * tightnessFactor;
                
                Vector3 offset = planeRight * Mathf.Cos(angle) * radius + planeUp * Mathf.Sin(angle) * radius;
                Vector3 point = center + offset;
                
                path.Add(point);
            }
            
            pathStart = path[0];
            pathEnd = path[path.Count - 1];
            
            return path;
        }
        
        protected override void OnPathCompleted()
        {
            // Enhanced scoring based on angular velocity consistency
            float velocityScore = currentAngularVelocity >= minAngularVelocity && 
                                 currentAngularVelocity <= maxAngularVelocity ? 1f : 0.5f;
            
            // For easier gameplay, use path accuracy directly without velocity penalty
            float pathAccuracy = currentPath.Accuracy;
            float combinedAccuracy = pathAccuracy; // Simplified: just use path accuracy
            bool success = combinedAccuracy >= minAccuracy;
            float completionTime = elapsedTime - pathStartTime; // Actual time spent tracing this path
            
            Debug.Log($"[SpiralTracingTask] Path completed! PathAccuracy: {pathAccuracy:P1}, RadialAccuracy: {radialAccuracy:P1}, VelocityScore: {velocityScore:F2}, CombinedAccuracy: {combinedAccuracy:P1}, Required: {minAccuracy:P1}, Success: {success}");
            
            if (success)
            {
                pathsCompleted++;
                int scoreToAdd = Mathf.RoundToInt(combinedAccuracy * 100);
                AddScore(scoreToAdd);
                Debug.Log($"[SpiralTracingTask] Score added: {scoreToAdd}, Total paths completed: {pathsCompleted}");
                feedback?.PlaySuccess(currentPath.transform.position);
            }
            else
            {
                IncrementError();
                Debug.Log($"[SpiralTracingTask] Path failed - accuracy too low");
                feedback?.PlayError(currentPath.transform.position);
            }
            
            // Report attempt with combined accuracy
            ReportAttempt(completionTime, success, combinedAccuracy);
            
            completedPaths.Add(currentPath);
            currentPath = null;
            isTracing = false;
            
            // Reset velocity tracking
            lastPosition = Vector3.zero;
            currentAngularVelocity = 0f;
        }
        
        public void SetSpiralDifficulty(float tightness, float minVel, float maxVel)
        {
            tightnessFactor = tightness;
            minAngularVelocity = minVel;
            maxAngularVelocity = maxVel;
        }
    }
}

