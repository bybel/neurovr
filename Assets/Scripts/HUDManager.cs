using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeuroReachVR.Core;
using NeuroReachVR.UI;
using System.Collections;

/// <summary>
/// Clean, VR-Optimized HUD Manager.
/// Handles Menu Navigation, VR Positioning, and Task Feedback.
/// </summary>
public class HUDManager : MenuManager
{
    [Header("Menus")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject patientLoginMenu;
    [SerializeField] private GameObject selectTaskMenu;
    [SerializeField] private GameObject difficultyMenu;
    [SerializeField] private GameObject startTrialMenu;
    [SerializeField] private GameObject taskCompletionMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject gameplayHUD;
    [SerializeField] private GameObject durationMenu; // New Menu

    [Header("Buttons - Main")]
    [SerializeField] private Button selectTaskButton;
    [SerializeField] private Button patientLoginButton;
    [SerializeField] private Button quitButton;

    [Header("Buttons - Login")]
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_InputField patientIDInput;
    [SerializeField] private Button loginBackButton;

    [Header("Buttons - Task Select")]
    [SerializeField] private Button balloonTaskButton;
    [SerializeField] private Button pathTaskButton;
    [SerializeField] private Button spiralTaskButton;
    [SerializeField] private Button taskBackButton;

    [Header("Buttons - Difficulty")]
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button difficultyBackButton;
    
    [Header("Buttons - Duration")]
    [SerializeField] private Button duration15Button;
    [SerializeField] private Button duration30Button;
    [SerializeField] private Button duration60Button;
    [SerializeField] private Button durationBackButton;

    [Header("Buttons - Start")]
    [SerializeField] private Button startTrialButton;

    [Header("Buttons - Completion")]
    [SerializeField] private Button retryTaskButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Buttons - Pause")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitTaskButton;

    [Header("Text Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI mainMenuTitleText;
    
    public void SetProgressText(string text)
    {
        if (progressText != null) progressText.text = text;
    }
    
    [Header("Stylus Visuals")]
    [SerializeField] private Vector3 stylusPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 stylusRotationOffset = new Vector3(60, 0, 0);
    [SerializeField] private Color stylusColor = new Color(0.9f, 0.9f, 0.9f);
    
    // Completion Text
    [SerializeField] private TextMeshProUGUI completionTitleText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI sessionDurationText;
    [SerializeField] private TextMeshProUGUI taskTypeText;

    private GameManager gameManager;
    private Camera mainCamera;
    private int score;
    private TaskType selectedTask;
    private DifficultyLevel selectedDifficulty;
    private int selectedDuration = 60; // Default 60s

    protected override void Awake()
    {
        base.Awake();
        
        // Force user-calibrated offsets for Stylus Visualizer
        // This ensures the Inspector values don't override our hardcoded fix
        stylusRotationOffset = new Vector3(60, 0, 0);
        stylusPositionOffset = new Vector3(0, 0.03f, 0.095f);
        
        // Initialize main camera
        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindFirstObjectByType<Camera>();
        
        // Ensure all menus are properly set up for VR
        SetupVRMenu(mainMenu);
        SetupVRMenu(patientLoginMenu);
        SetupVRMenu(selectTaskMenu);
        SetupVRMenu(difficultyMenu);
        SetupVRMenu(startTrialMenu);
        SetupVRMenu(taskCompletionMenu);
        SetupVRMenu(pauseMenu);
        SetupVRMenu(gameplayHUD);
        
        // Ensure Duration Menu exists (create if missing)
        if (durationMenu == null)
        {
            CreateDurationMenu();
        }
        SetupVRMenu(durationMenu);
        
        // Ensure Stylus Visualizer exists
        var visualizer = FindFirstObjectByType<NeuroReachVR.Visuals.StylusVisualizer>();
        if (visualizer == null)
        {
            GameObject visualizerObj = new GameObject("StylusVisualizer");
            visualizer = visualizerObj.AddComponent<NeuroReachVR.Visuals.StylusVisualizer>();
        }
        
        // Apply Settings to Stylus Input Manager (Interaction Logic)
        var stylusInput = FindFirstObjectByType<NeuroReachVR.Input.StylusInputManager>();
        if (stylusInput != null)
        {
            stylusInput.SetCalibration(stylusPositionOffset, stylusRotationOffset);
        }

        // Apply Settings to Visualizer
        if (visualizer != null)
        {
            // CRITICAL: Since StylusInputManager now applies the offset to the "Position" it reports,
            // the Visualizer (which reads that Position) should NOT apply the offset again.
            // So we pass ZERO offsets to the visualizer, but keep the color.
            visualizer.SetConfiguration(Vector3.zero, Vector3.zero, stylusColor);
        }
        
        FixAllCanvases();
        
        if (gameplayHUD == null)
        {
            CreateGameplayHUD();
        }
        
        // Ensure Task Completion Menu exists (create if missing)
        if (taskCompletionMenu == null)
        {
            CreateTaskCompletionMenu();
        }
    }

    private void CreateGameplayHUD()
    {
        Debug.Log("[HUDManager] Creating Gameplay HUD (Exit Button) dynamically...");
        gameplayHUD = new GameObject("GameplayHUD_Auto");
        SetupVRMenu(gameplayHUD);

        // Position it explicitly (will be refined by PositionMenuInFrontOfUser)
        // We want it slightly lower than eye level
        
        // Create a dedicated Quit Button
        GameObject btnObj = new GameObject("QuitButton");
        btnObj.transform.SetParent(gameplayHUD.transform, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // Reddish
        
        quitTaskButton = btnObj.AddComponent<Button>(); // Assign to field
        quitTaskButton.targetGraphic = img;
        
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 50);
        // Position at bottom center of the HUD area
        rect.anchorMin = new Vector2(0.5f, 0.0f);
        rect.anchorMax = new Vector2(0.5f, 0.0f);
        rect.pivot = new Vector2(0.5f, 0.0f);
        rect.anchoredPosition = new Vector2(0, 0); // Bottom
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "STOP TASK";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Add Collider for VR
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(120, 50, 1);

        // --- TIMER TEXT ---
        GameObject timerObj = new GameObject("TaskTimerText");
        timerObj.transform.SetParent(gameplayHUD.transform, false);
        timerText = timerObj.AddComponent<TextMeshProUGUI>(); // Assign to field
        timerText.text = "Time: --";
        timerText.fontSize = 24;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.yellow;
        
        RectTransform timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.sizeDelta = new Vector2(200, 50);
        timerRect.anchorMin = new Vector2(0.5f, 1.0f); // Top
        timerRect.anchorMax = new Vector2(0.5f, 1.0f);
        timerRect.pivot = new Vector2(0.5f, 1.0f);
        timerRect.anchoredPosition = new Vector2(0, -10); // Slightly down from top
        
        RegisterMenu("gameplay", gameplayHUD);
    }

    private void CreateTaskCompletionMenu()
    {
        Debug.Log("[HUDManager] Creating Task Completion Menu dynamically...");
        taskCompletionMenu = new GameObject("TaskCompletionMenu_Auto");
        SetupVRMenu(taskCompletionMenu);
        
        // Background Panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(taskCompletionMenu.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 400);
        
        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        completionTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        completionTitleText.text = "Task Complete!";
        completionTitleText.fontSize = 48;
        completionTitleText.alignment = TextAlignmentOptions.Center;
        completionTitleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Score
        GameObject scoreObj = new GameObject("Score");
        scoreObj.transform.SetParent(panel.transform, false);
        finalScoreText = scoreObj.AddComponent<TextMeshProUGUI>();
        finalScoreText.text = "Score: 0";
        finalScoreText.fontSize = 36;
        finalScoreText.alignment = TextAlignmentOptions.Center;
        finalScoreText.color = Color.yellow;
        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0, 0.6f);
        scoreRect.anchorMax = new Vector2(1, 0.8f);
        scoreRect.offsetMin = Vector2.zero;
        scoreRect.offsetMax = Vector2.zero;
        
        // Buttons Container
        GameObject buttonsObj = new GameObject("Buttons");
        buttonsObj.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup layout = buttonsObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        RectTransform buttonsRect = buttonsObj.GetComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0, 0);
        buttonsRect.anchorMax = new Vector2(1, 0.4f);
        buttonsRect.offsetMin = Vector2.zero;
        buttonsRect.offsetMax = Vector2.zero;

        // Retry Button
        retryTaskButton = CreateCompletionButton(buttonsObj.transform, "RetryButton", "Retry", Color.blue);
        
        // Menu Button
        backToMenuButton = CreateCompletionButton(buttonsObj.transform, "MenuButton", "Main Menu", Color.red);
        
        // Register it
        RegisterMenu("taskCompletion", taskCompletionMenu);
        
        // Ensure listeners are re-bound since we just created buttons
        InitializeButtonListeners();
    }
    
    private void CreateDurationMenu()
    {
        Debug.Log("[HUDManager] Creating Duration Menu dynamically...");
        durationMenu = new GameObject("DurationMenu_Auto");
        
        // Clone from Difficulty Menu if possible for consistency, otherwise create scratch
        if (difficultyMenu != null)
        {
            // Clone only the panel structure if feasible... 
            // Actually, let's just build it to be safe and consistent with code-constructed UI
        }
        
        SetupVRMenu(durationMenu);
        
        // Background Panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(durationMenu.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(500, 400);
        
        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Select Duration";
        titleText.fontSize = 36;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        // Buttons Container
        GameObject buttonsObj = new GameObject("Buttons");
        buttonsObj.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup layout = buttonsObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        RectTransform buttonsRect = buttonsObj.GetComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0, 0.15f);
        buttonsRect.anchorMax = new Vector2(1, 0.8f);
        buttonsRect.offsetMin = Vector2.zero;
        buttonsRect.offsetMax = Vector2.zero;

        // Create Buttons
        duration15Button = CreateCompletionButton(buttonsObj.transform, "Btn15", "15 Seconds", Color.white);
        duration15Button.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f); // Blueish
        
