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
        
        // Track dynamically created materials to prevent memory leaks
        private readonly List<Material> createdMaterials = new List<Material>();

        public bool IsComplete => targetPath != null && targetPath.Count > 0 && currentSegment >= targetPath.Count - 1;
        public float Accuracy => CalculateAccuracy();
        public float Progress => (targetPath != null && targetPath.Count > 0) ? currentSegment / (float)targetPath.Count : 0f;
        public float AverageDeviation => deviationCount > 0 ? totalDeviation / deviationCount : 0f;

        private void Awake()
        {
            // Initialize lists first
            targetPath = new List<Vector3>();
            tracedPath = new List<Vector3>();
            
            // Try to get existing LineRenderer first, only add if not present
            if (targetLineRenderer == null)
            {
                targetLineRenderer = GetComponent<LineRenderer>();
                if (targetLineRenderer == null)
                {
                    targetLineRenderer = gameObject.AddComponent<LineRenderer>();
                }
                ConfigureLineRenderer(targetLineRenderer, targetColor);
            }

            if (tracedLineRenderer == null && showRealTimeFeedback)
            {
                // Check if a TracedPath child already exists
                Transform existingTracedPath = transform.Find("TracedPath");
                if (existingTracedPath != null)
                {
                    tracedLineRenderer = existingTracedPath.GetComponent<LineRenderer>();
                }
                
                if (tracedLineRenderer == null)
                {
                    GameObject tracedObj = new GameObject("TracedPath");
                    tracedObj.transform.SetParent(transform);
                    tracedLineRenderer = tracedObj.AddComponent<LineRenderer>();
                }
                ConfigureLineRenderer(tracedLineRenderer, correctColor);
            }
        }

        private void OnDestroy()
        {
            // Clean up dynamically created materials to prevent memory leaks
            foreach (Material material in createdMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
            createdMaterials.Clear();
        }

        private void ConfigureLineRenderer(LineRenderer renderer, Color color)
        {
            if (renderer == null) return;
            
            renderer.startWidth = pathWidth;
            renderer.endWidth = pathWidth;
            
            // Create material with fallback shaders (Shader.Find can return null in builds)
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("UI/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");
            
            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = color;
                createdMaterials.Add(material);
                renderer.material = material;
            }
            else
            {
                Debug.LogWarning("[TraceablePath] Could not find any shader for LineRenderer. Using default material.");
                // Use renderer's existing material or Unity's default
            }
            
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
            
            // Ensure tracedPath is initialized (might not be if Awake hasn't run yet)
            if (tracedPath == null)
                tracedPath = new List<Vector3>();
            else
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
                // Find the closest point on the path (not just the current segment)
                float minDeviation = float.MaxValue;
                int closestSegment = currentSegment;
                
                // Look ahead a few segments to allow for faster tracing
                int lookAhead = Mathf.Min(currentSegment + 10, targetPath.Count);
                for (int i = currentSegment; i < lookAhead; i++)
                {
                    float dist = Vector3.Distance(stylusPosition, targetPath[i]);
                    if (dist < minDeviation)
                    {
                        minDeviation = dist;
                        closestSegment = i;
                    }
                }

                // More generous tolerance: 3x path width or 0.3m, whichever is larger
                float tolerance = Mathf.Max(pathWidth * 3f, 0.3f);
                if (minDeviation < tolerance)
                {
                    // Only count deviation when we make progress (not every frame)
                    // This gives a more accurate representation of tracing quality
                    totalDeviation += minDeviation;
                    deviationCount++;
                    
                    // Progress to the closest segment we found
                    currentSegment = closestSegment + 1;
                    Debug.Log($"[TraceablePath] Progress: {currentSegment}/{targetPath.Count}, deviation: {minDeviation:F3}m");
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

            // Set positions for all traced points
            for (int i = 0; i < tracedPath.Count; i++)
            {
                tracedLineRenderer.SetPosition(i, tracedPath[i]);
            }

            // Create gradient for color feedback
            // Unity's Gradient supports a maximum of 8 color keys, so we sample at regular intervals
            const int maxGradientKeys = 8;
            Gradient gradient = new Gradient();
            
            int keyCount = Mathf.Min(tracedPath.Count, maxGradientKeys);
            GradientColorKey[] colorKeys = new GradientColorKey[keyCount];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

            for (int keyIndex = 0; keyIndex < keyCount; keyIndex++)
            {
                // Calculate the sample index in the traced path
                // For keyCount keys, we sample at evenly spaced intervals
                int sampleIndex;
                float gradientTime;
                
                if (keyCount == 1)
                {
                    sampleIndex = 0;
                    gradientTime = 0f;
                }
                else
                {
                    // Map key index to path index, ensuring we include first and last points
                    float t = keyIndex / (float)(keyCount - 1);
                    sampleIndex = Mathf.RoundToInt(t * (tracedPath.Count - 1));
                    gradientTime = t;
                }

                // Determine color based on deviation at this sample point
                float deviation = sampleIndex < targetPath.Count ?
                    Vector3.Distance(tracedPath[sampleIndex], targetPath[sampleIndex]) : float.MaxValue;

                Color pointColor = deviation < pathWidth ? correctColor : errorColor;
                colorKeys[keyIndex] = new GradientColorKey(pointColor, gradientTime);
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
