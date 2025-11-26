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
        [SerializeField] private float spiralStartRadius = 0.2f;
        [SerializeField] private float spiralEndRadius = 0.8f;
        [SerializeField] private int spiralTurns = 3;
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
            Vector3 center = (pathStart + pathEnd) * 0.5f;
            
            int totalSegments = pathSegments * spiralTurns;
            
            for (int i = 0; i <= totalSegments; i++)
            {
                float t = i / (float)totalSegments;
                float angle = t * TWO_PI * spiralTurns;
                float radius = Mathf.Lerp(spiralStartRadius, spiralEndRadius, t) * tightnessFactor;
                
                Vector3 point = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
                
                path.Add(point);
            }
            
            return path;
        }
        
        protected override void OnPathCompleted()
        {
            // Enhanced scoring based on angular velocity consistency
            float velocityScore = currentAngularVelocity >= minAngularVelocity && 
                                 currentAngularVelocity <= maxAngularVelocity ? 1f : 0.5f;
            
            float combinedAccuracy = (currentPath.Accuracy + radialAccuracy) * 0.5f * velocityScore;
            bool success = combinedAccuracy >= minAccuracy;
            float completionTime = elapsedTime - pathStartTime; // Actual time spent tracing this path
            
            if (success)
            {
                pathsCompleted++;
                AddScore(Mathf.RoundToInt(combinedAccuracy * 100));
                feedback?.PlaySuccess(currentPath.transform.position);
            }
            else
            {
                IncrementError();
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

