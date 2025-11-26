using UnityEngine;
using NeuroReachVR.Input;
using NeuroReachVR.Core;
using NeuroReachVR.Data;

namespace NeuroReachVR.Tasks
{
    /// <summary>
    /// Base class for all rehabilitation tasks
    /// Provides common functionality: timing, scoring, input handling
    /// </summary>
    public abstract class BaseTask : MonoBehaviour, ITaskPerformanceReporter
    {
        [Header("Input")]
        [SerializeField] protected InputHandler inputHandler;
        
        [Header("Task Settings")]
        [SerializeField] protected float sessionDuration = 60f;
        [SerializeField] protected bool autoStart = false;
        
        [Header("Adaptive Difficulty")]
        [SerializeField] protected AdaptiveDifficultyController adaptiveController;
        
        [Header("Data Logging")]
        [SerializeField] protected DataLogger dataLogger;
        [SerializeField] protected KinematicDataCollector kinematicCollector;
        
        protected float elapsedTime;
        protected bool isActive;
        protected int score;
        protected int errorCount;
        
        public bool IsActive => isActive;
        public float ElapsedTime => elapsedTime;
        public float RemainingTime => Mathf.Max(0f, sessionDuration - elapsedTime);
        public int Score => score;
        public float Progress => sessionDuration > 0 ? elapsedTime / sessionDuration : 0f;
        
        protected virtual void Start()
        {
            // Auto-find InputHandler if not assigned
            if (inputHandler == null)
            {
                inputHandler = FindFirstObjectByType<InputHandler>();
                if (inputHandler == null)
                    Debug.LogError($"[{GetType().Name}] InputHandler not found in scene!");
                else
                    Debug.Log($"[{GetType().Name}] Auto-found InputHandler");
            }
            
            // Auto-find AdaptiveDifficultyController if not assigned
            if (adaptiveController == null)
            {
                adaptiveController = FindFirstObjectByType<AdaptiveDifficultyController>();
            }
            
            // Auto-find DataLogger if not assigned
            if (dataLogger == null)
            {
                dataLogger = FindFirstObjectByType<DataLogger>();
            }
            
            // Auto-find KinematicDataCollector if not assigned
            if (kinematicCollector == null)
            {
                kinematicCollector = FindFirstObjectByType<KinematicDataCollector>();
            }
            
            if (autoStart)
                StartTask();
        }
        
        protected virtual void Update()
        {
            if (!isActive) return;
            
            elapsedTime += Time.deltaTime;
            
            if (elapsedTime >= sessionDuration)
            {
                EndTask();
                return;
            }
            
            UpdateTask();
        }
        
        public virtual void StartTask()
        {
            isActive = true;
            elapsedTime = 0f;
            score = 0;
            errorCount = 0;
            
            if (dataLogger != null)
            {
                string taskType = GetType().Name;
                DifficultyLevel difficulty = adaptiveController != null 
                    ? adaptiveController.CurrentLevel 
                    : DifficultyLevel.Easy;
                dataLogger.LogSessionStart(taskType, difficulty);
            }
            
            if (kinematicCollector != null)
                kinematicCollector.Clear();
            
            OnTaskStarted();
        }
        
        public virtual void EndTask()
        {
            isActive = false;
            OnTaskEnded();
        }
        
        public virtual void PauseTask()
        {
            isActive = false;
        }
        
        public virtual void ResumeTask()
        {
            isActive = true;
        }
        
        protected virtual void AddScore(int points)
        {
            score += points;
        }
        
        public virtual void ReportAttempt(float completionTime, bool success, float accuracy)
        {
            adaptiveController?.RecordAttempt(completionTime, success, accuracy);
            
            // Log data
            if (dataLogger != null && kinematicCollector != null)
            {
                string adjustment = adaptiveController != null 
                    ? adaptiveController.CurrentLevel.ToString() 
                    : "";
                dataLogger.LogTaskAttempt(this, kinematicCollector, success, accuracy, errorCount, adjustment);
            }
        }
        
        protected virtual void IncrementError()
        {
            errorCount++;
        }
        
        protected virtual void ResetErrorCount()
        {
            errorCount = 0;
        }
        
        protected abstract void UpdateTask();
        protected abstract void OnTaskStarted();
        protected abstract void OnTaskEnded();
    }
}

