using UnityEngine;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Individual balloon component for balloon pop exercise
    /// Handles collision detection and pop mechanics with visual feedback
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Balloon : MonoBehaviour
    {
        [Header("Balloon Settings")]
        [SerializeField] private float popRadius = 0.5f;
        [SerializeField] private float lifetime = 10f;
        
        [Header("Visual Effects")]
        [SerializeField] private bool enableFloatingAnimation = true;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float floatAmplitude = 0.1f;
        [SerializeField] private bool enableColorVariation = true;
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool logDebugInfo = true;

        private Collider balloonCollider;
        private Renderer balloonRenderer;
        private MaterialPropertyBlock propertyBlock;
        private bool isPopped;
        private float spawnTime;
        private bool hasReportedFailure;
        private Vector3 originalPosition;
        
        // Vibrant balloon colors
        private static readonly Color[] balloonColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f),     // Bright Red
            new Color(0.2f, 0.9f, 0.2f),   // Bright Green
            new Color(0.2f, 0.4f, 1f),     // Bright Blue
            new Color(1f, 0.9f, 0.1f),     // Bright Yellow
            new Color(1f, 0.4f, 0.7f),     // Hot Pink
            new Color(0.7f, 0.2f, 1f),     // Purple
            new Color(1f, 0.5f, 0.1f),     // Orange
            new Color(0.1f, 0.9f, 0.9f),   // Cyan
        };

        public bool IsPopped => isPopped;
        public float Age => Time.time - spawnTime;
        public float Lifetime => lifetime;
        public System.Action<Balloon> OnLifetimeExpired;

        private void Awake()
        {
            balloonCollider = GetComponent<Collider>();
            balloonRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (isPopped) return;
            
            // Check lifetime expiration
            if (Age >= lifetime && !hasReportedFailure)
            {
                hasReportedFailure = true;
                OnLifetimeExpired?.Invoke(this);
                Pop();
                return;
            }
            
            // Floating animation
            if (enableFloatingAnimation)
            {
                float yOffset = Mathf.Sin(Time.time * floatSpeed + spawnTime) * floatAmplitude;
                transform.position = originalPosition + Vector3.up * yOffset;
            }
        }

        public bool CheckPop(Vector3 handPosition)
        {
            if (isPopped) return false;

            float distance = Vector3.Distance(transform.position, handPosition);
            
            if (logDebugInfo)
            {
                Debug.Log($"[Balloon] CheckPop - Hand: {handPosition}, Balloon: {transform.position}, Distance: {distance:F2}, PopRadius: {popRadius}");
            }
            
            if (distance <= popRadius)
            {
                if (logDebugInfo)
                    Debug.Log($"[Balloon] POPPED! Distance {distance:F2} <= PopRadius {popRadius}");
                Pop();
                return true;
            }

            return false;
        }

        public void Pop()
        {
            if (isPopped) return;

            Debug.Log($"[Balloon] Pop() called on {gameObject.name}");
            isPopped = true;
            
            if (balloonCollider != null)
                balloonCollider.enabled = false;
            gameObject.SetActive(false);
        }

        public void ResetBalloon()
        {
            isPopped = false;
            hasReportedFailure = false;
            spawnTime = Time.time;
            
            if (balloonCollider != null)
                balloonCollider.enabled = true;
            
            // Apply random color variation using MaterialPropertyBlock (works with URP)
            if (enableColorVariation && balloonRenderer != null)
            {
                Color randomColor = balloonColors[Random.Range(0, balloonColors.Length)];
                
                
                // Try multiple shader property names for compatibility
                balloonRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", randomColor);      // URP Lit
                propertyBlock.SetColor("_Color", randomColor);          // Standard
                propertyBlock.SetColor("_MainColor", randomColor);      // Custom shaders
                balloonRenderer.SetPropertyBlock(propertyBlock);
                
                // Also try direct material color as fallback
                if (balloonRenderer.material != null)
                {
                    balloonRenderer.material.SetColor("_BaseColor", randomColor);
                    balloonRenderer.material.SetColor("_Color", randomColor);
                    balloonRenderer.material.color = randomColor;
                }
                
                if (logDebugInfo)
                    Debug.Log($"[Balloon] Set color to {randomColor} on {gameObject.name}");
            }
            
            // Store position for floating animation
            originalPosition = transform.position;
            
            if (logDebugInfo)
                Debug.Log($"[Balloon] Reset at position {originalPosition}, popRadius: {popRadius}");
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Draw pop radius - always visible
            Gizmos.color = isPopped ? Color.gray : Color.green;
            Gizmos.DrawWireSphere(transform.position, popRadius);
            
            // Draw lifetime indicator (red when about to expire)
            if (!isPopped && Application.isPlaying)
            {
                float remainingLifeRatio = 1f - (Age / lifetime);
                Gizmos.color = Color.Lerp(Color.red, Color.green, remainingLifeRatio);
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * (0.5f * remainingLifeRatio));
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw larger pop radius when selected
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, popRadius);
        }
    }
}
