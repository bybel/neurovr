using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NeuroReachVR.Data
{
    /// <summary>
    /// Calculates real-time performance metrics for analytics
    /// Movement smoothness, path deviation, completion rate, velocity, reaction time
    /// </summary>
    public static class PerformanceMetricsCalculator
    {
        private const float JERK_NORMALIZATION_FACTOR = 10f;
        private const float MOVEMENT_THRESHOLD = 0.01f;
        
        /// <summary>
        /// Calculate movement smoothness using jerk (rate of change of acceleration)
        /// Lower jerk = smoother movement
        /// </summary>
        public static float CalculateSmoothness(List<KinematicSample> samples)
        {
            if (samples.Count < 3) return 0f;
            
            float totalJerk = 0f;
            for (int i = 1; i < samples.Count - 1; i++)
            {
                Vector3 jerk = (samples[i + 1].acceleration - samples[i - 1].acceleration) / 
                               (samples[i + 1].timestamp - samples[i - 1].timestamp);
                totalJerk += jerk.magnitude;
            }
            
            float avgJerk = totalJerk / (samples.Count - 2);
            // Normalize: 0 = perfect smoothness, higher = less smooth
            return Mathf.Clamp01(avgJerk / JERK_NORMALIZATION_FACTOR);
        }
        
        /// <summary>
        /// Calculate RMS error for path deviation
        /// </summary>
        public static float CalculatePathDeviation(List<Vector3> actualPath, List<Vector3> targetPath)
        {
            if (actualPath.Count == 0 || targetPath.Count == 0) return float.MaxValue;
            
            float totalError = 0f;
            int count = Mathf.Min(actualPath.Count, targetPath.Count);
            
            for (int i = 0; i < count; i++)
            {
                float distance = Vector3.Distance(actualPath[i], targetPath[i]);
                totalError += distance * distance;
            }
            
            return Mathf.Sqrt(totalError / count);
        }
        
        /// <summary>
        /// Calculate average velocity magnitude
        /// </summary>
        public static float CalculateAverageVelocity(List<KinematicSample> samples)
        {
            if (samples.Count == 0) return 0f;
            
            return samples.Average(s => s.velocity.magnitude);
        }
        
        /// <summary>
        /// Calculate reaction time (time from task start to first movement)
        /// </summary>
        public static float CalculateReactionTime(List<KinematicSample> samples, float movementThreshold = MOVEMENT_THRESHOLD)
        {
            if (samples.Count == 0) return 0f;
            
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i].velocity.magnitude > movementThreshold)
                    return samples[i].timestamp - samples[0].timestamp;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Calculate completion rate (successful attempts / total attempts)
        /// </summary>
        public static float CalculateCompletionRate(int successfulAttempts, int totalAttempts)
        {
            if (totalAttempts == 0) return 0f;
            return successfulAttempts / (float)totalAttempts;
        }
        
        /// <summary>
        /// Calculate peak velocity
        /// </summary>
        public static float CalculatePeakVelocity(List<KinematicSample> samples)
        {
            if (samples.Count == 0) return 0f;
            return samples.Max(s => s.velocity.magnitude);
        }
        
        /// <summary>
        /// Calculate movement efficiency (distance traveled / straight-line distance)
        /// </summary>
        public static float CalculateMovementEfficiency(List<KinematicSample> samples)
        {
            if (samples.Count < 2) return 1f;
            
            float totalDistance = 0f;
            for (int i = 1; i < samples.Count; i++)
            {
                totalDistance += Vector3.Distance(samples[i].position, samples[i - 1].position);
            }
            
            float straightLineDistance = Vector3.Distance(samples[0].position, samples[samples.Count - 1].position);
            
            if (straightLineDistance <= 0f) return 1f;
            return straightLineDistance / totalDistance; // Higher = more efficient
        }
    }
}

