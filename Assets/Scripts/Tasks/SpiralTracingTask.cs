using UnityEngine;
using System.Collections.Generic;

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
        
        private const float TWO_PI = 2f * Mathf.PI;
        
        private float currentAngularVelocity;
        private Vector3 lastPosition;
        private float lastAngle;
        private float radialAccuracy;
        
        public float AngularVelocity => currentAngularVelocity;
        public float RadialAccuracy => radialAccuracy;
        
        protected override void UpdateTask()
        {
            base.UpdateTask();
            
            // Support all input modes: Stylus press, Mouse click, or Hand pinch
            bool isPressed = inputHandler.IsStylusPressed || inputHandler.IsPinching;
            if (isTracing && isPressed)
                TrackAngularVelocity();
        }
        
        private void TrackAngularVelocity()
        {
            Vector3 currentPos = inputHandler.Position;
            Vector3 center = (pathStart + pathEnd) * 0.5f;
            
            Vector3 toCurrent = currentPos - center;
            float currentAngle = Mathf.Atan2(toCurrent.z, toCurrent.x);
            float currentRadius = toCurrent.magnitude;
            
            if (lastPosition != Vector3.zero)
            {
                float deltaAngle = Mathf.DeltaAngle(lastAngle * Mathf.Rad2Deg, currentAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                float deltaTime = Time.deltaTime;
                
                if (deltaTime > 0f)
                    currentAngularVelocity = Mathf.Abs(deltaAngle / deltaTime);
                
                // Calculate radial accuracy (how close to ideal spiral radius)
                float idealRadius = GetIdealRadiusAtAngle(currentAngle);
                radialAccuracy = 1f - Mathf.Clamp01(Mathf.Abs(currentRadius - idealRadius) / (spiralEndRadius - spiralStartRadius));
            }
            
            lastPosition = currentPos;
            lastAngle = currentAngle;
        }
        
        private float GetIdealRadiusAtAngle(float angle)
        {
            // Archimedean spiral: r = a + b*θ
            float totalAngle = TWO_PI * spiralTurns;
            float normalizedAngle = (angle % totalAngle) / totalAngle;
            return Mathf.Lerp(spiralStartRadius, spiralEndRadius, normalizedAngle);
        }
        
        protected override List<Vector3> GeneratePathPoints()
        {
            return GenerateSpiralPath();
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
            
            // Use the same interaction depth as SimulatorInput (1.5m)
            float interactionDepth = 1.5f;
            
            // Position spiral at a fixed point in front of the camera at eye level
            // This creates a predictable position that users can see and trace
            Vector3 camPos = mainCam.transform.position;
            Vector3 camForward = mainCam.transform.forward;
            
            // Flatten forward direction to horizontal (ignore camera pitch)
            Vector3 flatForward = new Vector3(camForward.x, 0, camForward.z);
            if (flatForward.sqrMagnitude < 0.01f)
                flatForward = Vector3.forward;
            flatForward.Normalize();
            
            // Center: in front of camera at same height (eye level)
            Vector3 center = camPos + flatForward * interactionDepth;
            // Keep Y at camera height so spiral is at eye level
            center.y = camPos.y;
            
            // Spiral plane: horizontal (XZ) plane at eye level
            // This matches how mouse Y maps to world Y more predictably
            Vector3 planeRight = mainCam.transform.right;
            planeRight.y = 0;
            planeRight.Normalize();
            
            Vector3 planeUp = Vector3.up; // Pure vertical
            
            Debug.Log($"[SpiralTracingTask] Positioned spiral at center: {center}, camY: {camPos.y}, forward: {flatForward}");
            
            int totalSegments = pathSegments * spiralTurns;
            
            for (int i = 0; i <= totalSegments; i++)
            {
                float t = i / (float)totalSegments;
                float angle = t * TWO_PI * spiralTurns;
                float radius = Mathf.Lerp(spiralStartRadius, spiralEndRadius, t) * tightnessFactor;
                
                // Create spiral in a vertical plane (using right for horizontal, up for vertical)
                Vector3 offset = planeRight * Mathf.Cos(angle) * radius + planeUp * Mathf.Sin(angle) * radius;
                Vector3 point = center + offset;
                
                path.Add(point);
            }
            
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

