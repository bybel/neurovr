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
    /// Mechanics: Fixed time duration (default 3s).
    /// </summary>
    public class PathTask : PathTracingTask
    {
        [Header("Path Task Settings")]
        [SerializeField] private int trialsPerSession = 5;
        [SerializeField] private float delayBetweenTrials = 1.0f;
        [SerializeField] private float trialDuration = 3.0f; // Configurable duration
        
        [Header("Table Calibration")]
        [SerializeField] private TableCalibrationManager calibrationManager;
        
        private int currentTrialIndex = 0;
        private bool isWaitingForNextTrial = false;
        
        // Input State
        private bool wasPressed = false;
        private bool isDebounced = false;
        private float lastPressTime = 0;
        private float lastReleaseTime = 0;

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
            wasPressed = false;
            
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
                    return; 
                }
            }
        }

        [Header("UI")]
        [SerializeField] private TMPro.TextMeshProUGUI timerText;

        protected override void UpdateTask()
        {
            // Debug Input status periodically
            if (Time.frameCount % 60 == 0)
            {
                 Debug.Log($"[PathTask] UpdateTask Frame: {Time.frameCount}. InputRaw: {inputHandler.IsStylusPressed}. Path: {currentPath != null}. TimerText: {timerText != null}");
            }

            // If calibrating, do nothing else
            if (calibrationManager != null && !calibrationManager.IsCalibrated && 
                adaptiveController != null && adaptiveController.CurrentLevel == DifficultyLevel.Easy)
            {
                if (Time.frameCount % 60 == 0) Debug.Log("[PathTask] Blocked by Calibration Check");
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
                    if (Time.frameCount % 60 == 0) Debug.Log("[PathTask] Session Complete");
                    if (timerText != null) timerText.text = "Finished";
                    return;
                }
            }
            
            if (currentPath == null) 
            {
                if (Time.frameCount % 60 == 0) Debug.Log("[PathTask] CurrentPath is NULL after generation attempt.");
                return;
            }

            // 2. Timer Logic
            float timeCheck = elapsedTime - pathStartTime;
            float timeLeft = Mathf.Max(0, trialDuration - timeCheck);
             
            // Update UI
            if (timerText != null)
            {
                timerText.text = $"{timeLeft:F1}s";
            }
            // Fallback: Create UI if missing
            else if (currentPath != null && Time.frameCount % 60 == 0) // Try once a second
            {
                 CreateTimerUI();
            }

            if (timeCheck > trialDuration)
            {
                Debug.Log($"[PathTask] Time limit reached. Elapsed: {elapsedTime:F2}, Start: {pathStartTime:F2}, Diff: {timeCheck:F2} > {trialDuration}");
                OnPathCompleted(false);
                return;
            }

            // 3. Custom Input Logic with DEBOUNCE
            // Debounce is critical to avoid micro-strokes that don't render.
            
            bool rawPressed = inputHandler.IsStylusPressed || inputHandler.IsPinching;
            
            if (rawPressed)
            {
                // DEBOUNCE PRESS
                if (!wasPressed)
                {
                    if (Time.time - lastReleaseTime > 0.05f) // 50ms debouce
                    {
                        wasPressed = true;
                        isDebounced = true;
                        Debug.Log("[PathTask] Stroke Started (Debounced)");
                        currentPath.StartNewStroke();
                    }
                }
                
                // Drawing
                if (isDebounced)
                {
                    if (currentPath != null)
                    {
                        // USE CORRECTED TRANSFORM (Fixes alignment)
                        GetCalculatedInkTransform(out Vector3 inputPos, out Quaternion rot);
                        currentPath.UpdateTracing(inputPos);
                    }
                }
            }
            else
            {
                // DEBOUNCE RELEASE
                if (wasPressed)
                {
                   if (Time.time - lastPressTime > 0.05f)
                   {
                       wasPressed = false;
                       isDebounced = false;
                       lastReleaseTime = Time.time;
                       Debug.Log("[PathTask] Stoke Ended (Debounced)");
                   }
                }
            }
            
            // Track last press time for release debounce
            if (rawPressed) lastPressTime = Time.time;
        }

        private void CreateTimerUI()
        {
             // Try to find specific Timer Text first
             GameObject tObj = GameObject.Find("TaskTimerText");
             if (tObj != null) 
             {
                 timerText = tObj.GetComponent<TMPro.TextMeshProUGUI>();
                 return;
             }

             // Fallback to searching all (in case name is slightly different or inactive)
             var texts = FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None);
             foreach(var t in texts) 
             {
                 if (t.name.ToLower().Contains("timer")) 
                 {
                     timerText = t;
                     return;
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
                PositionPathInFrontOfCamera(0.4f);
            }
            
            // Generate Points
            List<Vector3> pathPoints = GeneratePathPoints();
            
            // Instantiate
            GameObject pathObj = Instantiate(pathPrefab.gameObject);
            pathObj.transform.SetParent(null);
            
            pathObj.transform.position = Vector3.zero; 
            pathObj.transform.rotation = Quaternion.identity;
            
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
            }
            
            isTracing = true;

            // CRITICAL: Ensure Timer Reset
            pathStartTime = elapsedTime; 
            
            wasPressed = false; // Reset input state
            
            Debug.Log($"[PathTask] Generated Trial {currentTrialIndex + 1}/{trialsPerSession}. StartTime: {pathStartTime:F2}, Duration: {trialDuration}s");
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
            Vector3 center = calibrationManager.PlaneCenter;
            Quaternion rotation = calibrationManager.PlaneRotation;
            Vector2 zoneSize = calibrationManager.ZoneSize;
            
            int segments = pathSegments;
            List<Vector3> points = new List<Vector3>();
            
            float width = zoneSize.x * 0.8f;
            float height = zoneSize.y * 0.8f;
            
            Vector3 startLocal = new Vector3(-width * 0.4f, 0, -height * 0.2f);
            Vector3 endLocal = new Vector3(width * 0.4f, 0, height * 0.2f);
            
            Random.InitState(currentTrialIndex * 1337); 
            
            Vector3 cp1 = new Vector3(Random.Range(-width*0.3f, width*0.3f), 0, Random.Range(-height*0.3f, height*0.3f));
            
            Vector3 planeRight = rotation * Vector3.right;
            Vector3 planeForward = rotation * Vector3.forward;
            
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = CalculateBezierPoint(t, startLocal, cp1, endLocal);
                
                Vector3 worldP = center + (planeRight * p.x) + (planeForward * p.z);
                points.Add(worldP);
            }
            
            pathStart = points[0];
            pathEnd = points[points.Count - 1];
            
            return points;
        }

        private List<Vector3> GenerateAirPath()
        {
             // Use Center-to-Right logic from PositionPathInFrontOfCamera
             // GenerateCurve requires Start, End, and Control Point.
             
             // Control point: Midpoint + Up 10cm
             return PathGenerator.GenerateCurve(pathStart, pathEnd, 
                     (pathStart + pathEnd) * 0.5f + Vector3.up * 0.1f, pathSegments);
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

        protected override void OnPathCompleted(bool reachedEnd)
        {
            float completionTime = elapsedTime - pathStartTime;
            float timeLeft = trialDuration - completionTime;
            
            // STRICT CHECK: Only allow completion if timer is done OR if we explicitly force it (e.g. error)
            // If called early, LOG IT and RETURN (don't destroy path)
            if (timeLeft > 0.1f)
            {
                Debug.LogWarning($"[PathTask] OnPathCompleted called EARLY! TimeLeft: {timeLeft:F2}s. IGNORING completion.");
                return;
            }

            Debug.Log($"[PathTask] Path Completed ON TIME. Elapsed: {completionTime:F2}s, Duration: {trialDuration}s");

            // Check Accuracy
            float accuracy = currentPath.Accuracy;
            bool success = accuracy >= minAccuracy;
            
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
                IncrementError(); 
            }
            
            // Report
            ReportAttempt(completionTime, success, accuracy);
            
            // Cleanup - DESTROY or HIDE immediately (as requested)
            // User requested: "ink drawn by the user for this path should also disapear"
            // Destroying the path object (which contains the ink mesh) handles this.
            
            completedPaths.Add(currentPath);
            currentPath.gameObject.SetActive(false); 
            Destroy(currentPath.gameObject); // Destroy creates cleaner state
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
            
            // Center position based on Camera Forward
            Vector3 centerPos = mainCam.transform.position + (mainCam.transform.forward * depth); 
            
            // Total width of the path (user interaction width)
            // Reduced to 20cm (0.2f) for "Center of FOV" comfort (user requested center)
            float totalWidth = 0.2f; 
            float halfWidth = totalWidth * 0.5f;

            // Define Start and End symmetrically around the center
            // Start on Left, End on Right
            pathStart = centerPos - (mainCam.transform.right * halfWidth);
            pathEnd = centerPos + (mainCam.transform.right * halfWidth);

            // Reset task transform to world zero so points are absolute
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            
            Debug.Log($"[PathTask] Positioned path. Center: {centerPos}, Start: {pathStart}, End: {pathEnd}");
        }
    }
}
