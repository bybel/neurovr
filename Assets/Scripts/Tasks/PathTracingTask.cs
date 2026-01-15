using UnityEngine;
using System.Collections.Generic;
using NeuroReachVR.Input;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Path tracing exercise for fine motor control training
    /// Uses stylus to trace generated paths with real-time accuracy feedback
    /// </summary>
    /// </summary>
    [DefaultExecutionOrder(100)] // Ensure this runs AFTER StylusVisualizer and InputHandler
    public class PathTracingTask : BaseTask
    {
        [Header("Path Generation")]
        [SerializeField] protected TraceablePath pathPrefab; // Protected so SpiralTracingTask can access it
        [SerializeField] protected PathType pathType = PathType.Curve;
        [SerializeField] protected Vector3 pathStart = new Vector3(0.5f, 1f, 1f);
        [SerializeField] protected Vector3 pathEnd = new Vector3(0.5f, 1f, 1f);
        [SerializeField] private float pathLength = 2f;
        
        [Header("Tracing Settings")]
        [SerializeField] protected float pathWidth = 0.0025f; // 0.25cm Target
        [SerializeField] protected int pathSegments = 100; // INCREASED from 5 to 100 for high-res validation
        [SerializeField] protected float minAccuracy = 0.5f; // Lowered from 0.7 for easier gameplay
        
        [Header("Ink Alignment")]
        [SerializeField] private Vector3 inkPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 inkRotationOffset = Vector3.zero;
        
        [Header("Feedback")]
        [SerializeField] protected TaskFeedback feedback;
        
        protected TraceablePath currentPath;
        protected List<TraceablePath> completedPaths;
        protected bool isTracing;
        protected int pathsCompleted;
        protected float pathStartTime; // Time when current path tracing began
        
        private NeuroReachVR.Visuals.StylusVisualizer stylusVisualizer;
        private NeuroReachVR.UI.VRUIInputManager vruiInputManager;
        private HUDManager hudManager;

        protected override void Start()
        {
            base.Start();
            
            stylusVisualizer = FindFirstObjectByType<NeuroReachVR.Visuals.StylusVisualizer>();
            vruiInputManager = FindFirstObjectByType<NeuroReachVR.UI.VRUIInputManager>();
            hudManager = FindFirstObjectByType<HUDManager>();
            
            // Cleaned up misleading logs - we use Manual Offsets now.
            
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

        // ... Skiped Logic ...
        
        private System.Collections.IEnumerator WaitAndSpawnNext()
        {
            // Wait 3 seconds with the Old Path still visible
             for (int i = 3; i > 0; i--)
            {
                if (hudManager != null) hudManager.SetProgressText($"Next Path in {i}...");
                else Debug.Log($"Next Path in {i}..."); // Fallback log
                yield return new WaitForSeconds(1.0f);
            }
             if (hudManager != null) hudManager.SetProgressText("");

            isTracing = false; // Stop drawing now

            // NOW destroy the old path
            if (currentPath != null)
            {
                Destroy(currentPath.gameObject);
                currentPath = null;
            }

            // And generate the new one
            isWaitingForNextTrial = false;
            GenerateNewPath();
        }
        
        [ContextMenu("Reset Ink Offsets")]
        public void ResetInkOffsets()
        {
            inkPositionOffset = Vector3.zero;
            inkRotationOffset = Vector3.zero;
            Debug.Log("[PathTracingTask] Offsets reset to zero.");
        }
        
        protected override void UpdateTask()
        {
            // 1. Ensure path exists (Visuals first!)
            if (currentPath == null && !isWaitingForNextTrial)
            {
                GenerateNewPath();
            }
            
            // 2. Check Input
            if (!inputHandler.HasValidInput)
            {
                 if (Time.frameCount % 120 == 0)
                    Debug.LogWarning($"[PathTracing] HasValidInput is FALSE. Mode: {inputHandler.CurrentMode}. Check connection or Input Actions.");
                return;
            }
            
            if (currentPath == null) return; 
            
            // 3. Check for Completion (Only if not already done/waiting)
            if (currentPath.IsComplete && !isWaitingForNextTrial)
            {
                OnPathCompleted(true); // Completed by reaching end
                // Ensure we continue to update visuals below so user can keep drawing!
            }
            // 4. Check for Timeout (New Requirement: Don't let users get stuck)
            else if (!isWaitingForNextTrial && (elapsedTime - pathStartTime > maxTrialDuration))
            {
                 OnPathCompleted(false); // Completed by Timeout (did not reach end)
            }
            
            // 5. Update Tracing (Visuals & Logic)
            // Allow drawing even if complete, as long as path exists (during the 3s wait)
            UpdateTracing();
        }
        
        protected virtual void GenerateNewPath()
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
            pathObj.transform.position = Vector3.zero;
            
            // RANDOM ROTATION (Y-Axis only)
            float randomY = UnityEngine.Random.Range(0f, 360f);
            pathObj.transform.rotation = Quaternion.Euler(0, randomY, 0);
            
            pathObj.transform.localScale = Vector3.one;
            
            currentPath = pathObj.GetComponent<TraceablePath>();
            
            if (currentPath == null)
            {
                Debug.LogError("[PathTracingTask] Instantiated path prefab is missing TraceablePath component!");
                Destroy(pathObj);
                return;
            }
            
            currentPath.InitializePath(pathPoints);
            // enforce 0.25cm for target path
            currentPath.SetPathWidth(0.0025f); 
            
            Debug.Log($"[PathTracingTask] Generated path from {pathStart} to {pathEnd} with {pathPoints.Count} points");
            
            isTracing = true;
            pathStartTime = elapsedTime; // Record when path tracing begins
            
            // RESET INPUT STATE
            isDebouncedPressed = false;
            lastPressTime = 0;
            lastReleaseTime = Time.time - 5f; // clear release debounce lockout
        }

        // ... Skiped Logic ...
        
        private bool isWaitingForNextTrial = false;

        [SerializeField] protected float maxTrialDuration = 30.0f; // 30 seconds max per path

        protected virtual void OnPathCompleted(bool reachedEnd)
        {
            if (isWaitingForNextTrial) return; // Prevent double completion

            float accuracy = currentPath.Accuracy;
            bool success = reachedEnd && accuracy >= minAccuracy;
            float completionTime = elapsedTime - pathStartTime; // Actual time spent tracing this path
            
            Debug.Log($"[PathTracingTask] Path Finished. ReachedEnd: {reachedEnd}, Accuracy: {accuracy:P1}, Success: {success}");
            
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
                // If timed out, show specific feedback?
                if (!reachedEnd) 
                {
                     if (hudManager != null) hudManager.SetProgressText("Time's Up!");
                }
                
                IncrementError();
                Debug.Log($"[PathTracingTask] Path failed - Accuracy low or Time out");
                feedback?.PlayError(currentPath.transform.position);
            }
            
            // Report attempt to adaptive difficulty system
            ReportAttempt(completionTime, success, accuracy);
            
            // DO NOT DESTROY YET. Leave it visible for feedback.
            isWaitingForNextTrial = true;
            
            // Start Delay for next path
            StartCoroutine(WaitAndSpawnNext());
        }
        

        
        /// <summary>
        /// Positions the path start and end points in front of the camera
        /// Uses the same projection method as SimulatorInput for accurate mouse interaction
        /// </summary>
        protected void PositionPathInFrontOfCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;
            
            // Use the same interaction depth as SimulatorInput (1.5m)
            float interactionDepth = 0.5f;
            
            // Get the center point by projecting the screen center
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            Ray centerRay = mainCam.ScreenPointToRay(screenCenter);
            Vector3 pathCenter = centerRay.GetPoint(interactionDepth);
            
            // Get left and right points by projecting screen edges
            // Use 30% from center to edge for comfortable reach
            float screenOffset = Screen.width * 0.3f;
            
            // Raise paths slightly (0.65 instead of 0.5) based on user feedback "too low"
            Vector3 screenLeft = new Vector3(Screen.width * 0.5f - screenOffset, Screen.height , 0);
            Vector3 screenRight = new Vector3(Screen.width * 0.5f + screenOffset, Screen.height , 0);
            
            Ray leftRay = mainCam.ScreenPointToRay(screenLeft);
            Ray rightRay = mainCam.ScreenPointToRay(screenRight);
            
            pathStart = leftRay.GetPoint(interactionDepth);
            pathEnd = rightRay.GetPoint(interactionDepth);
            
            Debug.Log($"[PathTracingTask] Positioned path at center: {pathCenter}, start: {pathStart}, end: {pathEnd}");
        }
        
        protected virtual List<Vector3> GeneratePathPoints()
        {
            // FORCE Resolution to prevent low-poly path issues (Inspector override fix)
            if (pathSegments < 50) pathSegments = 100;

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
        
        private bool inputWasPressed = false;

        [Header("Debug Visuals")]
        [SerializeField] private bool showDebugInk = false; // Disabled by default

        [SerializeField] private Vector3 debugGhostRotationOffset = new Vector3(90, 0, 0);
        private GameObject debugInkCursor;
        private GameObject debugInkStylus; // A "Ghost Stylus" to show rotation

        protected override void Update()
        {
            // Call base.Update to handle timers and UpdateTask()
            base.Update();
            
            // Debug Visuals: Show ALWAYS if enabled, regardless of task state
            if (showDebugInk) 
            {
                UpdateDebugVisuals();
            }
            else
            {
                if (debugInkCursor != null) debugInkCursor.SetActive(false);
                if (debugInkStylus != null) debugInkStylus.SetActive(false);
            }
        }

        private void UpdateDebugVisuals()
        {
            if (debugInkCursor != null) debugInkCursor.SetActive(false);
            
            // To properly update the ghost stylus (if we want it), we need the calculated transform
            GetCalculatedInkTransform(out Vector3 pos, out Quaternion rot);
            
            // 1. Ink Tip Cursor (Sphere) - DISABLED
            /*
            if (debugInkCursor == null)
            {
                debugInkCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugInkCursor.name = "DebugInkCursor";
                // ... setup ...
            }
            */
            
            // 2. Ink Stylus Ghost (Cylinder) - DISABLED (User feedback: "Trippy")
            /*
            if (debugInkStylus == null)
            {
                // Parent pivot container
                GameObject ghostPivot = new GameObject("DebugInkStylusPivot");
                
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.name = "GhostModel";
                Destroy(cylinder.GetComponent<Collider>());
                cylinder.transform.SetParent(ghostPivot.transform);
                
                // Align cylinder: Z-forward, extending back from pivot
                cylinder.transform.localScale = new Vector3(0.005f, 0.075f, 0.005f); 
                cylinder.transform.localRotation = Quaternion.Euler(debugGhostRotationOffset); 
                cylinder.transform.localPosition = new Vector3(0, 0, -0.075f);
                
                var renderer = cylinder.GetComponent<Renderer>();
                var shader = Shader.Find("Unlit/Color");
                if (!shader) shader = Shader.Find("Standard");
                renderer.material.shader = shader;
                renderer.material.color = Color.cyan; 
                
                debugInkStylus = ghostPivot;
            }
            
            debugInkStylus.SetActive(true);
            debugInkStylus.transform.position = pos;
            debugInkStylus.transform.rotation = rot;
            */
            
            // Allow live tuning of ghost offset
            /*
            Transform ghostModel = debugInkStylus.transform.Find("GhostModel");
            if (ghostModel != null)
                ghostModel.localRotation = Quaternion.Euler(debugGhostRotationOffset);
            */
        }

        protected void GetCalculatedInkTransform(out Vector3 position, out Quaternion rotation)
        {
            Vector3 inputPos = inputHandler.Position;
            Quaternion inputRot = inputHandler.Rotation; 
            
            // Apply Manual Offsets
            Quaternion offsetRot = Quaternion.Euler(inkRotationOffset);
            rotation = inputRot * offsetRot;
            
            // Apply Position Offset
            position = inputPos + (rotation * inkPositionOffset);
        }

        // Debounce variables
        private float lastPressTime = 0f;
        private float lastReleaseTime = 0f;
        private bool isDebouncedPressed = false;
        private const float DEBOUNCE_THRESHOLD = 0.05f; // reduced from 150ms to 50ms for faster strokes

        private void UpdateTracing()
        {
            if (!isTracing) return;
            
            // Raw input state
            bool rawPressed = inputHandler.IsStylusPressed || inputHandler.IsPinching || UnityEngine.Input.GetKey(KeyCode.Space);
            
            // Debounce Logic with Failsafe
            if (rawPressed)
            {
                // FAILSAFE: If raw input is persistent but debounce logic fails, force it after 0.5s
                if (!isDebouncedPressed && Time.time - lastReleaseTime > 0.5f) 
                {
                     isDebouncedPressed = true;
                     if (currentPath != null) currentPath.StartNewStroke();
                }

                lastPressTime = Time.time;
                if (!isDebouncedPressed)
                {
                    // Normal Debounce Entry
                    if (Time.time - lastReleaseTime > DEBOUNCE_THRESHOLD)
                    {
                        isDebouncedPressed = true;
                        Debug.Log("[PathTracing] Input Debounced STARTED > New Stroke");
                        if (currentPath != null)
                        {
                            currentPath.StartNewStroke();
                        }
                    }
                    else
                    {
                        // Bounce Re-entry
                        isDebouncedPressed = true; 
                         Debug.Log("[PathTracing] Input Debounced CONTINUED (Bounce)");
                    }
                }
            }
            else 
            {
                // Raw release
                if (isDebouncedPressed)
                {
                   if (Time.time - lastPressTime > DEBOUNCE_THRESHOLD)
                   {
                       isDebouncedPressed = false;
                       lastReleaseTime = Time.time;
                       Debug.Log("[PathTracing] Input Debounced ENDED");
                   }
                }
            }

            // Status Log - EVERY 30 FRAMES
            if (Time.frameCount % 30 == 0)
            {
                 Debug.Log($"[PathTracing] Raw: {rawPressed}, Debounced: {isDebouncedPressed}, Valid: {inputHandler.HasValidInput}, Path: {currentPath != null}");
            }

            // Use Debounced state for drawing
            if (isDebouncedPressed)
            {
                GetCalculatedInkTransform(out Vector3 inputPos, out Quaternion finalRot);

                if (currentPath != null)
                {
                    // FIX: Auto-break stroke if distance jumped too far (e.g. tracking glitch or rapid movement)
                    // This prevents "Connecting Lines" if debounce failed to catch a lift.
                    currentPath.UpdateTracing(inputPos);
                }
            }
            else if (rawPressed)
            {
                // Debug: Why is raw pressed but not debounced?
                // This happens during the "Wait" phase of the debounce or if logic is broken.
            }
            
            inputWasPressed = isDebouncedPressed;
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

