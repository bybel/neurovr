using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using NeuroReachVR.Core;
using NeuroReachVR.UI;
using NeuroReachVR.Utils;

/// <summary>
/// Modern, elegant HUD Manager using MenuManager base class
/// Eliminates duplicate SetActive calls, uses ServiceLocator and ValidationHelper
/// </summary>
public class HUDManager : MenuManager
{
    [Header("Menu GameObjects")]
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
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_InputField patientIDInput;
    [SerializeField] private Button loginBackButton;

    [Header("Task Selection")]
    [SerializeField] private Button balloonTaskButton;
    [SerializeField] private Button pathTaskButton;
    [SerializeField] private Button spiralTaskButton;
    [SerializeField] private Button taskBackButton;

    [Header("Difficulty Buttons")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;

    [Header("Start Trial")]
    [SerializeField] private Button startTrialButton;

    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Main Menu Title")]
    [SerializeField] private TextMeshProUGUI mainMenuTitleText;

    private GameManager gameManager;
    private int score;
    private TaskType selectedTask;
    private DifficultyLevel selectedDifficulty;

    protected override void Awake()
    {
        base.Awake();

        // Get GameManager via ServiceLocator
        gameManager = ServiceLocator.Get<GameManager>();

        // Validate all required fields
        ValidateRequiredComponents();
    }

    private void Start()
    {
        InitializeButtonListeners();
        score = 0;
        UpdateScoreText();
        
        // Set welcome message
        if (mainMenuTitleText != null)
        {
            mainMenuTitleText.text = "Welcome to NeuroVR!";
        }
        
        ShowMenu("main");
    }

    protected override void InitializeMenus()
    {
        Debug.Log("[HUDManager] Registering menus...");
        
        // Register all menus
        RegisterMenu("main", mainMenu);
        RegisterMenu("patientLogin", patientLoginMenu);
        RegisterMenu("selectTask", selectTaskMenu);
        RegisterMenu("difficulty", difficultyMenu);
        RegisterMenu("startTrial", startTrialMenu);
        
        Debug.Log($"[HUDManager] Registered {menus.Count} menus.");
    }

    private void ValidateRequiredComponents()
    {
        // Validate using ValidationHelper
        ValidationHelper.ValidateRequiredFields(this,
            (mainMenu, "Main Menu"),
            (patientLoginMenu, "Patient Login Menu"),
            (selectTaskMenu, "Select Task Menu"),
            (difficultyMenu, "Difficulty Menu"),
            (startTrialMenu, "Start Trial Menu"),
            (scoreText, "Score Text"),
            (progressText, "Progress Text")
        );

        if (gameManager == null)
        {
            Debug.LogError("[HUDManager] GameManager not found! Please ensure GameManager exists in the scene.");
        }
    }

    private void InitializeButtonListeners()
    {
        Debug.Log("[HUDManager] Initializing button listeners...");
        
        // Check EventSystem - Use InputSystemUIInputModule for new Input System
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("[HUDManager] EventSystem not found! Creating one...");
            GameObject es = new GameObject("EventSystem");
            eventSystem = es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>(); // Use new Input System module
        }
        else
        {
            // Remove old StandaloneInputModule if present
            var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Debug.LogWarning("[HUDManager] Removing old StandaloneInputModule (incompatible with new Input System)...");
                Destroy(oldModule);
            }
            
            // Add InputSystemUIInputModule if missing
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                Debug.LogWarning("[HUDManager] InputSystemUIInputModule missing! Adding it...");
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
        
        // Main Menu
        if (selectTaskButton != null)
        {
            selectTaskButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Select Task clicked"); ShowMenu("selectTask"); });
        }
        else Debug.LogError("[HUDManager] selectTaskButton is NULL!");
        
        if (patientLoginButton != null)
        {
            patientLoginButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Patient Login clicked"); ShowMenu("patientLogin"); });
        }
        else Debug.LogError("[HUDManager] patientLoginButton is NULL!");
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitApplication);
        else Debug.LogError("[HUDManager] quitButton is NULL!");

