using UnityEngine;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Individual balloon component for balloon pop exercise
    /// Handles collision detection and pop mechanics
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Balloon : MonoBehaviour
    {
        [Header("Balloon Settings")]
        [SerializeField] private float popRadius = 0.15f;
        [SerializeField] private int points = 10;
        [SerializeField] private float lifetime = 10f;

        private Collider balloonCollider;
        private bool isPopped;
        private float spawnTime;
        private bool hasReportedFailure;

        public bool IsPopped => isPopped;
        public int Points => points;
        public float Age => Time.time - spawnTime;
        public System.Action<Balloon> OnLifetimeExpired;

        private void Awake()
        {
            balloonCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            if (Age >= lifetime && !isPopped && !hasReportedFailure)
            {
                hasReportedFailure = true;
                OnLifetimeExpired?.Invoke(this);
                Pop();
            }
        }

        public bool CheckPop(Vector3 handPosition)
        {
            if (isPopped) return false;

            float distance = Vector3.Distance(transform.position, handPosition);
            if (distance <= popRadius)
            {
                Pop();
                return true;
            }

            return false;
        }

        public void Pop()
        {
            if (isPopped) return;

            isPopped = true;
            balloonCollider.enabled = false;
            gameObject.SetActive(false);
        }

        public void ResetBalloon()
        {
            isPopped = false;
            hasReportedFailure = false;
            balloonCollider.enabled = true;
            spawnTime = Time.time;  // NOW properly set when spawned from pool
            gameObject.SetActive(true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, popRadius);
        }
    }
}
