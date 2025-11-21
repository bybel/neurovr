using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuroReachVR.Core;
using NeuroReachVR.UI;

namespace NeuroReachVR.UI
{
    /// <summary>
    /// Modern HUD Manager with smooth animations and elegant transitions
    /// Refactored for clean architecture and better UX
    /// </summary>
    public class ModernHUDManager : MonoBehaviour
    {
        [Header("Menu References")]
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject patientLoginMenu;
        [SerializeField] private GameObject selectTaskMenu;
        [SerializeField] private GameObject difficultyMenu;
        [SerializeField] private GameObject startTrialMenu;
        
        [Header("Main Menu Buttons")]
        [SerializeField] private Button selectTaskButton;
        [SerializeField] private Button patientLoginButton;
        [SerializeField] private Button quitButton;
        
        [Header("Patient Login")]
        [SerializeField] private Button logPatientButton;
        [SerializeField] private TMP_InputField patientIDInput;
        [SerializeField] private Button backToMainButton;
        
        [Header("Task Selection")]
        [SerializeField] private Button spiralTaskButton;
        [SerializeField] private Button pathTaskButton;
        [SerializeField] private Button backToMainFromTasksButton;
        
        [Header("Difficulty Selection")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button mediumButton;
        [SerializeField] private Button hardButton;
        
        [Header("Trial Start")]
        [SerializeField] private Button startTrialButton;
        
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI progressText;
        
        [Header("Animation")]
        [SerializeField] private UIAnimationManager animationManager;
        
        private const string SCORE_FORMAT = "Score: {0}";
        private const string PROGRESS_FORMAT = "Progress: {0:P0}";
        
        private int score;
        private GameManager gameManager;
        private PatientDataManager patientDataManager;
        
        private void Awake()
        {
            if (animationManager == null)
                animationManager = GetComponent<UIAnimationManager>();
            
            if (animationManager == null)
                animationManager = gameObject.AddComponent<UIAnimationManager>();
            
            gameManager = FindFirstObjectByType<GameManager>();
            patientDataManager = FindFirstObjectByType<PatientDataManager>();
        }
        
        private void Start()
        {
            InitializeButtons();
            ShowMainMenu();
            UpdateScore(0);
        }
        
        private void InitializeButtons()
        {
            // Main Menu
            selectTaskButton?.onClick.AddListener(ShowSelectTaskMenu);
            patientLoginButton?.onClick.AddListener(ShowPatientLoginMenu);
            quitButton?.onClick.AddListener(OnQuit);
            
            // Patient Login
            logPatientButton?.onClick.AddListener(OnLogPatient);
            backToMainButton?.onClick.AddListener(ShowMainMenu);
            
            // Task Selection
            spiralTaskButton?.onClick.AddListener(() => OnTaskSelected(TaskType.SpiralTracing));
            pathTaskButton?.onClick.AddListener(() => OnTaskSelected(TaskType.PathTracing));
            backToMainFromTasksButton?.onClick.AddListener(ShowMainMenu);
            
            // Difficulty
            easyButton?.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Easy));
            mediumButton?.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Medium));
            hardButton?.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Hard));
            
            // Trial Start
            startTrialButton?.onClick.AddListener(OnStartTrial);
        }
        
        public void ShowMainMenu()
        {
            HideAllMenus();
            ShowMenu(mainMenu);
        }
        
        public void ShowPatientLoginMenu()
        {
            HideAllMenus();
            ShowMenu(patientLoginMenu);
        }
        
        public void ShowSelectTaskMenu()
        {
            HideAllMenus();
            ShowMenu(selectTaskMenu);
        }
        
        public void ShowDifficultyMenu()
        {
            HideAllMenus();
            ShowMenu(difficultyMenu);
        }
        
        public void ShowStartTrialMenu()
        {
            HideAllMenus();
            ShowMenu(startTrialMenu);
        }
        
        private void ShowMenu(GameObject menu)
        {
            if (menu == null) return;
            menu.SetActive(true);
            animationManager?.FadeIn(menu);
        }
        
        private void HideAllMenus()
        {
            HideMenu(mainMenu);
            HideMenu(patientLoginMenu);
            HideMenu(selectTaskMenu);
            HideMenu(difficultyMenu);
            HideMenu(startTrialMenu);
        }
        
        private void HideMenu(GameObject menu)
        {
            if (menu == null || !menu.activeSelf) return;
            animationManager?.FadeOut(menu);
        }
        
        private void OnLogPatient()
        {
            string patientID = patientIDInput?.text ?? "";
            if (string.IsNullOrEmpty(patientID))
            {
                Debug.LogWarning("[HUD] Patient ID is empty");
                return;
            }
            
            patientDataManager?.SetCurrentPatient(patientID);
            gameManager?.SetPatientID(patientID);
            Debug.Log($"[HUD] Patient logged: {patientID}");
            
            ShowMainMenu();
        }
        
        private void OnTaskSelected(TaskType taskType)
        {
            ShowDifficultyMenu();
        }
        
        private void OnDifficultySelected(DifficultyLevel difficulty)
        {
            gameManager?.SetDifficulty(difficulty);
            ShowStartTrialMenu();
        }
        
        private void OnStartTrial()
        {
            // Task start logic handled by GameManager
            if (gameManager != null)
            {
                // Get task type from context (simplified - could be improved)
                gameManager.StartTask(TaskType.BalloonPop);
            }
            
            HideAllMenus();
        }
        
        private void OnQuit()
        {
            gameManager?.QuitApplication();
        }
        
        public void UpdateScore(int newScore)
        {
            score = newScore;
            if (scoreText != null)
                scoreText.text = string.Format(SCORE_FORMAT, score);
        }
        
        public void AddScore(int points)
        {
            UpdateScore(score + points);
        }
        
        public void UpdateProgress(float progress)
        {
            if (progressText != null)
                progressText.text = string.Format(PROGRESS_FORMAT, progress);
        }
    }
}

