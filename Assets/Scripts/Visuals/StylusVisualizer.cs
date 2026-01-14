using UnityEngine;
using NeuroReachVR.Input;

namespace NeuroReachVR.Visuals
{
    /// <summary>
    /// Renders a virtual stylus model on top of everything to prevent occlusion by virtual objects.
    /// Useful for Mixed Reality where virtual objects (like balloons) shouldn't hide the user's tool.
    /// </summary>
    public class StylusVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputHandler inputHandler;
        
        [Header("Visual Settings")]
        [SerializeField] private float length = 0.15f;
        [SerializeField] private float diameter = 0.01f;
        [SerializeField] private Color stylusColor = new Color(0.9f, 0.9f, 0.9f); // Off-white
        [SerializeField] private Vector3 rotationOffset = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 positionOffset = new Vector3(0, 0, 0);

        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffset => rotationOffset;

        private GameObject stylusModel;
        private Renderer stylusRenderer;

        private void Start()
        {
            if (inputHandler == null)
            {
                inputHandler = FindFirstObjectByType<InputHandler>();
            }

            CreateStylusModel();
        }

        private void CreateStylusModel()
        {
            // Create a sleek cylinder representing the stylus
            stylusModel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stylusModel.name = "StylusOverlayModel";
            stylusModel.transform.SetParent(transform);
            
            // Remove collider to prevent physics interference
            Destroy(stylusModel.GetComponent<Collider>());

            // Scale it (Cylinder default height is 2, diameter 1)
            // We want the pivot to be at the tip or center? 
            // InputHandler usually gives tip position.
            // If tip, we need to offset the cylinder so the tip is at 0,0,0
            
            // Let's assume InputHandler gives TIP position.
            // Cylinder center is at (0,0,0). We need to move it back by half length.
            // But first, let's just make it a child of a pivot object
            GameObject modelPivot = new GameObject("ModelPivot");
            modelPivot.transform.SetParent(stylusModel.transform);
            
            // Actually, easier to just offset the mesh transform relative to this container
            // Let's make 'stylusModel' the container
            Destroy(stylusModel); 
            stylusModel = new GameObject("StylusVisual");
            stylusModel.transform.SetParent(transform);

            GameObject meshObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            meshObj.transform.SetParent(stylusModel.transform);
            Destroy(meshObj.GetComponent<Collider>());
            
            // Align cylinder: Default cylinder is along Y axis.
            // Stylus usually points forward (Z) or has a specific orientation.
            // Let's assume Z-forward is the pointing direction.
            // Rotate cylinder to lie along Z
            meshObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            
            // Offset so tip is at origin (0,0,0) and body extends backwards (-Z)
            meshObj.transform.localPosition = new Vector3(0, 0, -length / 2);
            meshObj.transform.localScale = new Vector3(diameter, length / 2, diameter); // Height is 2 * scale.y

            // Setup Material for Overlay
            stylusRenderer = meshObj.GetComponent<Renderer>();
            SetupOverlayMaterial();
        }

        private void SetupOverlayMaterial()
        {
            // Use a shader that renders on top
            Shader shader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(shader);
            mat.color = stylusColor;
            
            // CRITICAL: Render on top of everything
            mat.renderQueue = 4000; // Overlay
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            
            stylusRenderer.material = mat;
        }

        public void SetConfiguration(Vector3 posOffset, Vector3 rotOffset, Color color)
        {
            positionOffset = posOffset;
            rotationOffset = rotOffset;
            stylusColor = color;
            
            if (stylusRenderer != null && stylusRenderer.material != null)
            {
                stylusRenderer.material.color = stylusColor;
            }
        }
        
        private void Update()
        {
            if (inputHandler == null) return;

            // Only show if we have valid input (Stylus or Hand)
            // And specifically if we are in Stylus mode (or Simulator mimicking stylus)
            bool shouldShow = inputHandler.HasValidInput && 
                             (inputHandler.CurrentMode == InputMode.Stylus || inputHandler.CurrentMode == InputMode.Simulator);
            
            if (stylusModel.activeSelf != shouldShow)
                stylusModel.SetActive(shouldShow);

            if (shouldShow)
            {
                // Calculate rotation first
                Quaternion finalRotation = inputHandler.Rotation * Quaternion.Euler(rotationOffset);
                transform.rotation = finalRotation;
                
                // Apply position offset RELATIVE to the rotation (Local Space)
                // This matches how VRUIInputManager calculates the ray origin
                transform.position = inputHandler.Position + (finalRotation * positionOffset);
            }
        }
    }
}
