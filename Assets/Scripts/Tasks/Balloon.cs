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
        [SerializeField] private Color startColor = Color.blue;
        [SerializeField] private Color endColor = Color.red;
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

            UpdateColorOverLifetime();
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
            
            UpdateColorOverLifetime(0f);
            
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

        private void UpdateColorOverLifetime(float? overrideT = null)
        {
            if (!enableColorVariation || balloonRenderer == null)
                return;

            float t = overrideT.HasValue ? Mathf.Clamp01(overrideT.Value) : Mathf.Clamp01(Age / lifetime);
            Color currentColor = Color.Lerp(startColor, endColor, t);

            balloonRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", currentColor);      // URP Lit
            propertyBlock.SetColor("_Color", currentColor);          // Standard
            propertyBlock.SetColor("_MainColor", currentColor);      // Custom shaders
            balloonRenderer.SetPropertyBlock(propertyBlock);

            if (balloonRenderer.material != null)
            {
                balloonRenderer.material.SetColor("_BaseColor", currentColor);
                balloonRenderer.material.SetColor("_Color", currentColor);
                balloonRenderer.material.color = currentColor;
            }
        }
    }
}
