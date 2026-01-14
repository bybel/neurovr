using UnityEngine;
using System.Collections.Generic;
using NeuroReachVR.Input;

namespace NeuroReachVR.Input
{
    /// <summary>
    /// Manages the calibration of a physical table surface using a "Trace Zone" method.
    /// User traces the area they want to use. We calculate the plane and the bounds (center, size) from this trace.
    /// </summary>
    public class TableCalibrationManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float minTracePoints = 10;
        [SerializeField] private float minTraceDistance = 0.01f; // Min distance between points to register
        [SerializeField] private Color traceColor = Color.cyan;
        [SerializeField] private Color planeColor = new Color(0, 1, 1, 0.2f);
        
        [Header("References")]
        [SerializeField] private StylusInputManager stylusInput;
        
        private List<Vector3> tracePoints = new List<Vector3>();
        private LineRenderer traceLine;
        private GameObject planeVisual;
        
        private bool isCalibrating;
        private bool isTracing;
        private bool isCalibrated;
        
        // Calibration Results
        private Plane tablePlane;
        private Vector3 planeCenter;
        private Quaternion planeRotation;
        private Vector2 zoneSize; // Width (X) and Height (Y/Z) on the plane
        
        public bool IsCalibrated => isCalibrated;
        public Vector3 PlaneCenter => planeCenter;
        public Quaternion PlaneRotation => planeRotation;
        public Vector2 ZoneSize => zoneSize;
        
        // Event for when calibration is complete
        public System.Action OnCalibrationComplete;

        private void Start()
        {
            if (stylusInput == null)
                stylusInput = FindFirstObjectByType<StylusInputManager>();
        }

        public void StartCalibration()
        {
            Debug.Log($"[TableCalibration] StartCalibration called. StylusInput: {(stylusInput != null ? "Valid" : "NULL")}");
            ClearCalibration();
            isCalibrating = true;
            isTracing = false;
            Debug.Log("[TableCalibration] Calibration started. Please TRACE the area you want to use on the table.");
        }

        private void Update()
        {
            if (!isCalibrating || stylusInput == null) return;

            bool isPressed = stylusInput.IsPressed || stylusInput.IsButtonPressed;
            
            // DEBUG: Log input state every 60 frames to verify we are getting data
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[TableCalibration] Calibrating... Pressed: {isPressed} (Pressure: {stylusInput.IsPressed}, Button: {stylusInput.IsButtonPressed}), Pos: {stylusInput.Position}");
            }

            Vector3 currentPos = stylusInput.Position;

