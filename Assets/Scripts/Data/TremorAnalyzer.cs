using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using NeuroReachVR.Utils;

namespace NeuroReachVR.Data
{
    /// <summary>
    /// Analyzes kinematic data for tremor characteristics
    /// Calculates tremor frequency, amplitude, jerk metrics for Parkinson's assessment
    /// </summary>
    public class TremorAnalyzer : MonoBehaviour
    {
        [Header("Analysis Settings")]
        [SerializeField] private float minTremorFrequency = 3f; // Hz
        [SerializeField] private float maxTremorFrequency = 8f; // Hz (typical Parkinson's tremor: 4-6 Hz)
        [SerializeField] private int fftSize = 512;

        public TremorMetrics AnalyzeTremor(List<KinematicSample> samples)
        {
            if (samples == null || samples.Count < 30)
                return new TremorMetrics();

            var metrics = new TremorMetrics
            {
                sampleCount = samples.Count,
                duration = samples[samples.Count - 1].timestamp - samples[0].timestamp,
                jerkScore = KinematicsCalculator.CalculateJerk(samples),
                smoothness = KinematicsCalculator.CalculateSmoothness(samples),
                tremorFrequency = EstimateTremorFrequency(samples),
                tremorAmplitude = KinematicsCalculator.CalculateTremorAmplitude(samples),
                movementEfficiency = KinematicsCalculator.CalculateMovementEfficiency(samples)
            };

            return metrics;
        }

        // Removed duplicates - now using KinematicsCalculator.CalculateJerk() and CalculateSmoothness()

        private float EstimateTremorFrequency(List<KinematicSample> samples)
        {
            if (samples.Count < fftSize / 2)
                return 0f;

            // Extract velocity magnitudes over time
            List<float> velocityMagnitudes = new List<float>();
            for (int i = 0; i < Mathf.Min(samples.Count, fftSize); i++)
            {
                velocityMagnitudes.Add(samples[i].velocity.magnitude);
            }

            // Simple peak detection in velocity oscillations
            // (Proper implementation would use FFT for frequency domain analysis)
            float avgVelocity = velocityMagnitudes.Average();
            int zeroCrossings = 0;
            bool wasAbove = velocityMagnitudes[0] > avgVelocity;

            for (int i = 1; i < velocityMagnitudes.Count; i++)
            {
                bool isAbove = velocityMagnitudes[i] > avgVelocity;
                if (isAbove != wasAbove)
                {
                    zeroCrossings++;
                    wasAbove = isAbove;
                }
            }

            float duration = samples[Mathf.Min(samples.Count - 1, fftSize - 1)].timestamp - samples[0].timestamp;
            float frequency = (zeroCrossings / 2f) / duration; // Divide by 2 for full cycles

            // Clamp to expected tremor range
            return Mathf.Clamp(frequency, minTremorFrequency, maxTremorFrequency);
        }

        // Removed duplicates - now using KinematicsCalculator.CalculateTremorAmplitude() and CalculateMovementEfficiency()

        public string GetTremorSeverityDescription(TremorMetrics metrics)
        {
            if (metrics.jerkScore < 50f)
                return "Minimal tremor";
            else if (metrics.jerkScore < 100f)
                return "Mild tremor";
            else if (metrics.jerkScore < 200f)
                return "Moderate tremor";
            else
                return "Severe tremor";
        }

        public Color GetTremorSeverityColor(TremorMetrics metrics)
        {
            if (metrics.jerkScore < 50f)
                return Color.green;
            else if (metrics.jerkScore < 100f)
                return Color.yellow;
            else if (metrics.jerkScore < 200f)
                return new Color(1f, 0.5f, 0f); // Orange
            else
                return Color.red;
        }
    }

    [System.Serializable]
    public struct TremorMetrics
    {
        public int sampleCount;
        public float duration;
        public float jerkScore;
        public float smoothness;
        public float tremorFrequency; // Hz
        public float tremorAmplitude; // meters
        public float movementEfficiency; // 0-1

        public override string ToString()
        {
            return $"Tremor Analysis:\n" +
                   $"- Jerk Score: {jerkScore:F2}\n" +
                   $"- Smoothness: {smoothness:P0}\n" +
                   $"- Frequency: {tremorFrequency:F2} Hz\n" +
                   $"- Amplitude: {tremorAmplitude * 1000f:F2} mm\n" +
                   $"- Efficiency: {movementEfficiency:P0}";
        }
    }
}
