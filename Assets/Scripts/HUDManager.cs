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
    [SerializeField] private Button difficultyBackButton;

    [Header("Start Trial")]
    [SerializeField] private Button startTrialButton;

    [Header("Task Completion Menu")]
    [SerializeField] private GameObject taskCompletionMenu;
    [SerializeField] private TextMeshProUGUI completionTitleText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI sessionDurationText;
    [SerializeField] private TextMeshProUGUI taskTypeText;
    [SerializeField] private TextMeshProUGUI performanceRatingText;
    [SerializeField] private TextMeshProUGUI additionalStatsText;
    [SerializeField] private Button retryTaskButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private TextMeshProUGUI pauseMenuTitle;
    [SerializeField] private TextMeshProUGUI pauseScoreText;
    [SerializeField] private TextMeshProUGUI pauseTimeText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitTaskButton;

    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Main Menu Title")]
    [SerializeField] private TextMeshProUGUI mainMenuTitleText;

    private GameManager gameManager;
    private int score;
    private TaskType selectedTask;
    private DifficultyLevel selectedDifficulty;
    
    // Task completion data
    private int lastTaskScore;
    private float lastTaskDuration;
    private string lastTaskType;
    
    // Pause state
    private bool isPaused = false;
    
    // Cached Canvas references to avoid expensive FindObjectsOfType calls
    private Canvas[] cachedCanvases;
    private Camera cachedMainCamera;

    protected override void Awake()
    {
        useAnimations = false; // Disable animations to ensure visibility and prevent fade issues
        
        // Debug: Print current Inspector assignments
        PrintInspectorAssignments();
        
        // Auto-fix wrongly assigned buttons BEFORE validation
        AutoFixWrongAssignments();
        
        // Auto-create missing UI elements BEFORE base.Awake() validates hierarchy
        CreateMissingUIElements();
        
        base.Awake();
        
        // NOTE: Do NOT get GameManager here - Unity doesn't guarantee Awake() order
        // GameManager is retrieved in Start() after all Awake() calls have completed
    }
    
    /// <summary>
    /// Debug helper: prints current Inspector assignments to help diagnose setup issues
    /// </summary>
    private void PrintInspectorAssignments()
    {
        Debug.Log("[HUDManager] === INSPECTOR ASSIGNMENTS DEBUG ===");
        Debug.Log($"  Menus:");
        Debug.Log($"    mainMenu: {(mainMenu ? mainMenu.name : "NULL")}");
        Debug.Log($"    selectTaskMenu: {(selectTaskMenu ? selectTaskMenu.name : "NULL")}");
        Debug.Log($"    difficultyMenu: {(difficultyMenu ? difficultyMenu.name : "NULL")}");
        Debug.Log($"    patientLoginMenu: {(patientLoginMenu ? patientLoginMenu.name : "NULL")}");
        Debug.Log($"  Main Menu Buttons:");
        Debug.Log($"    selectTaskButton: {GetButtonPath(selectTaskButton)}");
        Debug.Log($"    patientLoginButton: {GetButtonPath(patientLoginButton)}");
        Debug.Log($"    quitButton: {GetButtonPath(quitButton)}");
        Debug.Log($"  Task Selection Buttons:");
        Debug.Log($"    balloonTaskButton: {GetButtonPath(balloonTaskButton)}");
        Debug.Log($"    pathTaskButton: {GetButtonPath(pathTaskButton)}");
        Debug.Log($"    spiralTaskButton: {GetButtonPath(spiralTaskButton)}");
        Debug.Log($"    taskBackButton: {GetButtonPath(taskBackButton)}");
        Debug.Log($"  Difficulty Buttons:");
        Debug.Log($"    easyButton: {GetButtonPath(easyButton)}");
        Debug.Log($"    mediumButton: {GetButtonPath(mediumButton)}");
        Debug.Log($"    hardButton: {GetButtonPath(hardButton)}");
        Debug.Log($"    difficultyBackButton: {GetButtonPath(difficultyBackButton)}");
        Debug.Log("[HUDManager] === END DEBUG ===");
    }
    
    private string GetButtonPath(Component comp)
    {
        if (comp == null) return "NULL";
        
        var path = new System.Collections.Generic.List<string>();
        Transform t = comp.transform;
        while (t != null)
        {
            path.Insert(0, t.name);
            t = t.parent;
        }
        return string.Join("/", path);
    }

    private void Start()
    {
        // Get GameManager via ServiceLocator in Start() to ensure GameManager.Awake() has registered it
        // Unity guarantees all Awake() methods complete before any Start() methods are called
        gameManager = ServiceLocator.Get<GameManager>();

        // Validate all required fields (now that GameManager should be available)
        ValidateRequiredComponents();
        
        CleanupDuplicates(); // Remove accidental duplicates before initialization

        // Debug Aid: Rename buttons to match their logic so you can verify assignments in Hierarchy
        if (balloonTaskButton) balloonTaskButton.name = "Logic_BalloonBtn";
        if (pathTaskButton) pathTaskButton.name = "Logic_PathBtn";
        if (spiralTaskButton) spiralTaskButton.name = "Logic_SpiralBtn";
        if (taskBackButton) taskBackButton.name = "Logic_TaskBackBtn";
        if (selectTaskButton) selectTaskButton.name = "Logic_SelectTaskBtn";
        if (startTrialButton) startTrialButton.name = "Logic_StartTrialBtn";
        if (difficultyBackButton) difficultyBackButton.name = "Logic_DifficultyBackBtn";

        // Failsafe: Explicitly hide all menus to prevent overlap if initialization glitches
        if (mainMenu) mainMenu.SetActive(false);
        if (patientLoginMenu) patientLoginMenu.SetActive(false);
        if (selectTaskMenu) selectTaskMenu.SetActive(false);
        if (difficultyMenu) difficultyMenu.SetActive(false);
        if (startTrialMenu) startTrialMenu.SetActive(false);
        if (taskCompletionMenu) taskCompletionMenu.SetActive(false);
        if (pauseMenu) pauseMenu.SetActive(false);

        AssignCamerasToCanvases();
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

    private void CleanupDuplicates()
    {
        Debug.Log("[HUDManager] Scanning for duplicate UI elements...");
        System.Collections.Generic.HashSet<GameObject> validObjects = new System.Collections.Generic.HashSet<GameObject>();

        void AddValid(Component c) { if (c != null) validObjects.Add(c.gameObject); }
        void AddValidGO(GameObject g) { if (g != null) validObjects.Add(g); }

        // Register Valid Menus
        AddValidGO(mainMenu); AddValidGO(patientLoginMenu); AddValidGO(selectTaskMenu);
        AddValidGO(difficultyMenu); AddValidGO(startTrialMenu);

        // Register Valid Buttons
        AddValid(selectTaskButton); AddValid(patientLoginButton); AddValid(quitButton);
        AddValid(loginButton); AddValid(loginBackButton);
        AddValid(balloonTaskButton); AddValid(pathTaskButton); AddValid(spiralTaskButton); AddValid(taskBackButton);
        AddValid(easyButton); AddValid(mediumButton); AddValid(hardButton); AddValid(difficultyBackButton);
        AddValid(startTrialButton);

        // Register Valid Inputs
        AddValid(patientIDInput);

        // Scan scene for impostors
        Button[] allButtons = FindObjectsOfType<Button>(true);
        int disabledCount = 0;

        foreach (var btn in allButtons)
        {
            // If this button is NOT in our valid list
            if (!validObjects.Contains(btn.gameObject))
            {
                // Check if it mimics a valid object (same name)
                foreach (var valid in validObjects)
                {
                    if (btn.name == valid.name)
                    {
                        Debug.LogWarning($"[HUDManager] Found Duplicate/Ghost Button: '{btn.name}'. Disabling it.");
                        btn.gameObject.SetActive(false);
                        btn.name += "_DISABLED_DUPLICATE";
                        disabledCount++;
                        break;
                    }
                }
            }
        }
    }

    private void AssignCamerasToCanvases()
    {
        // Cache canvases and camera to avoid expensive FindObjectsOfType calls in Update()
        CacheCanvasReferences();

        if (cachedMainCamera != null)
        {
            foreach (var canvas in cachedCanvases)
            {
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                {
                    canvas.worldCamera = cachedMainCamera;
                }
            }
        }
    }
    
    /// <summary>
    /// Caches Canvas and Camera references to avoid expensive FindObjectsOfType calls.
    /// Call this if canvases are dynamically added/removed from the scene.
    /// </summary>
    public void CacheCanvasReferences()
    {
        // Use legacy API for maximum stability/compatibility - only called once during init
        cachedCanvases = FindObjectsOfType<Canvas>(true);
        cachedMainCamera = Camera.main;
        if (cachedMainCamera == null) 
            cachedMainCamera = FindFirstObjectByType<Camera>();
        
        Debug.Log($"[HUDManager] Cached {cachedCanvases.Length} canvas references and main camera.");
    }

    protected override void InitializeMenus()
    {
        // Smart Fix: Check for Inspector misassignments (Menu assigned to a Button instead of Panel)
        if (selectTaskMenu != null)
        {
            if ((spiralTaskButton != null && selectTaskMenu == spiralTaskButton.gameObject) ||
                (balloonTaskButton != null && selectTaskMenu == balloonTaskButton.gameObject) ||
                (pathTaskButton != null && selectTaskMenu == pathTaskButton.gameObject) ||
                (startTrialButton != null && selectTaskMenu == startTrialButton.gameObject))
            {
                Debug.LogWarning("[HUDManager] selectTaskMenu was assigned to a Button! Auto-fixing to Parent Panel.");
                if (selectTaskMenu.transform.parent != null)
                    selectTaskMenu = selectTaskMenu.transform.parent.gameObject;
            }
        }

        // Validate scene hierarchy - buttons should already be children of their menus
        // NOTE: Runtime reparenting was removed as it breaks layouts and causes visual glitches.
        // If buttons are not properly parented, fix the scene hierarchy in Unity Editor instead.
        ValidateMenuHierarchy();

        Debug.Log("[HUDManager] Registering menus...");
        
        // Register all menus
        RegisterMenu("main", mainMenu);
        RegisterMenu("patientLogin", patientLoginMenu);
        RegisterMenu("selectTask", selectTaskMenu);
        RegisterMenu("difficulty", difficultyMenu);
        RegisterMenu("startTrial", startTrialMenu);
        RegisterMenu("taskCompletion", taskCompletionMenu);
        RegisterMenu("pause", pauseMenu);
        
        Debug.Log($"[HUDManager] Registered {menus.Count} menus.");
    }

    /// <summary>
    /// Validates that UI elements are properly parented in the scene hierarchy.
    /// Logs warnings for any misconfigurations that need to be fixed in the Unity Editor.
    /// </summary>
    private void ValidateMenuHierarchy()
    {
        var issues = new System.Collections.Generic.List<string>();

        // Check Task Selection Menu hierarchy
        if (selectTaskMenu != null)
        {
            CheckAndLogHierarchy(spiralTaskButton, selectTaskMenu, "spiralTaskButton", "selectTaskMenu", issues);
            CheckAndLogHierarchy(balloonTaskButton, selectTaskMenu, "balloonTaskButton", "selectTaskMenu", issues);
            CheckAndLogHierarchy(pathTaskButton, selectTaskMenu, "pathTaskButton", "selectTaskMenu", issues);
            CheckAndLogHierarchy(taskBackButton, selectTaskMenu, "taskBackButton", "selectTaskMenu", issues);
        }

        // Check Patient Login Menu hierarchy
        if (patientLoginMenu != null)
        {
            CheckAndLogHierarchy(loginBackButton, patientLoginMenu, "loginBackButton", "patientLoginMenu", issues);
            CheckAndLogHierarchy(loginButton, patientLoginMenu, "loginButton", "patientLoginMenu", issues);
            CheckAndLogHierarchy(patientIDInput, patientLoginMenu, "patientIDInput", "patientLoginMenu", issues);
        }

        // Check Main Menu hierarchy
        if (mainMenu != null)
        {
            CheckAndLogHierarchy(selectTaskButton, mainMenu, "selectTaskButton", "mainMenu", issues);
            CheckAndLogHierarchy(patientLoginButton, mainMenu, "patientLoginButton", "mainMenu", issues);
            CheckAndLogHierarchy(quitButton, mainMenu, "quitButton", "mainMenu", issues);
        }

        // Check Difficulty Menu hierarchy
        if (difficultyMenu != null)
        {
            CheckAndLogHierarchy(easyButton, difficultyMenu, "easyButton", "difficultyMenu", issues);
            CheckAndLogHierarchy(mediumButton, difficultyMenu, "mediumButton", "difficultyMenu", issues);
            CheckAndLogHierarchy(hardButton, difficultyMenu, "hardButton", "difficultyMenu", issues);
            CheckAndLogHierarchy(difficultyBackButton, difficultyMenu, "difficultyBackButton", "difficultyMenu", issues);
        }

        // Check Start Trial Menu hierarchy
        if (startTrialMenu != null)
        {
            CheckAndLogHierarchy(startTrialButton, startTrialMenu, "startTrialButton", "startTrialMenu", issues);
        }

        if (issues.Count > 0)
        {
            Debug.LogError($"[HUDManager] Found {issues.Count} hierarchy issue(s):\n" + string.Join("\n", issues) + 
                           "\n\nFix in Unity Editor: select HUDManager and reassign the buttons from the correct menu canvases.");
        }
        else
        {
            Debug.Log("[HUDManager] Menu hierarchy validation passed.");
        }
    }
    
    private void CheckAndLogHierarchy(Component child, GameObject expectedParent, string childName, string parentName, System.Collections.Generic.List<string> issues)
    {
        if (child == null || expectedParent == null)
            return;

        if (!child.transform.IsChildOf(expectedParent.transform))
        {
            string actualParent = child.transform.parent != null ? child.transform.parent.name : "None (root)";
            string actualCanvas = GetParentCanvasName(child.transform);
            issues.Add($"  • {childName}: is in '{actualCanvas}' but should be in '{parentName}' ({expectedParent.name})");
        }
    }
    
    private string GetParentCanvasName(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            if (current.GetComponent<Canvas>() != null)
                return current.name;
            current = current.parent;
        }
        return t.parent?.name ?? "Unknown";
    }


    private void ValidateRequiredComponents()
    {
        // Note: CreateMissingUIElements() is already called in Awake() before hierarchy validation
        
        // Validate using ValidationHelper (only truly required fields)
        ValidationHelper.ValidateRequiredFields(this,
            (mainMenu, "Main Menu"),
            (patientLoginMenu, "Patient Login Menu"),
            (selectTaskMenu, "Select Task Menu"),
            (difficultyMenu, "Difficulty Menu"),
            (startTrialMenu, "Start Trial Menu"),
            (scoreText, "Score Text")
            // progressText is now optional - auto-created if missing
        );

        // Fallback: If GameManager is still null, try direct scene search
        if (gameManager == null)
        {
            Debug.LogWarning("[HUDManager] GameManager not found via ServiceLocator, attempting direct scene search...");
            gameManager = FindFirstObjectByType<GameManager>();
            
            if (gameManager != null)
            {
                Debug.Log("[HUDManager] GameManager found via direct scene search.");
            }
            else
            {
                Debug.LogError("[HUDManager] GameManager not found! Please ensure GameManager exists in the scene and is active.");
            }
        }
    }
    
    /// <summary>
    /// Attempts to auto-fix wrongly assigned Inspector references by finding buttons in correct menus.
    /// </summary>
    private void AutoFixWrongAssignments()
    {
        // Fix loginButton - should be in patientLoginMenu (LogInMenuCanvas), not MainMenuCanvas
        if (loginButton != null && patientLoginMenu != null && !loginButton.transform.IsChildOf(patientLoginMenu.transform))
        {
            Debug.Log("[HUDManager] Auto-fixing loginButton assignment...");
            // Find a button in patientLoginMenu that could be the login button
            var buttonsInLoginMenu = patientLoginMenu.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttonsInLoginMenu)
            {
                string btnName = btn.name.ToLower();
                // Look for button with "login" or "log" in name (not "back")
                if ((btnName.Contains("login") || btnName.Contains("log")) && !btnName.Contains("back"))
                {
                    Debug.Log($"[HUDManager] Found login button in correct menu: {btn.name}");
                    loginButton = btn;
                    break;
                }
            }
        }
        
        // Fix startTrialMenu assignment - might be pointing to wrong canvas
        if (startTrialMenu != null && startTrialButton != null)
        {
            // If startTrialButton exists but isn't a child of startTrialMenu, find correct menu
            if (!startTrialButton.transform.IsChildOf(startTrialMenu.transform))
            {
                Debug.Log("[HUDManager] Auto-fixing startTrialMenu assignment...");
                // Find the parent canvas of startTrialButton
                Transform parent = startTrialButton.transform;
                while (parent != null)
                {
                    if (parent.GetComponent<Canvas>() != null || parent.name.Contains("Menu"))
                    {
                        Debug.Log($"[HUDManager] Reassigning startTrialMenu to: {parent.name}");
                        startTrialMenu = parent.gameObject;
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }
        
        // Alternative: If startTrialMenu is wrong, search scene for StartTaskMenu
        if (startTrialMenu != null && startTrialMenu.name == "TaskSelectionCanvas")
        {
            Debug.Log("[HUDManager] startTrialMenu is wrongly set to TaskSelectionCanvas, searching for StartTaskMenu...");
            // Find StartTaskMenu in scene
            var allGameObjects = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in allGameObjects)
            {
                if (canvas.name.Contains("StartTask") || canvas.name.Contains("StartTrial"))
                {
                    Debug.Log($"[HUDManager] Found correct start menu: {canvas.name}");
                    startTrialMenu = canvas.gameObject;
                    break;
                }
            }
            
            // Also try finding by transform name
            var startMenu = GameObject.Find("StartTaskMenu");
            if (startMenu != null)
            {
                Debug.Log($"[HUDManager] Found StartTaskMenu by name");
                startTrialMenu = startMenu;
            }
        }
    }
    
    /// <summary>
    /// Auto-creates missing UI elements at runtime to prevent null reference errors.
    /// This is a fallback for when elements aren't assigned in the Inspector.
    /// </summary>
    private void CreateMissingUIElements()
    {
        // Create Progress Text if missing
        if (progressText == null && scoreText != null)
        {
            Debug.Log("[HUDManager] Auto-creating ProgressText...");
            GameObject progressObj = new GameObject("ProgressText_AutoCreated");
            progressObj.transform.SetParent(scoreText.transform.parent, false);
            progressText = progressObj.AddComponent<TextMeshProUGUI>();
            progressText.text = "Progress: 0%";
            progressText.fontSize = scoreText.fontSize;
            progressText.color = scoreText.color;
            progressText.alignment = scoreText.alignment;
            
            // Position below scoreText
            RectTransform rect = progressObj.GetComponent<RectTransform>();
            RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
            rect.anchorMin = scoreRect.anchorMin;
            rect.anchorMax = scoreRect.anchorMax;
            rect.pivot = scoreRect.pivot;
            rect.anchoredPosition = scoreRect.anchoredPosition - new Vector2(0, scoreRect.rect.height + 10);
            rect.sizeDelta = scoreRect.sizeDelta;
        }
        
        // Create Difficulty Back Button if missing
        if (difficultyBackButton == null && difficultyMenu != null)
        {
            Debug.Log("[HUDManager] Auto-creating DifficultyBackButton...");
            
            // Find the DifficultyButtons container or use difficultyMenu directly
            Transform buttonContainer = difficultyMenu.transform.Find("DifficultyButtons");
            if (buttonContainer == null)
                buttonContainer = difficultyMenu.transform;
            
            // Try to clone an existing button for consistent styling
            Button templateButton = easyButton ?? mediumButton ?? hardButton;
            
            if (templateButton != null)
            {
                // Clone existing button
                GameObject backButtonObj = Instantiate(templateButton.gameObject, buttonContainer);
                backButtonObj.name = "DifficultyBackButton_AutoCreated";
                difficultyBackButton = backButtonObj.GetComponent<Button>();
                
                // Clear existing listeners from clone
                difficultyBackButton.onClick.RemoveAllListeners();
                
                // Update text
                var tmpText = backButtonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null)
                    tmpText.text = "Back";
                
                // Position at bottom
                RectTransform rect = backButtonObj.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - 150);
            }
            else
            {
                // Create from scratch
                GameObject backButtonObj = new GameObject("DifficultyBackButton_AutoCreated");
                backButtonObj.transform.SetParent(buttonContainer, false);
                
                // Add Image component (required for Button)
                var image = backButtonObj.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                
                // Add Button component
                difficultyBackButton = backButtonObj.AddComponent<Button>();
                difficultyBackButton.targetGraphic = image;
                
                // Set size
                RectTransform rect = backButtonObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 50);
                rect.anchoredPosition = new Vector2(0, -200);
                
                // Add text
                GameObject textObj = new GameObject("Text (TMP)");
                textObj.transform.SetParent(backButtonObj.transform, false);
                var tmpText = textObj.AddComponent<TextMeshProUGUI>();
                tmpText.text = "Back";
                tmpText.fontSize = 24;
                tmpText.color = Color.white;
                tmpText.alignment = TextAlignmentOptions.Center;
                
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
            
            Debug.Log("[HUDManager] DifficultyBackButton auto-created successfully.");
        }
        
        // Create "Continue as Guest" button on login menu (VR-friendly - no keyboard needed)
        CreateGuestButtonOnLoginMenu();
    }
    
    /// <summary>
    /// Creates a "Continue as Guest" button on the login menu for VR users who can't easily type
    /// </summary>
    private void CreateGuestButtonOnLoginMenu()
    {
        if (patientLoginMenu == null) return;
        
        // Check if guest button already exists
        var existingGuestBtn = patientLoginMenu.transform.Find("GuestButton_AutoCreated");
        if (existingGuestBtn != null) return;
        
        Debug.Log("[HUDManager] Auto-creating 'Continue as Guest' button...");
        
        // Find button container in login menu
        Transform buttonContainer = patientLoginMenu.transform.Find("LoginButtons");
        if (buttonContainer == null)
            buttonContainer = patientLoginMenu.transform;
        
        // Try to clone an existing button for consistent styling
        Button templateButton = loginButton ?? loginBackButton;
        
        GameObject guestButtonObj;
        Button guestButton;
        
        if (templateButton != null)
        {
            // Clone existing button
            guestButtonObj = Instantiate(templateButton.gameObject, buttonContainer);
            guestButtonObj.name = "GuestButton_AutoCreated";
            guestButton = guestButtonObj.GetComponent<Button>();
            guestButton.onClick.RemoveAllListeners();
            
            // Update text
            var tmpText = guestButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = "Continue as Guest";
            
            // Position below other buttons
            RectTransform rect = guestButtonObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - 80);
        }
        else
        {
            // Create from scratch
            guestButtonObj = new GameObject("GuestButton_AutoCreated");
            guestButtonObj.transform.SetParent(buttonContainer, false);
            
            var image = guestButtonObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.6f, 0.2f, 0.9f); // Green tint for guest
            
            guestButton = guestButtonObj.AddComponent<Button>();
            guestButton.targetGraphic = image;
            
            RectTransform rect = guestButtonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 60);
            rect.anchoredPosition = new Vector2(0, -150);
            
            GameObject textObj = new GameObject("Text (TMP)");
            textObj.transform.SetParent(guestButtonObj.transform, false);
            var tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "Continue as Guest";
            tmpText.fontSize = 24;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        // Add click listener for guest mode
        guestButton.onClick.AddListener(OnSkipLoginClicked);
        
        Debug.Log("[HUDManager] 'Continue as Guest' button created successfully.");
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
        if (difficultyBackButton != null)
            difficultyBackButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Difficulty Back clicked"); ShowMenu("selectTask"); });
        else Debug.LogWarning("[HUDManager] difficultyBackButton is NULL - was not auto-created (missing difficultyMenu?)");

        // Start Trial
        if (startTrialButton != null)
            startTrialButton.onClick.AddListener(OnStartTrialClicked);

        // Task Completion Menu
        if (retryTaskButton != null)
            retryTaskButton.onClick.AddListener(OnRetryTaskClicked);
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(() => { Debug.Log("[HUDManager] Back to Menu clicked"); ShowMenu("main"); });

        // Pause Menu
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        else
            Debug.LogWarning("[HUDManager] resumeButton is NULL - pause menu may not be set up in scene");
        
        if (quitTaskButton != null)
            quitTaskButton.onClick.AddListener(OnQuitTaskClicked);
        else
            Debug.LogWarning("[HUDManager] quitTaskButton is NULL - pause menu may not be set up in scene");

        // Fix: Force enable task selection buttons in case they are disabled in scene
        if (balloonTaskButton) balloonTaskButton.gameObject.SetActive(true);
        if (pathTaskButton) pathTaskButton.gameObject.SetActive(true);
        if (spiralTaskButton) spiralTaskButton.gameObject.SetActive(true);
        if (taskBackButton) taskBackButton.gameObject.SetActive(true);
        
        Debug.Log("[HUDManager] Button listeners initialized.");
    }

    private float nextCameraCheckTime;

    private void Update()
    {
        // Periodically ensure active canvases have cameras (every 2 seconds)
        // Uses cached references to avoid expensive FindObjectsOfType calls
        if (Time.time >= nextCameraCheckTime)
        {
            // Refresh camera reference in case it changed (e.g., scene reload)
            if (cachedMainCamera == null)
            {
                cachedMainCamera = Camera.main;
                if (cachedMainCamera == null)
                    cachedMainCamera = FindFirstObjectByType<Camera>();
            }

            if (cachedMainCamera != null && cachedCanvases != null)
            {
                foreach (var canvas in cachedCanvases)
                {
                    // Check for null in case canvas was destroyed
                    if (canvas != null && canvas.gameObject.activeInHierarchy && 
                        canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    {
                        canvas.worldCamera = cachedMainCamera;
                    }
                }
            }
            nextCameraCheckTime = Time.time + 2.0f;
        }

        // Check for Pause Input (works in both Editor and VR builds)
        if (gameManager != null)
        {
            bool pausePressed = false;
            
            // Keyboard input (Escape or P key)
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                pausePressed = UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                              UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame;
            }
            
            // VR Controller Menu Button (Meta Quest / Oculus controllers)
            // Check both hands for menu button press
            var rightController = UnityEngine.InputSystem.XR.XRController.rightHand;
            var leftController = UnityEngine.InputSystem.XR.XRController.leftHand;
            
            if (!pausePressed && rightController != null)
            {
                var menuButton = rightController.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("menuButton");
                if (menuButton != null && menuButton.wasPressedThisFrame)
                    pausePressed = true;
            }
            
            if (!pausePressed && leftController != null)
            {
                var menuButton = leftController.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("menuButton");
                if (menuButton != null && menuButton.wasPressedThisFrame)
                    pausePressed = true;
            }
            
            if (pausePressed)
            {
                TogglePause();
            }
        }

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

        // If no ID provided, auto-generate one (VR-friendly - no keyboard needed)
        if (string.IsNullOrWhiteSpace(patientID))
        {
            patientID = GenerateGuestID();
            Debug.Log($"[HUD] Auto-generated Guest ID: {patientID}");
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
    
    /// <summary>
    /// Generates a guest patient ID for quick testing/demo without keyboard input
    /// </summary>
    private string GenerateGuestID()
    {
        string timestamp = System.DateTime.Now.ToString("MMdd_HHmm");
        return $"Guest_{timestamp}";
    }
    
    /// <summary>
    /// Skip login and go directly to task selection (Guest Mode)
    /// Called from "Skip" or "Guest" button if added
    /// </summary>
    public void OnSkipLoginClicked()
    {
        Debug.Log("[HUDManager] Skip Login clicked - entering Guest Mode");
        
        string guestID = GenerateGuestID();
        
        if (gameManager != null)
        {
            gameManager.SetPatientID(guestID);
            Debug.Log($"[HUD] Guest mode activated with ID: {guestID}");
        }
        
        ShowMenu("selectTask");
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

        // Auto-login as guest if no patient is logged in (VR-friendly)
        if (!gameManager.IsPatientLoggedIn())
        {
            string guestID = GenerateGuestID();
            gameManager.SetPatientID(guestID);
            Debug.Log($"[HUD] Auto-logged in as guest: {guestID}");
        }

        Debug.Log($"[HUD] Starting task: {selectedTask} at difficulty: {selectedDifficulty} for patient: {gameManager.GetCurrentPatientID()}");

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
    
    /// <summary>
    /// Called by GameManager when a task ends to immediately update the HUD.
    /// This method receives the task data directly since the task will be null after EndTask().
    /// </summary>
    /// <param name="taskType">Name of the completed task type</param>
    /// <param name="score">Final score achieved</param>
    /// <param name="duration">Duration of the task in seconds</param>
    public void OnTaskCompleted(string taskType, int score, float duration)
    {
        Debug.Log($"[HUDManager] Task completion notification received - Type: {taskType}, Score: {score}, Duration: {duration:F1}s");
        
        // Store completion data
        lastTaskScore = score;
        lastTaskDuration = duration;
        lastTaskType = GetFriendlyTaskName(taskType);
        
        // Populate and show the completion menu
        PopulateCompletionMenu();
        ShowMenu("taskCompletion");
    }
    
    /// <summary>
    /// Converts task class name to user-friendly display name
    /// </summary>
    private string GetFriendlyTaskName(string taskClassName)
    {
        return taskClassName switch
        {
            "BalloonPopTask" => "Balloon Pop",
            "PathTracingTask" => "Path Tracing",
            "SpiralTracingTask" => "Spiral Tracing",
            _ => taskClassName.Replace("Task", "").Trim()
        };
    }
    
    /// <summary>
    /// Shows the task completion menu with performance summary
    /// Call this after a task ends to show the user their results
    /// </summary>
    public void ShowTaskCompletionMenu()
    {
        // Capture completion data from the current task before it's cleared
        if (gameManager?.CurrentTask != null)
        {
            lastTaskScore = gameManager.CurrentTask.Score;
            lastTaskDuration = gameManager.CurrentTask.ElapsedTime;
            lastTaskType = GetTaskDisplayName(selectedTask);
        }
        
        // Populate the completion menu with stats
        PopulateCompletionMenu();
        
        // Show the completion menu
        ShowMenu("taskCompletion");
        
        Debug.Log($"[HUDManager] Showing task completion - Score: {lastTaskScore}, Duration: {lastTaskDuration:F1}s, Task: {lastTaskType}");
    }
    
    /// <summary>
    /// Populates the task completion menu with performance data
    /// </summary>
    private void PopulateCompletionMenu()
    {
        // Title
        if (completionTitleText != null)
            completionTitleText.text = "Task Complete!";
        
        // Task type
        if (taskTypeText != null)
            taskTypeText.text = $"Task: {lastTaskType}";
        
        // Final score
        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {lastTaskScore}";
        
        // Session duration
        if (sessionDurationText != null)
        {
            int minutes = Mathf.FloorToInt(lastTaskDuration / 60f);
            int seconds = Mathf.FloorToInt(lastTaskDuration % 60f);
            sessionDurationText.text = $"Duration: {minutes:00}:{seconds:00}";
        }
        
        // Performance rating based on score and time
        if (performanceRatingText != null)
        {
            string rating = CalculatePerformanceRating(lastTaskScore, lastTaskDuration);
            performanceRatingText.text = $"Rating: {rating}";
        }
        
        // Additional stats
        if (additionalStatsText != null)
        {
            float pointsPerSecond = lastTaskDuration > 0 ? lastTaskScore / lastTaskDuration : 0;
            additionalStatsText.text = $"Avg: {pointsPerSecond:F1} pts/sec\nDifficulty: {selectedDifficulty}";
        }
    }
    
    /// <summary>
    /// Calculates a performance rating based on score and duration
    /// </summary>
    private string CalculatePerformanceRating(int score, float duration)
    {
        if (duration <= 0) return "N/A";
        
        float efficiency = score / duration;
        
        // Adjust thresholds based on difficulty
        float multiplier = selectedDifficulty switch
        {
            DifficultyLevel.Easy => 1.0f,
            DifficultyLevel.Medium => 0.8f,
            DifficultyLevel.Hard => 0.6f,
            _ => 1.0f
        };
        
        float adjustedEfficiency = efficiency / multiplier;
        
        return adjustedEfficiency switch
        {
            >= 5.0f => "★★★★★ Excellent!",
            >= 3.5f => "★★★★☆ Great!",
            >= 2.0f => "★★★☆☆ Good",
            >= 1.0f => "★★☆☆☆ Fair",
            _ => "★☆☆☆☆ Keep Practicing"
        };
    }
    
    /// <summary>
    /// Gets a user-friendly display name for the task type
    /// </summary>
    private string GetTaskDisplayName(TaskType taskType)
    {
        return taskType switch
        {
            TaskType.BalloonPop => "Balloon Pop",
            TaskType.PathTracing => "Path Tracing",
            TaskType.SpiralTracing => "Spiral Tracing",
            _ => "Unknown Task"
        };
    }
    
    /// <summary>
    /// Callback for retry button - restarts the same task
    /// </summary>
    private void OnRetryTaskClicked()
    {
        Debug.Log($"[HUDManager] Retrying task: {selectedTask}");
        
        if (!ValidationHelper.ValidateComponent(gameManager, "GameManager"))
        {
            Debug.LogError("[HUD] Cannot retry task - GameManager is null!");
            ShowMenu("main");
            return;
        }
        
        // Hide menus and restart the task
        HideAllMenus();
        gameManager.StartTask(selectedTask);
    }
    
    #region Pause Menu
    
    /// <summary>
    /// Toggles the pause state of the current task
    /// </summary>
    public void TogglePause()
    {
        if (gameManager == null) return;
        
        // If we're paused, resume
        if (isPaused)
        {
            OnResumeClicked();
            return;
        }
        
        // If a task is active, pause it
        if (gameManager.CurrentState == GameState.TaskActive && gameManager.CurrentTask != null)
        {
            ShowPauseMenu();
        }
    }
    
    /// <summary>
    /// Shows the pause menu and pauses the game
    /// </summary>
    public void ShowPauseMenu()
    {
        if (gameManager == null || gameManager.CurrentTask == null) return;
        
        Debug.Log("[HUDManager] Pausing game...");
        
        isPaused = true;
        gameManager.PauseCurrentTask();
        
        // Update pause menu UI with current task info
        UpdatePauseMenuUI();
        
        // Show pause menu
        ShowMenu("pause");
        
        // Optionally pause game time (uncomment if you want full time freeze)
        // Time.timeScale = 0f;
    }
    
    /// <summary>
    /// Hides the pause menu and resumes the game
    /// </summary>
    public void HidePauseMenu()
    {
        Debug.Log("[HUDManager] Resuming game...");
        
        isPaused = false;
        
        // Resume game time if it was paused
        // Time.timeScale = 1f;
        
        // Hide the pause menu
        HideMenu("pause");
        
        // Resume the task
        if (gameManager != null)
        {
            gameManager.ResumeCurrentTask();
        }
    }
    
    /// <summary>
    /// Updates the pause menu UI with current task information
    /// </summary>
    private void UpdatePauseMenuUI()
    {
        if (gameManager?.CurrentTask == null) return;
        
        var task = gameManager.CurrentTask;
        
        if (pauseMenuTitle != null)
            pauseMenuTitle.text = "Game Paused";
        
        if (pauseScoreText != null)
            pauseScoreText.text = $"Current Score: {task.Score}";
        
        if (pauseTimeText != null)
        {
            float remainingTime = task.RemainingTime;
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            pauseTimeText.text = $"Time Remaining: {minutes:00}:{seconds:00}";
        }
    }
    
    /// <summary>
    /// Callback for resume button - resumes the paused task
    /// </summary>
    private void OnResumeClicked()
    {
        Debug.Log("[HUDManager] Resume button clicked");
        HidePauseMenu();
    }
    
    /// <summary>
    /// Callback for quit task button - ends the current task and returns to main menu
    /// </summary>
    private void OnQuitTaskClicked()
    {
        Debug.Log("[HUDManager] Quit Task button clicked");
        
        isPaused = false;
        
        // Resume game time if it was paused
        // Time.timeScale = 1f;
        
        if (gameManager != null)
        {
            // Show completion screen with current progress
            ShowTaskCompletionMenu();
            gameManager.EndCurrentTask();
        }
        else
        {
            ShowMainMenu();
        }
    }
    
    /// <summary>
    /// Check if the game is currently paused
    /// </summary>
    public bool IsPaused => isPaused;
    
    #endregion
}
