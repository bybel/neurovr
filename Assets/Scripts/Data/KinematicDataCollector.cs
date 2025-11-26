using UnityEngine;
using System.Collections.Generic;
using NeuroReachVR.Input;

namespace NeuroReachVR.Data
{
    /// <summary>
    /// Collects kinematic data: position, velocity, acceleration, joint angles
    /// Tracks movement for smoothness and deviation calculations
    /// NOW WITH: 60Hz sampling, improved velocity calculation, tremor filtering
    /// Uses Queue<T> for O(1) FIFO operations instead of List.RemoveAt(0) which is O(n)
    /// </summary>
    public class KinematicDataCollector : MonoBehaviour
    {
        [Header("Collection Settings")]
        [SerializeField] private float sampleRate = 60f; // Increased for tremor detection
        [SerializeField] private int maxSamples = 3000; // Increased for longer recording
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float smoothingFactor = 0.3f; // Lower = more smoothing

        private InputHandler inputHandler;
        private Queue<KinematicSample> samplesQueue; // Queue for O(1) enqueue/dequeue
        private KinematicSample latestSample; // Cache latest sample for O(1) access
        private float lastSampleTime;
        private float sampleInterval;
        private Vector3 lastPosition;
        private Vector3 lastVelocity;
        private Vector3 smoothedPosition;
        private bool isFirstSample = true;
        private bool needsPositionInit = true; // Track when lastPosition needs initialization

        public int SampleCount => samplesQueue.Count;
        // Convert to list only when needed (called at task end for analysis)
        // This is acceptable since it's only called once per task completion
        public List<KinematicSample> Samples => new List<KinematicSample>(samplesQueue);

        private void Awake()
        {
            inputHandler = FindFirstObjectByType<InputHandler>(FindObjectsInactive.Include);
            samplesQueue = new Queue<KinematicSample>(maxSamples + 1); // Pre-allocate capacity
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

            samplesQueue.Enqueue(sample); // O(1) operation
            latestSample = sample; // Cache for O(1) GetLatestSample()

            // Limit sample count (FIFO queue behavior) - O(1) dequeue vs O(n) RemoveAt(0)
            while (samplesQueue.Count > maxSamples)
                samplesQueue.Dequeue();

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
            // Initialize lastPosition from actual hand position on first sample
            // This prevents velocity spikes from using Vector3.zero as the starting point
            if (needsPositionInit)
            {
                lastPosition = currentPosition;
                needsPositionInit = false;
                return Vector3.zero;
            }

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
            samplesQueue.Clear();
            latestSample = default;
            // Don't reset lastPosition to Vector3.zero - this caused velocity spikes
            // when the first sample calculated (currentPosition - Vector3.zero) / deltaTime
            // Instead, use needsPositionInit flag to initialize from actual hand position
            needsPositionInit = true;
            lastVelocity = Vector3.zero;
            smoothedPosition = Vector3.zero;
            isFirstSample = true;
        }

        public KinematicSample GetLatestSample()
        {
            // Return cached latest sample for O(1) access
            return samplesQueue.Count > 0 ? latestSample : default;
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
