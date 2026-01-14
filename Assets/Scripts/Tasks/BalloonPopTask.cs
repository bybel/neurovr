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
        [SerializeField] private float taskDuration = 60f; // Default duration
        
        [Header("Difficulty")]
        [SerializeField] private float minSpawnHeight = 0.5f;
        [SerializeField] private float maxSpawnHeight = 2f;
        
        [Header("Feedback")]
        [SerializeField] private TaskFeedback feedback;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private float explosionLifetime = 2f;
        [SerializeField] private Vector3 explosionOffset = Vector3.zero;
        
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
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;
        [SerializeField] private bool showOnScreenDebug = true;
        private float lastDebugLogTime;
        private string debugText = "";
        
        protected override void UpdateTask()
        {
            if (inputHandler == null)
            {
                debugText = "ERROR: InputHandler is NULL!";
                Debug.LogWarning("[BalloonPopTask] InputHandler not assigned!");
                return;
            }
            
            // VERBOSE DEBUG: Log input state every second
            if (showDebugLogs && Time.time - lastDebugLogTime > 1f)
            {
                Debug.Log($"[BalloonPopTask] INPUT STATE - Mode: {inputHandler.CurrentMode}, HasValidInput: {inputHandler.HasValidInput}, IsPinching: {inputHandler.IsPinching}, Position: {inputHandler.Position}, ActiveBalloons: {activeBalloons?.Count ?? 0}");
                lastDebugLogTime = Time.time;
            }
            
            // Always update debug text
            debugText = $"Input Mode: {inputHandler.CurrentMode}\n" +
                       $"Has Valid Input: {inputHandler.HasValidInput}\n" +
                       $"Is Pinching: {inputHandler.IsPinching}\n" +
                       $"Position: {inputHandler.Position}\n" +
                       $"Active Balloons: {activeBalloons?.Count ?? 0}";
            
            // 1. Spawn Balloons (Visuals first!)
            SpawnBalloons();
            CleanupPoppedBalloons();

            // 2. Check Input
            if (!inputHandler.HasValidInput)
            {
                if (showDebugLogs && Time.time - lastDebugLogTime > 2f)
                {
                    Debug.LogWarning($"[BalloonPopTask] No valid input! Mode: {inputHandler.CurrentMode}, HasInput: {inputHandler.HasValidInput}");
                    lastDebugLogTime = Time.time;
                }
                return;
            }
            
            CheckBalloonPops();
            
            // Check for Timeout
            if (ElapsedTime >= taskDuration)
            {
                EndTask();
            }
        }
        
        public void SetDuration(float duration)
        {
            taskDuration = duration;
            Debug.Log($"[BalloonPopTask] Duration set to {taskDuration}s");
        }
        
        // On-screen debug display
        private void OnGUI()
        {
            if (!showOnScreenDebug || !isActive) return;
            
            // Draw debug info in top-right corner
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 14;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;
            
            GUI.Box(new Rect(Screen.width - 320, 10, 310, 150), "");
            GUI.Label(new Rect(Screen.width - 315, 15, 300, 140), 
                $"=== Balloon Task Debug ===\n{debugText}", style);
        }
        
        private void CheckBalloonPops()
        {
            Vector3 handPos = inputHandler.Position;
            // Determine interaction state
            // Hands: Require Pinch
            // Stylus: Just proximity (Touch) is enough, or require press if desired
            // Simulator: Require Click (Pinch)
            bool isInteracting = inputHandler.IsPinching;
            
            if (inputHandler.CurrentMode == InputMode.Stylus)
            {
                // For Stylus, we treat "being close enough" as the interaction trigger (Touch)
                // The actual distance check happens in balloon.CheckPop()
                isInteracting = true; 
            }
            
            if (!isInteracting) return;
            
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
            int pointsEarned = CalculateBalloonScore(balloon);
            AddScore(pointsEarned);
            feedback?.PlaySuccess(balloon.transform.position);
            PlayExplosionEffect(balloon.transform.position);

            // Report successful attempt (reaction time = balloon age)
            ReportAttempt(balloon.Age, true, 1f);

            ReturnToPool(balloon);
        }

        private int CalculateBalloonScore(Balloon balloon)
        {
            if (balloon == null || balloon.Lifetime <= 0f)
                return 0;

            float lifetimeFraction = Mathf.Clamp01(balloon.Age / balloon.Lifetime);
            int penaltySteps = Mathf.FloorToInt(lifetimeFraction * 10f);
            int score = 10 - penaltySteps;
            return Mathf.Max(0, score);
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
            balloon.gameObject.SetActive(true);
            activeBalloons.Add(balloon);
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            // Get camera position to spawn balloons in front of it
            Camera cam = Camera.main;
            Vector3 basePosition = spawnCenter;
            
            if (cam != null)
            {
                // Spawn in front of camera at REACHING distance (0.5m - 0.7m)
                // Was 2.0f which is too far for direct interaction
                basePosition = cam.transform.position + cam.transform.forward * 0.6f;
            }
            
            // Add random offset (reduced radius for reaching)
            float xOffset = Random.Range(-0.3f, 0.3f);
            
            // Fix Height: Spawn relative to eye level (Camera Y)
            // Range: -0.3m (below eye) to +0.1m (slightly above)
            float yOffset = Random.Range(-0.3f, 0.1f);
            
            float zOffset = Random.Range(-0.1f, 0.1f);
            
            Vector3 pos = basePosition + new Vector3(xOffset, yOffset, zOffset);
            
            if (showDebugLogs)
            {
                Debug.Log($"[BalloonPopTask] Spawning balloon at: {pos}");
            }
            
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
            if (balloon.gameObject.activeSelf)
                balloon.gameObject.SetActive(false);
            return balloon;
        }
        
        private void ReturnToPool(Balloon balloon)
        {
            if (balloon == null) return;

            activeBalloons.Remove(balloon);
            balloon.gameObject.SetActive(false);
            balloonPool.Enqueue(balloon);
        }

        private void PlayExplosionEffect(Vector3 position)
        {
            if (explosionPrefab == null) return;

            var fxInstance = Instantiate(explosionPrefab, position + explosionOffset, Quaternion.identity);

            var particleSystem = fxInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Clear(true);
                particleSystem.Play(true);
            }

            if (explosionLifetime > 0f)
                Destroy(fxInstance, explosionLifetime);
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
            
            // Disable Ray
            var uiManager = FindFirstObjectByType<NeuroReachVR.UI.VRUIInputManager>();
            if (uiManager != null)
            {
                uiManager.SetPointerActive(false);
            }
        }
        
        protected override void OnTaskEnded()
        {
            // Create a copy of the list to avoid "Collection was modified" exception
            var balloonsToReturn = new List<Balloon>(activeBalloons);
            activeBalloons.Clear(); // Clear first to prevent re-entry issues
            
            foreach (var balloon in balloonsToReturn)
            {
                if (balloon != null)
                    ReturnToPool(balloon);
            }
            
            Debug.Log($"[BalloonPopTask] Task ended. Final score: {balloonsPopped} balloons popped.");
            
            // Re-enable Ray
            var uiManager = FindFirstObjectByType<NeuroReachVR.UI.VRUIInputManager>();
            if (uiManager != null)
            {
                uiManager.SetPointerActive(true);
            }
        }
        
        public void SetDifficulty(float heightRange, float spawnRate)
        {
            maxSpawnHeight = minSpawnHeight + heightRange;
            spawnInterval = Mathf.Max(0.5f, 2f / spawnRate);
        }
    }
}
