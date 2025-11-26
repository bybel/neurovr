using UnityEngine;
using NeuroReachVR.Tasks;

namespace NeuroReachVR.Core
{
    /// <summary>
    /// Improved rule-based adaptive difficulty controller
    /// NOW WITH: Hysteresis, time-based adjustments, patient-specific calibration
    /// </summary>
    public class AdaptiveDifficultyController : MonoBehaviour
    {
        [Header("Task References")]
        [SerializeField] private BalloonPopTask balloonTask;
        [SerializeField] private PathTracingTask pathTask;
        [SerializeField] private SpiralTracingTask spiralTask;

        [Header("Difficulty Profiles")]
        [SerializeField] private DifficultyProfile easyProfile;
        [SerializeField] private DifficultyProfile mediumProfile;
        [SerializeField] private DifficultyProfile hardProfile;

        [Header("Adaptation Rules")]
        [SerializeField] private float successRateThresholdEasy = 0.8f;
        [SerializeField] private float successRateThresholdMedium = 0.7f;
        [SerializeField] private float successRateThresholdHard = 0.5f;
        [SerializeField] private float accuracyThresholdMediumToHard = 0.75f;
        [SerializeField] private int minAttemptsBeforeAdjustment = 5;

        [Header("Hysteresis Settings")]
        [SerializeField] private int minAttemptsAtLevelBeforeChange = 10;
        [SerializeField] private float evaluationInterval = 30f; // seconds

        [Header("Time-Based Adjustments")]
        [SerializeField] private float targetCompletionTimeEasy = 5f;
        [SerializeField] private float targetCompletionTimeMedium = 3f;
        [SerializeField] private float targetCompletionTimeHard = 2f;
        [SerializeField] private bool useTimeInDecisions = true;

        private DifficultyLevel currentLevel = DifficultyLevel.Easy;
        private DifficultyProfile currentProfile;
        private PerformanceMetrics metrics;
        private PerformanceMetrics longTermMetrics; // Never cleared for progression tracking
        private BaseTask activeTask;
        private int attemptsAtCurrentLevel;
        private float lastEvaluationTime;

        public DifficultyLevel CurrentLevel => currentLevel;
        public PerformanceMetrics Metrics => metrics;
        public PerformanceMetrics LongTermMetrics => longTermMetrics;

        private void Start()
        {
            ValidateProfiles();

            metrics = new PerformanceMetrics(minAttemptsBeforeAdjustment);
            longTermMetrics = new PerformanceMetrics(50); // Track last 50 attempts
            currentProfile = easyProfile.Clone();
            ApplyProfile(currentProfile);
            lastEvaluationTime = Time.time;
        }

        private void Update()
        {
            // Periodic evaluation instead of every attempt
            if (Time.time - lastEvaluationTime >= evaluationInterval &&
                metrics.AttemptCount >= minAttemptsBeforeAdjustment)
            {
                EvaluateAndAdjust();
                lastEvaluationTime = Time.time;
            }
        }

        private void ValidateProfiles()
        {
            if (easyProfile == null || mediumProfile == null || hardProfile == null)
            {
                Debug.LogError("[AdaptiveDifficulty] Difficulty profiles not assigned!");
                return;
            }
        }

        public void SetActiveTask(BaseTask task)
        {
            activeTask = task;
            metrics.Clear();
            attemptsAtCurrentLevel = 0;
            // Don't clear longTermMetrics - keep for progression tracking
        }

        public void RecordAttempt(float completionTime, bool success, float accuracy)
        {
            metrics.RecordAttempt(completionTime, success, accuracy);
            longTermMetrics.RecordAttempt(completionTime, success, accuracy);
            attemptsAtCurrentLevel++;

            // Immediate evaluation on significant events (can override interval)
            if (metrics.AttemptCount >= minAttemptsBeforeAdjustment && attemptsAtCurrentLevel % 5 == 0)
            {
                EvaluateAndAdjust();
            }
        }

        private void EvaluateAndAdjust()
        {
            float successRate = metrics.SuccessRate;
            float avgTime = metrics.AverageCompletionTime;
            float avgAccuracy = metrics.AverageAccuracy;

            DifficultyLevel newLevel = DetermineDifficulty(successRate, avgTime, avgAccuracy);

            // Apply hysteresis - only change if enough attempts at current level
            if (newLevel != currentLevel && attemptsAtCurrentLevel >= minAttemptsAtLevelBeforeChange)
            {
                ChangeDifficulty(newLevel);
            }
        }

