using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Represents a path that can be traced by the user
    /// Tracks progress and accuracy in real-time
    /// NOW WITH: Real-time visual feedback, accuracy clamping, separate traced line renderer
    /// </summary>
    public class TraceablePath : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private float pathWidth = 0.1f;
        [SerializeField] private Color targetColor = Color.blue;
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color errorColor = Color.red;

        [Header("Line Renderers")]
        [SerializeField] private LineRenderer targetLineRenderer;
        [SerializeField] private LineRenderer tracedLineRenderer;
        [SerializeField] private bool showRealTimeFeedback = true;

        private List<Vector3> targetPath;
        private List<Vector3> tracedPath;
        private bool isActive;
        private int currentSegment;
        private float totalDeviation;
        private int deviationCount;

        public bool IsComplete => currentSegment >= targetPath.Count - 1;
        public float Accuracy => CalculateAccuracy();
        public float Progress => targetPath.Count > 0 ? currentSegment / (float)targetPath.Count : 0f;
        public float AverageDeviation => deviationCount > 0 ? totalDeviation / deviationCount : 0f;

        private void Awake()
        {
            if (targetLineRenderer == null)
            {
                targetLineRenderer = gameObject.AddComponent<LineRenderer>();
                ConfigureLineRenderer(targetLineRenderer, targetColor);
            }

            if (tracedLineRenderer == null && showRealTimeFeedback)
            {
                GameObject tracedObj = new GameObject("TracedPath");
                tracedObj.transform.SetParent(transform);
                tracedLineRenderer = tracedObj.AddComponent<LineRenderer>();
                ConfigureLineRenderer(tracedLineRenderer, correctColor);
            }

            tracedPath = new List<Vector3>();
        }

        private void ConfigureLineRenderer(LineRenderer renderer, Color color)
        {
            renderer.startWidth = pathWidth;
            renderer.endWidth = pathWidth;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.startColor = color;
            renderer.endColor = color;
            renderer.useWorldSpace = true;
        }

        public void InitializePath(List<Vector3> path)
        {
            if (path == null || path.Count == 0)
            {
                Debug.LogError("[TraceablePath] Cannot initialize with null or empty path");
                return;
            }

            targetPath = new List<Vector3>(path);
            tracedPath.Clear();
            currentSegment = 0;
            totalDeviation = 0f;
            deviationCount = 0;
            isActive = true;

            RenderTargetPath();

            if (showRealTimeFeedback && tracedLineRenderer != null)
            {
                tracedLineRenderer.positionCount = 0;
            }
        }

        public void UpdateTracing(Vector3 stylusPosition)
        {
            if (!isActive || targetPath == null || targetPath.Count == 0) return;

            tracedPath.Add(stylusPosition);

            if (currentSegment < targetPath.Count)
            {
                float deviation = Vector3.Distance(stylusPosition, targetPath[currentSegment]);
                totalDeviation += deviation;
                deviationCount++;

                if (deviation < pathWidth * 1.5f) // Allow some tolerance
                {
                    currentSegment++;
                }
            }

            if (showRealTimeFeedback)
            {
                RenderProgress();
            }
        }

        private void RenderTargetPath()
        {
            if (targetLineRenderer == null || targetPath == null || targetPath.Count == 0) return;

            targetLineRenderer.positionCount = targetPath.Count;
            targetLineRenderer.startWidth = pathWidth;
            targetLineRenderer.endWidth = pathWidth;
            targetLineRenderer.startColor = targetColor;
            targetLineRenderer.endColor = targetColor;

            for (int i = 0; i < targetPath.Count; i++)
                targetLineRenderer.SetPosition(i, targetPath[i]);
        }

        private void RenderProgress()
        {
            if (tracedLineRenderer == null || tracedPath.Count == 0) return;

            tracedLineRenderer.positionCount = tracedPath.Count;

            // Create gradient for color feedback
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[tracedPath.Count];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

            for (int i = 0; i < tracedPath.Count; i++)
            {
                tracedLineRenderer.SetPosition(i, tracedPath[i]);

                // Determine color based on deviation
                float deviation = i < targetPath.Count ?
                    Vector3.Distance(tracedPath[i], targetPath[i]) : float.MaxValue;

                Color pointColor = deviation < pathWidth ? correctColor : errorColor;
                colorKeys[i] = new GradientColorKey(pointColor, i / (float)tracedPath.Count);
            }

            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            tracedLineRenderer.colorGradient = gradient;
        }

        private float CalculateAccuracy()
        {
            if (deviationCount == 0) return 1f;

            float avgDeviation = totalDeviation / deviationCount;
            float accuracy = 1f - Mathf.Clamp01(avgDeviation / pathWidth);

            // Clamp to 0-1 range
            return Mathf.Clamp01(accuracy);
        }

        public void ResetPath()
        {
            tracedPath.Clear();
            currentSegment = 0;
            totalDeviation = 0f;
            deviationCount = 0;
            isActive = false;

            if (tracedLineRenderer != null)
                tracedLineRenderer.positionCount = 0;
        }

        public float GetDeviationAt(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= tracedPath.Count || segmentIndex >= targetPath.Count)
                return float.MaxValue;

            return Vector3.Distance(tracedPath[segmentIndex], targetPath[segmentIndex]);
        }

        public void SetShowRealTimeFeedback(bool show)
        {
            showRealTimeFeedback = show;
            if (tracedLineRenderer != null)
                tracedLineRenderer.enabled = show;
        }

        public void SetPathWidth(float width)
        {
            pathWidth = Mathf.Max(0.01f, width);

            if (targetLineRenderer != null)
            {
                targetLineRenderer.startWidth = pathWidth;
                targetLineRenderer.endWidth = pathWidth;
            }

            if (tracedLineRenderer != null)
            {
                tracedLineRenderer.startWidth = pathWidth * 0.8f; // Slightly thinner
                tracedLineRenderer.endWidth = pathWidth * 0.8f;
            }
        }
    }
}