        duration30Button = CreateCompletionButton(buttonsObj.transform, "Btn30", "30 Seconds", Color.white);
        duration30Button.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);

        duration60Button = CreateCompletionButton(buttonsObj.transform, "Btn60", "60 Seconds", Color.white);
        duration60Button.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
        
        // Back Button
        GameObject backBtnObj = new GameObject("BackButton");
        backBtnObj.transform.SetParent(panel.transform, false);
        durationBackButton = backBtnObj.AddComponent<Button>();
        Image backImg = backBtnObj.AddComponent<Image>();
        backImg.color = Color.gray;
        durationBackButton.targetGraphic = backImg;
        RectTransform backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.3f, 0.02f);
        backRect.anchorMax = new Vector2(0.7f, 0.12f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;
        
        GameObject backTextObj = new GameObject("Text");
        backTextObj.transform.SetParent(backBtnObj.transform, false);
        TextMeshProUGUI backText = backTextObj.AddComponent<TextMeshProUGUI>();
        backText.text = "Back";
        backText.fontSize = 20;
        backText.alignment = TextAlignmentOptions.Center;
        backText.color = Color.white;
        RectTransform backTextRect = backTextObj.GetComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        
        // VR Collider for Back Button
        BoxCollider col = backBtnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(backRect.rect.width, backRect.rect.height, 1);
        
        RegisterMenu("duration", durationMenu);
    }
    
    private Button CreateCompletionButton(Transform parent, string name, string text, Color color)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 60);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Add Collider for VR
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(160, 60, 1);
        
        return btn;
    }

    private void FixAllCanvases()
    {
        // Find ALL canvases, including inactive ones
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[HUDManager] Found {allCanvases.Length} canvases in scene (including inactive). Fixing them...");
        
        foreach (var c in allCanvases)
        {
            SetupVRMenu(c.gameObject);
        }
    }

    private void Start()
    {
        gameManager = ServiceLocator.Get<GameManager>();
        
        InitializeButtonListeners();
        
        // Force hide all menus first to ensure clean state
        HideAllMenus(false);

        // Start with Main Menu
        ShowMenu("main");
        
        // Position Main Menu in front of user
        if (mainMenu != null)
        {
            PositionMenuInFrontOfUser(mainMenu);
        }
    }

    /// <summary>
    /// Configures a menu Canvas for VR WorldSpace rendering.
    /// </summary>
    private void SetupVRMenu(GameObject menuObj)
    {
        if (menuObj == null) return;

        Canvas canvas = menuObj.GetComponent<Canvas>();
        if (canvas == null) canvas = menuObj.AddComponent<Canvas>();

        // Force WorldSpace
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;
        
        // Standard VR Scale (approx 1 unit = 1 meter, so UI needs to be small)
        // If the UI is 1920x1080 pixels, a scale of 0.001 makes it 1.92m wide.
        // A scale of 0.0005 makes it ~1m wide, which is good for 1-2m distance.
        if (canvas.transform.localScale.x > 0.1f) // Only rescale if it looks like default UI scale (1.0)
        {
            canvas.transform.localScale = Vector3.one * 0.001f;
        }

        // Add Raycaster if missing
        if (menuObj.GetComponent<GraphicRaycaster>() == null)
        {
            menuObj.AddComponent<GraphicRaycaster>();
        }

        // FIX LEFT EYE ISSUE:
        // 1. Force Layer to UI (5)
        SetLayerRecursively(menuObj, 5); // 5 is the built-in UI layer

        // 2. Force Sprites/Default shader (often safer for WorldSpace VR than UI/Default)
        foreach (var graphic in menuObj.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.material == null || graphic.material.name == "Default UI Material")
            {
                graphic.material = new Material(Shader.Find("Sprites/Default"));
            }
        }
        
        // 3. Ensure Sorting Order is high enough
        canvas.sortingOrder = 10;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child != null) SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    /// <summary>
    /// Positions a menu comfortably in front of the user's head.
    /// </summary>
    public void PositionMenuInFrontOfUser(GameObject menuObj)
    {
        if (menuObj == null || mainCamera == null) return;

        float distance = 1.0f; // Standard distance
        float heightOffset = -0.1f; // Standard height

        // Special case for Gameplay HUD (Exit Button) - put it lower
        if (menuObj == gameplayHUD)
        {
            distance = 0.7f; // Closer
            heightOffset = -0.4f; // Much lower (waist/chest level)
        }

        Vector3 camPos = mainCamera.transform.position;
        
        // Fix: If camera is on floor (e.g. headset sleeping or initializing), force a default height
        if (camPos.y < 0.5f)
        {
            camPos.y = 1.6f; // Assume standing height
        }

        Vector3 targetPos = camPos + (mainCamera.transform.forward * distance);
        targetPos.y = camPos.y + heightOffset;

        // Keep menu upright, but facing the user
        Vector3 directionToUser = camPos - targetPos;
        directionToUser.y = 0; // Flatten rotation so it doesn't tilt up/down
        if (directionToUser.sqrMagnitude < 0.001f) directionToUser = Vector3.back; // Safety
        
        Quaternion targetRot = Quaternion.LookRotation(-directionToUser);

        menuObj.transform.position = targetPos;
        menuObj.transform.rotation = targetRot;
    }

    protected override void InitializeMenus()
    {
        RegisterMenu("main", mainMenu);
        RegisterMenu("patientLogin", patientLoginMenu);
        RegisterMenu("selectTask", selectTaskMenu);
        RegisterMenu("difficulty", difficultyMenu);
        RegisterMenu("startTrial", startTrialMenu);
        RegisterMenu("taskCompletion", taskCompletionMenu);
        RegisterMenu("pause", pauseMenu);
        RegisterMenu("gameplay", gameplayHUD);
        RegisterMenu("duration", durationMenu);
    }

    private void InitializeButtonListeners()
    {
        // Helper to clean and add
        void Bind(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners(); // Remove Inspector/Legacy listeners
                btn.onClick.AddListener(action);
            }
        }

        // Main Menu
        Bind(selectTaskButton, () => ShowMenu("selectTask"));
        Bind(patientLoginButton, () => ShowMenu("patientLogin"));
        Bind(quitButton, QuitGame);

        // Login
        Bind(loginButton, OnLoginClicked);
        Bind(loginBackButton, () => ShowMenu("main"));

        // Task Selection
        Bind(balloonTaskButton, () => SelectTask(TaskType.BalloonPop));
        Bind(pathTaskButton, () => SelectTask(TaskType.PathTracing));
        Bind(spiralTaskButton, () => SelectTask(TaskType.SpiralTracing));
        Bind(taskBackButton, () => ShowMenu("main"));

        // Difficulty
        Bind(easyButton, () => SelectDifficulty(DifficultyLevel.Easy));
        Bind(mediumButton, () => SelectDifficulty(DifficultyLevel.Medium));
        Bind(hardButton, () => SelectDifficulty(DifficultyLevel.Hard));
        Bind(difficultyBackButton, () => ShowMenu("selectTask"));

        // Duration
        Bind(duration15Button, () => SelectDuration(15));
        Bind(duration30Button, () => SelectDuration(30));
        Bind(duration60Button, () => SelectDuration(60));
        Bind(durationBackButton, () => ShowMenu("difficulty"));

        // Start
        Bind(startTrialButton, StartTrial);

        // Completion
        Bind(retryTaskButton, StartTrial);
        Bind(backToMenuButton, () => ShowMenu("main"));

        // Pause
        // Pause
        Bind(resumeButton, ResumeGame);
        Bind(quitTaskButton, QuitTask);
        
        // If we dynamically created the button, we need to make sure the binding works.
        // The CreateGameplayHUD method sets the 'quitTaskButton' field.
        // InitializeButtonListeners is called in Start(), which is AFTER Awake(), so it should be fine.
    }

    // --- Navigation Logic ---

    private void SelectTask(TaskType task)
    {
        selectedTask = task;
        Debug.Log($"[HUDManager] Selected Task: {task}");
        ShowMenu("difficulty");
    }

    private void SelectDifficulty(DifficultyLevel difficulty)
    {
        selectedDifficulty = difficulty;
        Debug.Log($"[HUDManager] Selected Difficulty: {difficulty}");
        
        // Check if task supports duration (Balloon does)
        if (selectedTask == TaskType.BalloonPop)
        {
            ShowMenu("duration");
        }
        else
        {
            // For others, go straight to Start
            UpdateStartMenuState();
            ShowMenu("startTrial");
        }
    }

    private void SelectDuration(int duration)
    {
        selectedDuration = duration;
        Debug.Log($"[HUDManager] Selected Duration: {duration}s");
        
        UpdateStartMenuState();
        ShowMenu("startTrial");
    }

    private void UpdateStartMenuState()
    {
        if (startTrialButton == null) return;
        
        TextMeshProUGUI btnText = startTrialButton.GetComponentInChildren<TextMeshProUGUI>();
        
        // Remove old listeners
        startTrialButton.onClick.RemoveAllListeners();

        // Check for existing Recalibrate button (created dynamically)
        Transform buttonContainer = startTrialButton.transform.parent;
        Transform recalibrateBtnTrans = buttonContainer.Find("RecalibrateButton");
        GameObject recalibrateBtnObj = recalibrateBtnTrans != null ? recalibrateBtnTrans.gameObject : null;

        if (selectedTask == TaskType.SpiralTracing && selectedDifficulty == DifficultyLevel.Easy)
        {
            // Check if already calibrated
            var calibManager = FindFirstObjectByType<NeuroReachVR.Input.TableCalibrationManager>();
            bool isCalibrated = calibManager != null && calibManager.IsCalibrated;
            
            if (isCalibrated)
            {
                if (btnText) btnText.text = "Start Trial";
                startTrialButton.onClick.AddListener(StartTrial);
                
                // Show Recalibrate Button
                if (recalibrateBtnObj == null)
                {
                    recalibrateBtnObj = CreateRecalibrateButton(buttonContainer);
                }
                recalibrateBtnObj.SetActive(true);
            }
            else
            {
                if (btnText) btnText.text = "Calibrate Table";
                startTrialButton.onClick.AddListener(StartCalibrationFlow);
                
                // Hide Recalibrate Button (since main button is Calibrate)
                if (recalibrateBtnObj != null) recalibrateBtnObj.SetActive(false);
            }
        }
        else
        {
            // Standard flow
            if (btnText) btnText.text = "Start Trial";
            startTrialButton.onClick.AddListener(StartTrial);
            
            // Hide Recalibrate Button
            if (recalibrateBtnObj != null) recalibrateBtnObj.SetActive(false);
        }
    }
    
    private GameObject CreateRecalibrateButton(Transform parent)
    {
        // Clone the start button to keep style
        GameObject btnObj = Instantiate(startTrialButton.gameObject, parent);
        btnObj.name = "RecalibrateButton";
        
        // Fix: Offset position to avoid overlap (assuming no LayoutGroup)
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        RectTransform originalRect = startTrialButton.GetComponent<RectTransform>();
        
        if (rect && originalRect)
        {
            // Move down by height + spacing
            float height = originalRect.rect.height;
            if (height == 0) height = 60f; // Default fallback
            
            Vector3 pos = startTrialButton.transform.localPosition;
            pos.y -= (height + 20f); // Move down
            rect.localPosition = pos;
        }
        
        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(StartCalibrationFlow);
        
        TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = "Recalibrate";
        
        // Change color to indicate secondary action
        // Make it distinct but readable
        Image img = btnObj.GetComponent<Image>();
        if (img) img.color = new Color(0.2f, 0.2f, 0.2f); // Dark Grey
        
        return btnObj;
    }

    private void StartCalibrationFlow()
    {
        Debug.Log("[HUDManager] Starting Calibration Flow from Menu...");
        
        var calibManager = FindFirstObjectByType<NeuroReachVR.Input.TableCalibrationManager>();
        if (calibManager == null)
        {
            GameObject cmObj = new GameObject("TableCalibrationManager");
            calibManager = cmObj.AddComponent<NeuroReachVR.Input.TableCalibrationManager>();
        }
        
        // Subscribe to completion
        calibManager.OnCalibrationComplete -= OnMenuCalibrationComplete; // Safety remove
        calibManager.OnCalibrationComplete += OnMenuCalibrationComplete;
        
        // Start Calibration
        calibManager.StartCalibration();
        
        // Update UI feedback
        if (startTrialButton)
        {
            TextMeshProUGUI btnText = startTrialButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText) btnText.text = "Tracing...";
            startTrialButton.interactable = false; // Disable button while calibrating
        }
    }

    private void OnMenuCalibrationComplete()
    {
        Debug.Log("[HUDManager] Menu Calibration Complete!");
        
        // Re-enable button and set to Start
        if (startTrialButton)
        {
            startTrialButton.interactable = true;
            UpdateStartMenuState(); // This will switch it to "Start Trial"
        }
        
        // Unsubscribe
        var calibManager = FindFirstObjectByType<NeuroReachVR.Input.TableCalibrationManager>();
        if (calibManager != null)
        {
            calibManager.OnCalibrationComplete -= OnMenuCalibrationComplete;
        }
    }

    private void StartTrial()
    {
        Debug.Log("[HUDManager] Starting Trial...");
        
        // Use ShowMenu to properly handle transitions/hiding
        ShowMenu("gameplay");

        gameManager.SetDifficulty(selectedDifficulty);
        gameManager.SetTaskDuration(selectedDuration); // Pass duration
        gameManager.StartTask(selectedTask);
    }

    private void OnLoginClicked()
    {
        string id = patientIDInput != null ? patientIDInput.text : "Guest";
        Debug.Log($"[HUDManager] Login: {id}");
        // TODO: Integrate with PatientManager
        
        // After login, go to Task Selection
        ShowMenu("selectTask");
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ResumeGame()
    {
        // Unpause logic
        ShowMenu(null); // Hide pause menu
        if (gameplayHUD) gameplayHUD.SetActive(true);
    }

    private void QuitTask()
    {
        gameManager.EndCurrentTask();
        ShowMenu("main");
    }

    // --- Public API for Game Events ---

    // START FIX: Timer Update Logic
    private TextMeshProUGUI timerText;

    public void UpdateScore(int newScore)
    {
        score = newScore;
        if (scoreText) scoreText.text = $"Score: {score}";
    }

    public void OnTaskCompleted(string taskName, int finalScore, float duration)
    {
        if (completionTitleText) completionTitleText.text = "Task Complete!";
        if (finalScoreText) finalScoreText.text = $"Score: {finalScore}";
        if (sessionDurationText) sessionDurationText.text = $"Time: {duration:F1}s";
        if (taskTypeText) taskTypeText.text = taskName;

        ShowMenu("taskCompletion");
        PositionMenuInFrontOfUser(taskCompletionMenu);
    }

    // Override ShowMenu to handle positioning
    public override void ShowMenu(string menuName, bool animated = true)
    {
        base.ShowMenu(menuName, animated);
        
        // When showing a menu, ensure it's positioned correctly
        if (menus.ContainsKey(menuName))
        {
            GameObject menuObj = menus[menuName];
            if (menuObj != null)
            {
                Debug.Log($"[HUDManager] Positioning menu '{menuName}' (Active: {menuObj.activeSelf})");
                PositionMenuInFrontOfUser(menuObj);
                Debug.Log($"[HUDManager] Menu '{menuName}' position: {menuObj.transform.position}");
            }
            else
            {
                Debug.LogError($"[HUDManager] Menu '{menuName}' is null in dictionary!");
            }
        }
    }

    private void Update()
    {
        // Failsafe: Keep the current active menu in front of the user if it drifts or wasn't positioned
        if (mainCamera != null && !string.IsNullOrEmpty(GetCurrentMenuName()))
        {
            string current = GetCurrentMenuName();
            if (menus.ContainsKey(current))
            {
                GameObject menu = menus[current];
                if (menu != null && menu.activeSelf)
                {
                    // Optional: Smoothly follow? Or just ensure it's visible?
                    // For now, let's just leave it. If it's WorldSpace, it stays where it is.
                }
            }
        }

        // TIMER UPDATE
        if (gameManager != null && gameManager.CurrentTask != null && gameManager.CurrentTask.IsActive)
        {
            if (timerText != null)
            {
                float remaining = gameManager.CurrentTask.RemainingTime;
                timerText.text = $"Time: {remaining:F0}s";
                
                // Optional: Change color if running out?
                if (remaining < 10f) timerText.color = Color.red;
                else timerText.color = Color.yellow;
            }
        }
    }
}