        private DifficultyLevel DetermineDifficulty(float successRate, float avgTime, float avgAccuracy)
        {
            float targetTime = GetTargetTimeForLevel(currentLevel);
            bool isTooFast = useTimeInDecisions && avgTime < targetTime * 0.7f;
            bool isTooSlow = useTimeInDecisions && avgTime > targetTime * 1.5f;

            return currentLevel switch
            {
                DifficultyLevel.Easy => DetermineFromEasy(successRate, avgAccuracy, isTooFast),
                DifficultyLevel.Medium => DetermineFromMedium(successRate, avgAccuracy, isTooFast, isTooSlow),
                DifficultyLevel.Hard => DetermineFromHard(successRate, isTooSlow),
                _ => DifficultyLevel.Easy
            };
        }

        private DifficultyLevel DetermineFromEasy(float successRate, float avgAccuracy, bool isTooFast)
        {
            // Increase to Medium if: high success rate AND (high accuracy OR too fast)
            if (successRate >= successRateThresholdEasy &&
                (avgAccuracy > 0.8f || isTooFast))
            {
                return DifficultyLevel.Medium;
            }

            return DifficultyLevel.Easy;
        }

        private DifficultyLevel DetermineFromMedium(float successRate, float avgAccuracy, bool isTooFast, bool isTooSlow)
        {
            // Increase to Hard if: high success AND high accuracy AND (optionally fast)
            if (successRate >= successRateThresholdMedium &&
                avgAccuracy > accuracyThresholdMediumToHard &&
                (!useTimeInDecisions || isTooFast))
            {
                return DifficultyLevel.Hard;
            }

            // Decrease to Easy if: low success rate OR too slow
            if (successRate < successRateThresholdHard || isTooSlow)
            {
                return DifficultyLevel.Easy;
            }

            return DifficultyLevel.Medium;
        }

        private DifficultyLevel DetermineFromHard(float successRate, bool isTooSlow)
        {
            // Decrease to Medium if: low success rate OR too slow
            if (successRate < successRateThresholdHard || isTooSlow)
            {
                return DifficultyLevel.Medium;
            }

            return DifficultyLevel.Hard;
        }

        private float GetTargetTimeForLevel(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => targetCompletionTimeEasy,
                DifficultyLevel.Medium => targetCompletionTimeMedium,
                DifficultyLevel.Hard => targetCompletionTimeHard,
                _ => targetCompletionTimeMedium
            };
        }

        private void ChangeDifficulty(DifficultyLevel newLevel)
        {
            // Capture metrics before clearing for logging
            float previousSuccessRate = metrics.SuccessRate;
            float previousAccuracy = metrics.AverageAccuracy;
            float previousAvgTime = metrics.AverageCompletionTime;

            currentLevel = newLevel;
            currentProfile = GetProfileForLevel(newLevel).Clone();
            ApplyProfile(currentProfile);

            // Reset short-term metrics but keep attempts at level counter
            metrics.Clear();
            attemptsAtCurrentLevel = 0;

            Debug.Log($"[AdaptiveDifficulty] Changed to {newLevel} " +
                     $"(Success: {previousSuccessRate:P0}, Accuracy: {previousAccuracy:P0}, " +
                     $"AvgTime: {previousAvgTime:F2}s)");
        }

        private DifficultyProfile GetProfileForLevel(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => easyProfile,
                DifficultyLevel.Medium => mediumProfile,
                DifficultyLevel.Hard => hardProfile,
                _ => easyProfile
            };
        }

        private void ApplyProfile(DifficultyProfile profile)
        {
            if (balloonTask != null)
            {
                balloonTask.SetDifficulty(profile.heightRange, profile.spawnRate);
            }

            if (pathTask != null)
            {
                pathTask.SetDifficulty(profile.pathWidth, profile.requiredAccuracy);
                pathTask.SetPathType(profile.pathType);
            }

            if (spiralTask != null)
            {
                spiralTask.SetSpiralDifficulty(profile.spiralTightness,
                    profile.minAngularVelocity, profile.maxAngularVelocity);
            }
        }

        public void SetDifficultyLevel(DifficultyLevel level)
        {
            ChangeDifficulty(level);
        }

        public void ResetMetrics()
        {
            metrics.Clear();
            attemptsAtCurrentLevel = 0;
        }

        public void ResetAllMetrics()
        {
            metrics.Clear();
            longTermMetrics.Clear();
            attemptsAtCurrentLevel = 0;
        }

        public void SetTargetTimes(float easy, float medium, float hard)
        {
            targetCompletionTimeEasy = easy;
            targetCompletionTimeMedium = medium;
            targetCompletionTimeHard = hard;
        }
    }
}
