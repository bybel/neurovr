using UnityEngine;
using NeuroReachVR.Input;
using NeuroReachVR.Tasks;
using NeuroReachVR.Core;
using NeuroReachVR.Data;
using NeuroReachVR.Feedback;
using NeuroReachVR.UI;

namespace NeuroReachVR.Core
{
    /// <summary>
    /// Main game manager that orchestrates all systems
    /// Manages task flow, system integration, game state
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private AdaptiveDifficultyController adaptiveController;
        [SerializeField] private DataLogger dataLogger;
        [SerializeField] private KinematicDataCollector kinematicCollector;
        [SerializeField] private MultimodalFeedback feedback;
        [SerializeField] private AccessibleUI accessibleUI;
        
        [Header("Task References")]
        [SerializeField] private BalloonPopTask balloonTask;
        [SerializeField] private PathTracingTask pathTask;
        [SerializeField] private SpiralTracingTask spiralTask;
        
        [Header("UI")]
        [SerializeField] private HUDManager hudManager;
        
        [Header("Game Settings")]
        [SerializeField] private bool autoInitialize = true;
        
        private const string PATIENT_ID_PLAYERPREF_KEY = "CurrentPatientID";
        
        private BaseTask currentTask;
        private GameState currentState;
        private PatientDataManager patientDataManager;
        
        /// <summary>
        /// Event fired when the game state changes (e.g., paused, resumed)
        /// </summary>
        public event System.Action<GameState> OnGameStateChanged;
        
        public GameState CurrentState => currentState;
        public BaseTask CurrentTask => currentTask;
        
        private void Awake()
        {
            // Register with ServiceLocator so HUDManager can find it
            ServiceLocator.Register(this);

#if UNITY_EDITOR
            if (inputHandler != null)
            {
                var simulator = FindFirstObjectByType<SimulatorInput>();
                if (simulator == null)
                {
                    GameObject simObj = new GameObject("SimulatorInput");
                    simulator = simObj.AddComponent<SimulatorInput>();
                }

                // Assign the simulator to the input handler
                var so = new UnityEditor.SerializedObject(inputHandler);
                var sp = so.FindProperty("simulatorInput");
                sp.objectReferenceValue = simulator;
                so.ApplyModifiedProperties();

                inputHandler.SetInputMode(InputMode.Simulator);
            }
#endif
            
            patientDataManager = FindFirstObjectByType<PatientDataManager>();
            if (patientDataManager == null)
                patientDataManager = gameObject.AddComponent<PatientDataManager>();
        }
        
        private void Start()
        {
            if (autoInitialize)
                Initialize();
        }
        
        public void Initialize()
        {
            currentState = GameState.MainMenu;
            ValidateSystems();
            ConnectSystems();
        }
        
        private void ValidateSystems()
        {
            if (inputHandler == null)
                Debug.LogWarning("[GameManager] InputHandler not assigned");
            
            if (adaptiveController == null)
                Debug.LogWarning("[GameManager] AdaptiveDifficultyController not assigned");
            
            if (dataLogger == null)
                Debug.LogWarning("[GameManager] DataLogger not assigned");
            
            if (kinematicCollector == null)
                Debug.LogWarning("[GameManager] KinematicDataCollector not assigned");
        }
        
        private void ConnectSystems()
        {
            // Connect adaptive controller to tasks
            if (adaptiveController != null)
            {
                if (balloonTask != null)
                    adaptiveController.SetActiveTask(balloonTask);
            }
            
            // Connect data logger to patient manager
            if (dataLogger != null && patientDataManager != null)
            {
                string patientID = patientDataManager.GetCurrentPatientID();
                if (!string.IsNullOrEmpty(patientID))
                    dataLogger.SetPatientID(patientID);
            }
        }
        