        // Patient Login
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Login clicked"); OnLoginClicked(); });
        }
        else Debug.LogError("[HUDManager] loginButton is NULL!");
        
        if (loginBackButton != null)
            loginBackButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Login Back clicked"); ShowMenu("main"); });
        else Debug.LogError("[HUDManager] loginBackButton is NULL!");

        // Task Selection
        if (balloonTaskButton != null)
            balloonTaskButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Balloon Task clicked"); OnTaskSelected(TaskType.BalloonPop); });
        else Debug.LogError("[HUDManager] balloonTaskButton is NULL!");
        
        if (pathTaskButton != null)
            pathTaskButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Path Task clicked"); OnTaskSelected(TaskType.PathTracing); });
        else Debug.LogError("[HUDManager] pathTaskButton is NULL!");
        
        if (spiralTaskButton != null)
            spiralTaskButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Spiral Task clicked"); OnTaskSelected(TaskType.SpiralTracing); });
        else Debug.LogError("[HUDManager] spiralTaskButton is NULL!");
        
        if (taskBackButton != null)
            taskBackButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Task Back clicked"); ShowMenu("main"); });
        else Debug.LogError("[HUDManager] taskBackButton is NULL!");

        // Difficulty
        if (easyButton != null)
            easyButton.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Easy));
        if (mediumButton != null)
            mediumButton.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Medium));
        if (hardButton != null)
            hardButton.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Hard));

        // Start Trial
        if (startTrialButton != null)
            startTrialButton.onClick.AddListener(OnStartTrialClicked);
        
        Debug.Log("[HUDManager] Button listeners initialized.");
    }

    private void Update()
    {
        // Update score and progress from current task
        if (gameManager?.CurrentTask != null)
        {
            score = gameManager.CurrentTask.Score;
            UpdateScoreText();
            UpdateProgressText(gameManager.CurrentTask.Progress);
        }
    }

    // Button Callbacks
    private void OnLoginClicked()
    {
        Debug.Log("[HUDManager] OnLoginClicked called");
        
        string patientID = patientIDInput?.text;
        Debug.Log($"[HUDManager] Patient ID from input: '{patientID}'");

        if (!ValidationHelper.ValidateString(patientID, "Patient ID"))
        {
            Debug.LogWarning("[HUD] Please enter a valid Patient ID");
            return;
        }

        if (gameManager != null)
        {
            gameManager.SetPatientID(patientID);
            Debug.Log($"[HUD] Patient ID set: {patientID}");
            ShowMenu("selectTask");
        }
        else
        {
            Debug.LogError("[HUDManager] GameManager is NULL! Cannot set patient ID.");
        }
    }

    private void OnTaskSelected(TaskType taskType)
    {
        Debug.Log($"[HUDManager] OnTaskSelected called: {taskType}");
        selectedTask = taskType;
        Debug.Log($"[HUD] Task selected: {taskType}");
        ShowMenu("difficulty");
    }

    private void OnDifficultySelected(DifficultyLevel difficulty)
    {
        selectedDifficulty = difficulty;
        Debug.Log($"[HUD] Difficulty set to: {difficulty}");

        gameManager?.SetDifficulty(difficulty);
        ShowMenu("startTrial");
    }

    private void OnStartTrialClicked()
    {
        if (!ValidationHelper.ValidateComponent(gameManager, "GameManager"))
        {
            Debug.LogError("[HUD] Cannot start trial - GameManager is null!");
            return;
        }

        Debug.Log($"[HUD] Starting task: {selectedTask} at difficulty: {selectedDifficulty}");

        // Hide all menus
        HideAllMenus();

        // Start the task
        gameManager.StartTask(selectedTask);
    }

    private void QuitApplication()
    {
        if (gameManager != null)
            gameManager.QuitApplication();
        else
            Application.Quit();
    }

    // UI Updates
    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            progressText.text = $"Progress: {Mathf.RoundToInt(progress * 100)}%";
        }
    }

    // Public API for external access
    public void ShowMainMenu() => ShowMenu("main");
    public void ShowTaskCompletionMenu() => ShowMenu("main"); // Can be extended with completion menu
}
