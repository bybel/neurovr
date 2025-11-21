using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeuroReachVR.Data;

namespace NeuroReachVR.Utils
{
    /// <summary>
    /// Consolidated kinematic calculations - eliminates 3 duplicate implementations
    /// </summary>
    public static class KinematicsCalculator
    {
        public static float CalculateJerk(List<KinematicSample> samples)
        {
            if (samples == null || samples.Count < 2) return 0f;

            float totalJerk = 0f;
            int count = 0;

            for (int i = 1; i < samples.Count; i++)
            {
                float dt = samples[i].timestamp - samples[i - 1].timestamp;
                if (dt > 0f)
                {
                    Vector3 jerk = (samples[i].acceleration - samples[i - 1].acceleration) / dt;
                    totalJerk += jerk.magnitude;
                    count++;
                }
            }

            return count > 0 ? totalJerk / count : 0f;
        }

        public static float CalculateSmoothness(List<KinematicSample> samples)
        {
            float jerk = CalculateJerk(samples);
            // Normalize: 0-50 normal, 50-200 moderate tremor, 200+ severe
            return Mathf.Clamp01(1f - (jerk / 50f));
        }

        public static float CalculateTremorAmplitude(List<KinematicSample> samples)
        {
            if (samples == null || samples.Count < 2) return 0f;

            Vector3 avgPosition = Vector3.zero;
            foreach (var sample in samples)
                avgPosition += sample.position;
            avgPosition /= samples.Count;

            float sumSquaredDeviations = 0f;
            foreach (var sample in samples)
            {
                float deviation = Vector3.Distance(sample.position, avgPosition);
                sumSquaredDeviations += deviation * deviation;
            }

            return Mathf.Sqrt(sumSquaredDeviations / samples.Count);
        }

        public static float CalculateMovementEfficiency(List<KinematicSample> samples)
        {
            if (samples == null || samples.Count < 2) return 1f;

            float straightLine = Vector3.Distance(samples[0].position, samples[samples.Count - 1].position);
            float actualPath = 0f;

            for (int i = 1; i < samples.Count; i++)
                actualPath += Vector3.Distance(samples[i - 1].position, samples[i].position);

            return actualPath < 0.001f ? 1f : Mathf.Clamp01(straightLine / actualPath);
        }
    }
}