        public void StartTask(TaskType taskType)
        {
            BaseTask task = GetTask(taskType);
            if (task == null)
            {
                Debug.LogError($"[GameManager] Task {taskType} not found");
                return;
            }
            
            currentTask = task;
            currentState = GameState.TaskActive;
            
            // Setup task systems
            if (adaptiveController != null)
                adaptiveController.SetActiveTask(task);
            
            if (dataLogger != null && kinematicCollector != null)
            {
                string taskTypeName = task.GetType().Name;
                DifficultyLevel difficulty = adaptiveController != null 
                    ? adaptiveController.CurrentLevel 
                    : DifficultyLevel.Easy;
                dataLogger.LogSessionStart(taskTypeName, difficulty);
            }
            
            task.StartTask();
            OnGameStateChanged?.Invoke(currentState);
        }
        
        public void EndCurrentTask()
        {
            if (currentTask != null)
            {
                // Capture task data BEFORE ending (task will be null after EndTask)
                string taskType = currentTask.GetType().Name;
                int finalScore = currentTask.Score;
                float finalDuration = currentTask.ElapsedTime;
                
                // Record session completion
                if (patientDataManager != null)
                {
                    patientDataManager.RecordSessionCompletion(
                        patientDataManager.GetCurrentPatientID(),
                        taskType,
                        finalDuration,
                        finalScore
                    );
                }
                
                currentTask.EndTask();
                currentTask = null;
                
                // Notify HUDManager immediately with captured task data
                if (hudManager != null)
                {
                    hudManager.OnTaskCompleted(taskType, finalScore, finalDuration);
                }
                else
                {
                    Debug.LogWarning("[GameManager] HUDManager not assigned - cannot show task completion UI");
                }
            }
            
            currentState = GameState.TaskComplete;
            OnGameStateChanged?.Invoke(currentState);
        }
        
        public void PauseCurrentTask()
        {
            if (currentTask != null)
            {
                currentTask.PauseTask();
                currentState = GameState.Paused;
                OnGameStateChanged?.Invoke(currentState);
                Debug.Log("[GameManager] Task paused");
            }
        }
        
        public void ResumeCurrentTask()
        {
            if (currentTask != null)
            {
                currentTask.ResumeTask();
                currentState = GameState.TaskActive;
                OnGameStateChanged?.Invoke(currentState);
                Debug.Log("[GameManager] Task resumed");
            }
        }
        
        public void SetPatientID(string patientID)
        {
            if (patientDataManager != null)
            {
                patientDataManager.SetCurrentPatient(patientID);
                
                if (dataLogger != null)
                    dataLogger.SetPatientID(patientID);
            }
        }
        
        /// <summary>
        /// Check if a valid patient is currently logged in
        /// </summary>
        /// <returns>True if a non-empty patient ID is set</returns>
        public bool IsPatientLoggedIn()
        {
            if (patientDataManager == null)
                return false;
            
            string patientID = patientDataManager.GetCurrentPatientID();
            return !string.IsNullOrEmpty(patientID);
        }
        
        /// <summary>
        /// Get the currently logged in patient ID
        /// </summary>
        /// <returns>Patient ID or empty string if not logged in</returns>
        public string GetCurrentPatientID()
        {
            if (patientDataManager == null)
                return string.Empty;
            
            return patientDataManager.GetCurrentPatientID();
        }
        
        public void SetDifficulty(DifficultyLevel level)
        {
            if (adaptiveController != null)
                adaptiveController.SetDifficultyLevel(level);
        }
        
        private BaseTask GetTask(TaskType taskType)
        {
            return taskType switch
            {
                TaskType.BalloonPop => balloonTask,
                TaskType.PathTracing => pathTask,
                TaskType.SpiralTracing => spiralTask,
                _ => null
            };
        }
        
        public void QuitApplication()
        {
            EndCurrentTask();
            
            if (dataLogger != null)
                dataLogger.SetLoggingEnabled(false);
            
            Application.Quit();
        }
    }
    
    public enum GameState
    {
        MainMenu,
        PatientLogin,
        TaskSelection,
        TaskActive,
        Paused,
        TaskComplete
    }
    
    public enum TaskType
    {
        BalloonPop,
        PathTracing,
        SpiralTracing
    }
}