            if (isPressed)
            {
                if (!isTracing)
                {
                    // Start Tracing
                    StartTrace();
                }
                
                // Continue Tracing
                UpdateTrace(currentPos);
            }
            else
            {
                if (isTracing)
                {
                    // Stop Tracing and Process
                    EndTrace();
                }
            }
        }
        
        private List<GameObject> traceVisuals = new List<GameObject>();

        private void StartTrace()
        {
            isTracing = true;
            tracePoints.Clear();
            
            // Clear old visuals
            foreach (var obj in traceVisuals) Destroy(obj);
            traceVisuals.Clear();
            
            Debug.Log("[TableCalibration] Trace started.");
        }
        
        private void UpdateTrace(Vector3 pos)
        {
            // Add point if far enough from last point
            if (tracePoints.Count == 0 || Vector3.Distance(tracePoints[tracePoints.Count - 1], pos) > minTraceDistance)
            {
                tracePoints.Add(pos);
                
                // Create visual breadcrumb
                GameObject crumb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crumb.transform.position = pos;
                crumb.transform.localScale = Vector3.one * 0.01f; // 1cm spheres
                crumb.GetComponent<Renderer>().material.color = traceColor;
                Destroy(crumb.GetComponent<Collider>()); // No physics
                
                traceVisuals.Add(crumb);
            }
        }
        
        private void EndTrace()
        {
            isTracing = false;
            Debug.Log($"[TableCalibration] Trace ended. Collected {tracePoints.Count} points.");
            
            if (tracePoints.Count >= minTracePoints)
            {
                CompleteCalibration();
                // Keep visuals until we clear? Or clear them now?
                // Let's keep them so user sees what they drew until they start again or we finish.
                // Actually, CompleteCalibration creates a plane visual, so we can clear the crumbs to avoid clutter.
                foreach (var obj in traceVisuals) Destroy(obj);
                traceVisuals.Clear();
            }
            else
            {
                Debug.LogWarning("[TableCalibration] Not enough points collected. Please trace a larger area.");
                // Reset to allow retry
                tracePoints.Clear();
                foreach (var obj in traceVisuals) Destroy(obj);
                traceVisuals.Clear();
            }
        }

        private void CompleteCalibration()
        {
            if (tracePoints.Count < 3) return;

            // 1. Calculate Centroid
            Vector3 center = Vector3.zero;
            foreach (var p in tracePoints) center += p;
            center /= tracePoints.Count;

            // 2. Calculate Normal (Robust Fit)
            // We'll use the covariance matrix approach for the normal too, or just simple regression.
            // But for a table, snapping to Up is usually what users want unless it's a ramp.
            
            // Simple normal from covariance of 3D points? 
            // Let's try a simpler approach: Average cross product of segments to get a rough normal.
            Vector3 roughNormal = Vector3.zero;
            for (int i = 0; i < tracePoints.Count - 1; i++)
            {
                roughNormal += Vector3.Cross(tracePoints[i] - center, tracePoints[i+1] - center);
            }
            roughNormal = roughNormal.normalized;
            if (roughNormal.y < 0) roughNormal = -roughNormal;

            // Snap to Up if close (within 20 degrees) to fix hand jitter
            if (Vector3.Angle(roughNormal, Vector3.up) < 20f)
            {
                roughNormal = Vector3.up;
            }
            
            Vector3 normal = roughNormal;
            tablePlane = new Plane(normal, center);

            // 3. Calculate Rotation (Align to Trace Shape)
            // Project points to 2D plane (defined by normal)
            // We need a temporary basis to project
            Vector3 tempRight = Vector3.Cross(normal, Vector3.forward).normalized;
            if (tempRight.sqrMagnitude < 0.01f) tempRight = Vector3.Cross(normal, Vector3.right).normalized;
            Vector3 tempForward = Vector3.Cross(tempRight, normal).normalized;
            
            // Calculate 2D Covariance on this plane
            float xx = 0, xy = 0, yy = 0;
            foreach (var p in tracePoints)
            {
                Vector3 vec = p - center;
                float x = Vector3.Dot(vec, tempRight);
                float y = Vector3.Dot(vec, tempForward);
                xx += x * x;
                xy += x * y;
                yy += y * y;
            }
            
            // PCA Angle
            float angle = 0.5f * Mathf.Atan2(2 * xy, xx - yy);
            
            // Convert PCA angle back to World Rotation
            // The PCA angle is relative to our tempRight/tempForward basis
            Vector3 principalAxis = (tempRight * Mathf.Cos(angle) + tempForward * Mathf.Sin(angle)).normalized;
            
            // Ensure the principal axis points somewhat "forward" relative to the camera?
            // Or just leave it as the long axis of the shape.
            // Let's check if the user drew "wide" or "tall".
            // Usually we want the "Up" of the UI to be "Forward" away from user.
            // Let's align the rotation such that "Forward" is the axis closest to Camera Forward.
            
            Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            Vector3 axis2 = Vector3.Cross(normal, principalAxis);
            
            // We have two axes: principalAxis and axis2. One of them is likely the "Forward" direction.
            // Pick the one closest to Camera Forward projected on plane.
            Vector3 flatCamFwd = Vector3.ProjectOnPlane(camForward, normal).normalized;
            
            float dot1 = Mathf.Abs(Vector3.Dot(principalAxis, flatCamFwd));
            float dot2 = Mathf.Abs(Vector3.Dot(axis2, flatCamFwd));
            
            Vector3 finalForward = (dot1 > dot2) ? principalAxis : axis2;
            
            // Ensure it points away from camera, not towards
            if (Vector3.Dot(finalForward, flatCamFwd) < 0) finalForward = -finalForward;
            
            planeRotation = Quaternion.LookRotation(finalForward, normal);
            
            // 4. Calculate Bounds (OBB)
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            
            foreach (var p in tracePoints)
            {
                // Transform to local space of the calculated rotation
                Vector3 localP = Quaternion.Inverse(planeRotation) * (p - center);
                
                if (localP.x < minX) minX = localP.x;
                if (localP.x > maxX) maxX = localP.x;
                if (localP.z < minZ) minZ = localP.z;
                if (localP.z > maxZ) maxZ = localP.z;
            }
            
            float width = maxX - minX;
            float height = maxZ - minZ;
            zoneSize = new Vector2(width, height);
            
            // Re-center
            Vector3 localCenterOffset = new Vector3((minX + maxX) * 0.5f, 0, (minZ + maxZ) * 0.5f);
            planeCenter = center + (planeRotation * localCenterOffset);
            
            isCalibrated = true;
            isCalibrating = false;
            
            CreatePlaneVisual();
            
            Debug.Log($"[TableCalibration] Calibration Complete! Center: {planeCenter}, Size: {zoneSize}, Normal: {normal}");
            OnCalibrationComplete?.Invoke();
        }

        private void CreatePlaneVisual()
        {
            if (planeVisual != null) Destroy(planeVisual);

            planeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            planeVisual.transform.position = planeCenter;
            planeVisual.transform.rotation = planeRotation;
            // Scale to match the traced zone
            planeVisual.transform.localScale = new Vector3(zoneSize.x, 0.001f, zoneSize.y); 
            
            Material mat = planeVisual.GetComponent<Renderer>().material;
            mat.color = planeColor;
            
            Destroy(planeVisual.GetComponent<Collider>());
        }

        public void ClearCalibration()
        {
            isCalibrated = false;
            isCalibrating = false;
            isTracing = false;
            tracePoints.Clear();
            
            if (traceLine != null)
            {
                traceLine.positionCount = 0;
                Destroy(traceLine.gameObject);
                traceLine = null;
            }
            
            if (planeVisual != null) Destroy(planeVisual);
        }
        
        public Vector3 ProjectPointOnTable(Vector3 point)
        {
            if (!isCalibrated) return point;
            return tablePlane.ClosestPointOnPlane(point);
        }
    }
}

