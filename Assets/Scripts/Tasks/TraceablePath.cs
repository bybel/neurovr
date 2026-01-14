using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Represents a path that can be traced by the user
    /// Tracks progress and accuracy in real-time
    /// NOW WITH: 3D Tube Rendering for user traces and multi-stroke support
    /// </summary>
    [RequireComponent(typeof(LineRenderer))] // For the target path
    public class TraceablePath : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private float pathWidth = 0.005f; // reduced from 0.04 to 0.005
        [SerializeField] private Color targetColor = Color.blue;
        [SerializeField] private Color correctColor = Color.green;
        [SerializeField] private Color errorColor = Color.red;

        // ... (Header items)





        [Header("Trace Settings")]
        [SerializeField] private float traceWidth = 0.005f; // Match pathWidth (0.5cm)

        [Header("Components")]
        [SerializeField] private LineRenderer targetLineRenderer;
        
        // REPLACED: tracedLineRenderer with Mesh components for 3D Tube
        [SerializeField] private MeshFilter tracedMeshFilter;
        [SerializeField] private MeshRenderer tracedMeshRenderer;
        
        [SerializeField] private bool showRealTimeFeedback = true;
        [SerializeField] private int tubeSegments = 8; // Radial segments for the tube

        private List<Vector3> targetPath;
        
        // CHANGE: data structure for multiple disconnected strokes
        private List<List<Vector3>> strokes = new List<List<Vector3>>();
        private List<Vector3> currentStroke;
        
        private bool isActive;
        private int currentSegment;
        private float totalDeviation;
        private int deviationCount;
        
        private Mesh tracedMesh;
        private List<Vector3> meshVertices = new List<Vector3>();
        private List<int> meshTriangles = new List<int>();
        private List<Color> meshColors = new List<Color>();
        private List<Vector3> meshNormals = new List<Vector3>(); // Optional for better lighting

        public bool IsComplete => targetPath != null && targetPath.Count > 0 && currentSegment >= targetPath.Count - 1;
        public float Accuracy => CalculateAccuracy();
        public float Progress => (targetPath != null && targetPath.Count > 0) ? currentSegment / (float)targetPath.Count : 0f;
        public float AverageDeviation => deviationCount > 0 ? totalDeviation / deviationCount : 0f;

        [SerializeField] private MeshFilter targetMeshFilter;
        [SerializeField] private MeshRenderer targetMeshRenderer;
        private Mesh targetMesh;

        // ... (Code continues) ...

        private void Awake()
        {
            // Initialize lists
            targetPath = new List<Vector3>();
            strokes = new List<List<Vector3>>();
            
            // Disable legacy LineRenderer if present
            if (targetLineRenderer != null) targetLineRenderer.enabled = false;

            // Setup Target Mesh Components FIRST
            SetupTargetMesh();
            
            // Setup Traced Mesh Components
            if (showRealTimeFeedback)
            {
               SetupTracedMesh();
            }

            // FORCE small width for now to fix persistent huge paths
            // Now that meshes are setup, this will actually render!
            SetPathWidth(0.0025f); // 0.25cm (Target)
            traceWidth = 0.005f; // 0.5cm (Trace - kept as requested)
        }
        
        private void SetupTargetMesh()
        {
             Transform existingObj = transform.Find("TargetPathMesh");
             GameObject obj;
             if (existingObj != null) obj = existingObj.gameObject;
             else 
             {
                 obj = new GameObject("TargetPathMesh");
                 obj.transform.SetParent(transform);
                 obj.transform.localPosition = Vector3.zero;
                 obj.transform.localRotation = Quaternion.identity;
                 obj.transform.localScale = Vector3.one;
             }
             
             targetMeshFilter = obj.GetComponent<MeshFilter>();
             if (!targetMeshFilter) targetMeshFilter = obj.AddComponent<MeshFilter>();
             
             targetMeshRenderer = obj.GetComponent<MeshRenderer>();
             if (!targetMeshRenderer) targetMeshRenderer = obj.AddComponent<MeshRenderer>();
             
             targetMesh = new Mesh();
             targetMesh.name = "TargetPathMesh";
             targetMeshFilter.mesh = targetMesh;
             
             // Material
             Shader shader = Shader.Find("Standard"); 
             if (!shader) shader = Shader.Find("Mobile/Diffuse");
             var material = new Material(shader);
             material.color = targetColor;
             // Make it semi-transparent ghost? Or solid? User said "same as ink". Ink is solid.
             // Solid blue is good.
             targetMeshRenderer.material = material;
        }

        private void SetupTracedMesh()
        {
            Transform existingTracedObj = transform.Find("TracedPathMesh");
                GameObject tracedObj;
                
                if (existingTracedObj != null)
                {
                    tracedObj = existingTracedObj.gameObject;
                }
                else
                {
                    tracedObj = new GameObject("TracedPathMesh");
                    tracedObj.transform.SetParent(transform);
                    tracedObj.transform.localPosition = Vector3.zero;
                    tracedObj.transform.localRotation = Quaternion.identity;
                    tracedObj.transform.localScale = Vector3.one;
                }

                tracedMeshFilter = tracedObj.GetComponent<MeshFilter>();
                if (tracedMeshFilter == null) tracedMeshFilter = tracedObj.AddComponent<MeshFilter>();
                
                tracedMeshRenderer = tracedObj.GetComponent<MeshRenderer>();
                if (tracedMeshRenderer == null) tracedMeshRenderer = tracedObj.AddComponent<MeshRenderer>();
                
                // Initialize Mesh
                tracedMesh = new Mesh();
                tracedMesh.name = "TracedPathMesh";
                tracedMeshFilter.mesh = tracedMesh;
                
                // Configure Material for Mesh
                Shader shader = Shader.Find("Standard"); 
                if (shader == null) shader = Shader.Find("Diffuse");
                
                if (shader != null)
                {
                    Material material = new Material(shader);
                    // Use a shader that supports vertex colors if we want green/red
                    // Or just default to Green/Ink Color
                    material.color = correctColor; 
                    tracedMeshRenderer.material = material;
                }
        }

        public void InitializePath(List<Vector3> path)
        {
            if (path == null || path.Count == 0) return;

            targetPath = new List<Vector3>(path);
            ResetPath(); // Clears strokes
            
            RenderTargetPathTube();
        }
        
        /// <summary>
        /// Call this when the pen touches the paper/button is pressed to start a new disconnected segment.
        /// </summary>
        public void StartNewStroke()
        {
            if (!isActive || !showRealTimeFeedback) return;
            
            // Force break current stroke if it exists
            if (currentStroke != null)
            {
               currentStroke = null;
            }
            
            Debug.Log($"[TraceablePath] StartNewStroke called! Current strokes count: {strokes.Count}");
            currentStroke = new List<Vector3>();
            strokes.Add(currentStroke);
        }



        public void SetAlignment(LineAlignment alignment)
        {
            // Mesh doesn't use alignment, it's 3D geometry.
        }

        // --- Render Target Path as Tube ---
        private void RenderTargetPathTube()
        {
            if (targetMesh == null || targetPath == null || targetPath.Count < 2) return;
            
            Debug.Log($"[TraceablePath] Rendering Target Tube with Width: {pathWidth} (Radius: {pathWidth*0.5f})");
            
            // Treat target path as a single stroke
            List<List<Vector3>> tempStrokes = new List<List<Vector3>> { targetPath };
            
            GenerateTubeMesh(targetMesh, tempStrokes, pathWidth * 0.5f, targetColor);
        }

        public void UpdateTracing(Vector3 worldPosition)
        {
            if (!isActive || targetPath == null || targetPath.Count == 0) return;

            // Ensure we have a stroke to add to
            if (currentStroke == null)
            {
                Debug.LogWarning("[TraceablePath] UpdateTracing called but currentStroke is null. Auto-starting stroke.");
                StartNewStroke();
            }
            
            // Avoid adding duplicate points too close together (optimization)
            // Use worldPosition for distance check to avoid error if localPosition isn't computed yet?
            // Actually, let's compute localPosition first.
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            if (currentStroke.Count > 0 && Vector3.Distance(currentStroke[currentStroke.Count - 1], localPosition) < 0.001f)
            {
                return;
            }

            currentStroke.Add(localPosition);

            // Logic for Accuracy/Progress (same as before)
            // We verify against the target properties using the stylus position
            if (currentSegment < targetPath.Count)
            {
                float minDeviation = float.MaxValue;
                int closestSegment = currentSegment;
                int lookAhead = Mathf.Min(currentSegment + 10, targetPath.Count);
                for (int i = currentSegment; i < lookAhead; i++)
                {
                    // Use localPosition for distance check against targetPath (which is Local)
                    float dist = Vector3.Distance(localPosition, targetPath[i]);
                    if (dist < minDeviation)
                    {
                        minDeviation = dist;
                        closestSegment = i;
                    }
                }

                // FIX: Tolerance was 0.3f (30cm), causing instant completion!
                // Reduced to Max(width*3, 2cm)
                float tolerance = Mathf.Max(pathWidth * 3f, 0.02f);
                
                if (minDeviation < tolerance)
                {
                    totalDeviation += minDeviation;
                    deviationCount++;
                    currentSegment = closestSegment + 1;
                }
            }

            if (showRealTimeFeedback)
            {
                UpdateMesh();
            }
        }
        
        // --- Tube Mesh Generation ---

        private void UpdateMesh()
        {
            if (tracedMesh == null) return;
             // USE TRACE WIDTH HERE
            float radius = traceWidth * 0.5f;
            GenerateTubeMesh(tracedMesh, strokes, radius, correctColor, true);
        }

        private void GenerateTubeMesh(Mesh mesh, List<List<Vector3>> strokesToRender, float radius, Color color, bool useVertexColors = false)
        {
            mesh.Clear();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();
            List<Vector3> normals = new List<Vector3>();

            foreach (var stroke in strokesToRender)
            {
                if (stroke.Count < 2) continue;

                int startIndex = vertices.Count;

                for (int i = 0; i < stroke.Count; i++)
                {
                    Vector3 currentPoint = stroke[i];
                    
                    // Calculate frame
                    Vector3 forward;
                    if (i < stroke.Count - 1) forward = (stroke[i + 1] - currentPoint).normalized;
                    else forward = (currentPoint - stroke[i - 1]).normalized;
                    
                    if (forward == Vector3.zero) forward = Vector3.forward;

                    Vector3 up = Vector3.up; 
                    if (Mathf.Abs(Vector3.Dot(forward, up)) > 0.9f) up = Vector3.right;
                    
                    Vector3 right = Vector3.Cross(forward, up).normalized;
                    up = Vector3.Cross(right, forward).normalized;

                    // Generate ring vertices
                    for (int j = 0; j <= tubeSegments; j++) 
                    {
                        float angle = j * Mathf.PI * 2f / tubeSegments;
                        Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
                        
                        vertices.Add(currentPoint + offset);
                        normals.Add(offset.normalized); 

                        // Color
                        colors.Add(color);
                    }
                }

                // Generate Triangles
                int ringSize = tubeSegments + 1;
                for (int i = 0; i < stroke.Count - 1; i++)
                {
                    int baseIndex = startIndex + i * ringSize;
                    for (int j = 0; j < tubeSegments; j++)
                    {
                        int current = baseIndex + j;
                        int next = baseIndex + j + 1;
                        int currentNextRing = baseIndex + ringSize + j;
                        int nextNextRing = baseIndex + ringSize + j + 1;

                        triangles.Add(current);
                        triangles.Add(nextNextRing);
                        triangles.Add(next);

                        triangles.Add(current);
                        triangles.Add(currentNextRing);
                        triangles.Add(nextNextRing);
                    }
                }
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors.ToArray();
            mesh.normals = normals.ToArray();
            mesh.RecalculateBounds();
        }
        private float CalculateAccuracy()
        {
            if (deviationCount == 0 || pathWidth <= 0) return 1f;
            float avgDeviation = totalDeviation / deviationCount;
            // Simple linear falloff: 0 deviation = 100%, pathWidth deviation = 0%
            return Mathf.Clamp01(1f - (avgDeviation / pathWidth));
        }

        public void ResetPath()
        {
            strokes.Clear();
            currentStroke = null;
            if (tracedMesh != null) tracedMesh.Clear();
            if (targetMesh != null) targetMesh.Clear();
            
            isActive = true;
            currentSegment = 0;
            totalDeviation = 0;
            deviationCount = 0;
        }

        public void SetShowRealTimeFeedback(bool show)
        {
            showRealTimeFeedback = show;
            if (tracedMeshRenderer != null) tracedMeshRenderer.enabled = show;
        }

        public void SetPathWidth(float width)
        {
            // Removed minimum clamp to allow thinner paths
            pathWidth = Mathf.Max(0.001f, width); // Min 1mm
            
            // Re-render target path if width changes
            RenderTargetPathTube();
        }
    }

}
