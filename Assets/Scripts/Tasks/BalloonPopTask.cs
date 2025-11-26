using UnityEngine;
using System.Collections.Generic;
using NeuroReachVR.Input;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Balloon popping exercise for reach-and-grasp training
    /// Uses hand tracking with pinch detection for popping balloons
    /// </summary>
    public class BalloonPopTask : BaseTask
    {
        [Header("Balloon Spawning")]
        [SerializeField] private GameObject balloonPrefab;
        [SerializeField] private int maxBalloons = 5;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private Vector3 spawnCenter = Vector3.zero;
        
        [Header("Difficulty")]
        [SerializeField] private float minSpawnHeight = 0.5f;
        [SerializeField] private float maxSpawnHeight = 2f;
        [SerializeField] private float spawnRadius = 1.5f;
        
        [Header("Feedback")]
        [SerializeField] private TaskFeedback feedback;
        
        private const int POOL_MULTIPLIER = 2; // Pool size = maxBalloons * multiplier
        
        private Queue<Balloon> balloonPool;
        private List<Balloon> activeBalloons;
        private float lastSpawnTime;
        private int balloonsPopped;
        
        protected override void Start()
        {
            base.Start();
            InitializePool();
            activeBalloons = new List<Balloon>();
        }
        
        private void InitializePool()
        {
            if (balloonPrefab == null)
            {
                Debug.LogError("[BalloonPopTask] Balloon prefab not assigned!");
                return;
            }

            balloonPool = new Queue<Balloon>();
            for (int i = 0; i < maxBalloons * POOL_MULTIPLIER; i++)
            {
                GameObject obj = Instantiate(balloonPrefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform); // Organize in hierarchy

                Balloon balloon = obj.GetComponent<Balloon>();
                if (balloon != null)
                {
                    balloon.OnLifetimeExpired += OnBalloonLifetimeExpired;
                    balloonPool.Enqueue(balloon);
                }
                else
                {
                    Debug.LogError("[BalloonPopTask] Balloon prefab missing Balloon component!");
                }
            }
        }
        
        protected override void UpdateTask()
        {
            if (inputHandler == null)
            {
                Debug.LogWarning("[BalloonPopTask] InputHandler not assigned!");
                return;
            }
            
            if (!inputHandler.HasValidInput) return;
            
            CheckBalloonPops();
            SpawnBalloons();
            CleanupPoppedBalloons();
        }
        
        private void CheckBalloonPops()
        {
            if (!inputHandler.IsPinching) return;
            
            Vector3 handPos = inputHandler.Position;
            
            foreach (var balloon in activeBalloons)
            {
                if (balloon != null && !balloon.IsPopped && balloon.CheckPop(handPos))
                {
                    OnBalloonPopped(balloon);
                    break; // One pop per pinch
                }
            }
        }
        
        private void OnBalloonPopped(Balloon balloon)
        {
            balloonsPopped++;
            AddScore(balloon.Points);
            feedback?.PlaySuccess(balloon.transform.position);

            // Report successful attempt (reaction time = balloon age)
            ReportAttempt(balloon.Age, true, 1f);

            ReturnToPool(balloon);
        }

        private void OnBalloonLifetimeExpired(Balloon balloon)
        {
            // Report failed attempt (timeout)
            feedback?.PlayError(balloon.transform.position);
            ReportAttempt(balloon.Age, false, 0f);
            
            // Return balloon to pool immediately to prevent pool exhaustion
            // Note: Balloon.Update() will call Pop() after this, but that's harmless
            // since ReturnToPool already deactivates the balloon
            ReturnToPool(balloon);
        }
        
        private void SpawnBalloons()
        {
            if (Time.time - lastSpawnTime < spawnInterval) return;
            if (activeBalloons.Count >= maxBalloons) return;
            
            SpawnBalloon();
            lastSpawnTime = Time.time;
        }
        
        private void SpawnBalloon()
        {
            Balloon balloon = GetFromPool();
            if (balloon == null) return;
            
            Vector3 spawnPos = GetRandomSpawnPosition();
            balloon.transform.position = spawnPos;
            balloon.ResetBalloon();
            activeBalloons.Add(balloon);
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(0.5f, spawnRadius);
            float height = Random.Range(minSpawnHeight, maxSpawnHeight);
            
            Vector3 pos = spawnCenter;
            pos.x += Mathf.Cos(angle) * radius;
            pos.z += Mathf.Sin(angle) * radius;
            pos.y = height;
            
            return pos;
        }
        
        private void CleanupPoppedBalloons()
        {
            for (int i = activeBalloons.Count - 1; i >= 0; i--)
            {
                if (activeBalloons[i].IsPopped)
                    ReturnToPool(activeBalloons[i]);
            }
        }
        
        private Balloon GetFromPool()
        {
            if (balloonPool.Count == 0)
            {
                Debug.LogWarning("[BalloonPopTask] Pool exhausted! Consider increasing pool size.");
                return null;
            }

            Balloon balloon = balloonPool.Dequeue();
            balloon.gameObject.SetActive(true);
            return balloon;
        }
        
        private void ReturnToPool(Balloon balloon)
        {
            if (balloon == null) return;

            activeBalloons.Remove(balloon);
            balloon.gameObject.SetActive(false);
            balloonPool.Enqueue(balloon);
        }

        private void OnDestroy()
        {
            // Cleanup event listeners
            if (balloonPool != null)
            {
                foreach (var balloon in balloonPool)
                {
                    if (balloon != null)
                        balloon.OnLifetimeExpired -= OnBalloonLifetimeExpired;
                }
            }

            if (activeBalloons != null)
            {
                foreach (var balloon in activeBalloons)
                {
                    if (balloon != null)
                        balloon.OnLifetimeExpired -= OnBalloonLifetimeExpired;
                }
            }
        }
        
        protected override void OnTaskStarted()
        {
            lastSpawnTime = Time.time;
            balloonsPopped = 0;
        }
        
        protected override void OnTaskEnded()
        {
            foreach (var balloon in activeBalloons)
                ReturnToPool(balloon);
            activeBalloons.Clear();
        }
        
        public void SetDifficulty(float heightRange, float spawnRate)
        {
            maxSpawnHeight = minSpawnHeight + heightRange;
            spawnInterval = Mathf.Max(0.5f, 2f / spawnRate);
        }
    }
}

