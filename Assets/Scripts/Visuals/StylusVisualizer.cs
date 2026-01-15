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

        // Fallback for tracking loss
        private Vector3 lastValidPosition;
        private Quaternion lastValidRotation;

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

            // FIX: Show if we are in Stylus mode OR Auto mode (searching for stylus)
            // Even if HasValidInput is false momentarily (tracking lost), we should keep showing it at last known pos
            // or maybe just default to showing it if we intend to use Stylus.
            bool intendedModeIsStylus = inputHandler.CurrentMode == InputMode.Stylus || 
                                      (inputHandler.CurrentMode == InputMode.Auto && !inputHandler.IsInputAvailable(InputMode.Hand)); 

            // If InputHandler is in "None" mode but Preferred is Auto/Stylus, it might be SEARCHING.
            // In Search mode, we should SHOW the stylus to indicate "Searching..." (maybe at head pos or last pos)
            
            bool shouldShow = false;
            
            if (inputHandler.HasValidInput)
            {
                 // precise check
                 shouldShow = (inputHandler.CurrentMode == InputMode.Stylus || inputHandler.CurrentMode == InputMode.Simulator);
            }
            else
            {
                 // Fallback: If we are "Auto" or "Stylus" preferred, show it?
                 // If CurrentMode is Hand, definitely HIDDEN.
                 if (inputHandler.CurrentMode == InputMode.Hand)
                 {
                     shouldShow = false;
                 }
                 else
                 {
                     // If CurrentMode is None (Searching) or Stylus (Lost Tracking), SHOW IT.
                     // But only if Preferred is Auto or Stylus
                     // actually, we can't easily check 'PreferredMode' from here (it's private in InputHandler... wait, no it's serialized but private field. Public accessor?)
                     // InputHandler doesn't expose PreferredMode publicly (only SetInputMode).
                     // But we can infer from CurrentMode == None or Stylus.
                     
                     // If we definitely don't have Hands, and we aren't in Simulator... well, let's just show it.
                     shouldShow = true;
                 }
            }
            
            if (stylusModel.activeSelf != shouldShow)
                stylusModel.SetActive(shouldShow);

            if (shouldShow)
            {
                // Calculate target position and rotation
                Vector3 targetPos = Vector3.zero;
                Quaternion targetRot = Quaternion.identity;

                // VISUALIZE RAW DATA DIRECTLY if possible
                if (inputHandler.ActiveInput is StylusInputManager stylusParams)
                {
                    targetPos = stylusParams.RawPosition;
                    targetRot = stylusParams.RawRotation;
                }
                // Fallback (e.g. Simulator or unexpected state)
                else
                {
                     targetPos = inputHandler.Position;
                     targetRot = inputHandler.Rotation;
                }
                
                // If we are searching (ActiveInput is null/None via InputHandler), try to find StylusManager manually?
                if (inputHandler.CurrentMode == InputMode.None || inputHandler.CurrentMode == InputMode.Auto)
                {
                    // Find StylusInputManager reference if we haven't
                     var stylusMan = FindFirstObjectByType<StylusInputManager>();
                     if (stylusMan != null)
                     {
                         targetPos = stylusMan.RawPosition;
                         targetRot = stylusMan.RawRotation;
                     }
                }

                // Handle Tracking Loss gracefully
                // If position crashes to zero (unlikely for a real held object unless untracked),
                // use last valid position to prevent visual glitching
                if (targetPos.sqrMagnitude > 0.001f)
                {
                    lastValidPosition = targetPos;
                    lastValidRotation = targetRot;
                }
                else if (lastValidPosition.sqrMagnitude > 0.001f)
                {
                     // Use fallback
                     targetPos = lastValidPosition;
                     targetRot = lastValidRotation;
                }
                
                Quaternion finalRotation = targetRot * Quaternion.Euler(rotationOffset);
                transform.rotation = finalRotation;
                
                // Apply position offset RELATIVE to the rotation (Local Space)
                transform.position = targetPos + (finalRotation * positionOffset);
            }

            // DEBUG LOGGING
            if (Time.frameCount % 120 == 0)
            {
                 Debug.Log($"[StylusVisualizer] Visible={shouldShow}, ModelActive={stylusModel.activeSelf}, HandlerMode={inputHandler.CurrentMode}, InputPos={inputHandler.Position}, VisualizerPos={transform.position}");
            }
        }
    }
}
