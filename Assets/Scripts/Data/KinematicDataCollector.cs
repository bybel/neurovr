using UnityEngine;
using System.Collections.Generic;
using NeuroReachVR.Input;

namespace NeuroReachVR.Data
{
    /// <summary>
    /// Collects kinematic data: position, velocity, acceleration, joint angles
    /// Tracks movement for smoothness and deviation calculations
    /// NOW WITH: 60Hz sampling, improved velocity calculation, tremor filtering
    /// </summary>
    public class KinematicDataCollector : MonoBehaviour
    {
        [Header("Collection Settings")]
        [SerializeField] private float sampleRate = 60f; // Increased for tremor detection
        [SerializeField] private int maxSamples = 3000; // Increased for longer recording
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float smoothingFactor = 0.3f; // Lower = more smoothing

        private InputHandler inputHandler;
        private List<KinematicSample> samples;
        private float lastSampleTime;
        private float sampleInterval;
        private Vector3 lastPosition;
        private Vector3 lastVelocity;
        private Vector3 smoothedPosition;
        private bool isFirstSample = true;

        public int SampleCount => samples.Count;
        // Return reference to avoid copying every time - caller should not modify
        public List<KinematicSample> Samples => samples;

        private void Awake()
        {
            inputHandler = FindFirstObjectByType<InputHandler>(FindObjectsInactive.Include);
            samples = new List<KinematicSample>();
            sampleInterval = 1f / sampleRate;
        }

        private void Update()
        {
            if (inputHandler == null || !inputHandler.HasValidInput) return;

            if (Time.time - lastSampleTime >= sampleInterval)
            {
                CollectSample();
                lastSampleTime = Time.time;
            }
        }

        private void CollectSample()
        {
            Vector3 rawPosition = inputHandler.Position;
            Vector3 position = enableSmoothing ? ApplySmoothing(rawPosition) : rawPosition;
            Vector3 velocity = CalculateVelocity(position);
            Vector3 acceleration = CalculateAcceleration(velocity);

            var sample = new KinematicSample
            {
                timestamp = Time.time,
                position = position,
                velocity = velocity,
                acceleration = acceleration,
                rotation = inputHandler.Rotation
            };

            samples.Add(sample);

            // Limit sample count (FIFO queue behavior)
            if (samples.Count > maxSamples)
                samples.RemoveAt(0);

            lastPosition = position;
            lastVelocity = velocity;
        }

        private Vector3 ApplySmoothing(Vector3 rawPosition)
        {
            if (isFirstSample)
            {
                isFirstSample = false;
                smoothedPosition = rawPosition;
                return rawPosition;
            }

            // Exponential moving average filter for tremor compensation
            smoothedPosition = Vector3.Lerp(smoothedPosition, rawPosition, smoothingFactor);
            return smoothedPosition;
        }

        private Vector3 CalculateVelocity(Vector3 currentPosition)
        {
            // Check if this is the first sample
            if (samples.Count == 0)
                return Vector3.zero;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return lastVelocity;

            return (currentPosition - lastPosition) / deltaTime;
        }

        private Vector3 CalculateAcceleration(Vector3 currentVelocity)
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return Vector3.zero;

            return (currentVelocity - lastVelocity) / deltaTime;
        }

        public void Clear()
        {
            samples.Clear();
            lastPosition = Vector3.zero;
            lastVelocity = Vector3.zero;
            smoothedPosition = Vector3.zero;
            isFirstSample = true;
        }

        public KinematicSample GetLatestSample()
        {
            return samples.Count > 0 ? samples[samples.Count - 1] : default;
        }

        public Vector3 GetSmoothedPosition()
        {
            return smoothedPosition;
        }

        public void SetSmoothingEnabled(bool enabled)
        {
            enableSmoothing = enabled;
        }

        public void SetSmoothingFactor(float factor)
        {
            smoothingFactor = Mathf.Clamp01(factor);
        }
    }

    [System.Serializable]
    public struct KinematicSample
    {
        public float timestamp;
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public Quaternion rotation;
    }
}
