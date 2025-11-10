
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenu;
    public Button SelectTaskButton;
    public Button PatientLoginButton;
    public Button QuitButton;

    [Header("Patient Login Menu")]
    public GameObject patientLoginMenu; // Instead we could make a list selector
    public Button Log2CSV;              //to choose for between given patients
    public TMP_InputField patientIDInput;
    public Button log2MainMenuButton;


    [Header("Select Task Menu")]
    public GameObject selectTaskMenu;
    public Button SpiralTaskButton;
    public Button LineTaskButton;
    public Button BackToMainMenuButton;

    [Header("Difficulty Settings")]
    public GameObject difficultyButtons;
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("Start Trial Menu")]
    public GameObject startTrialMenu;
    public Button startTrialButton;

    [Header("Score Display")]
    public TextMeshProUGUI scoreText;

    

    private int score;

    void Start()
    {
        // Initialize the score
        score = 0;
        UpdateScoreText();

        ShowMainMenu();


        // Add listeners for the buttons
        
        if (mainMenu != null)
        {
            SelectTaskButton.onClick.AddListener(ShowSelectTaskMenu);
            PatientLoginButton.onClick.AddListener(ShowPatientLoginMenu);
            QuitButton.onClick.AddListener(Application.Quit);
        }

        if (patientLoginMenu != null)
        {
            Log2CSV.onClick.AddListener(() =>
            {
                string patientID = patientIDInput.text;
                Debug.Log("Logging data for Patient ID: " + patientID);
                // Add logic to log data to CSV for the given patient ID
            });
            log2MainMenuButton.onClick.AddListener(ShowMainMenu);
        }


        if (selectTaskMenu != null)
        {
            SpiralTaskButton.onClick.AddListener(ShowDifficultyMenu);
            LineTaskButton.onClick.AddListener(ShowDifficultyMenu);
            BackToMainMenuButton.onClick.AddListener(ShowMainMenu);
        }

        if (difficultyButtons != null)
        {
            easyButton.onClick.AddListener(() => SetDifficulty("Easy"));
            mediumButton.onClick.AddListener(() => SetDifficulty("Medium"));
            hardButton.onClick.AddListener(() => SetDifficulty("Hard"));
        }

        if (startTrialButton != null)
        {
            startTrialButton.onClick.AddListener(ShowDifficultyMenu);
        }
    }


    public void ShowMainMenu()
    {
        // Hide all other menus and show the main menu
        if (patientLoginMenu != null)
        {
            patientLoginMenu.SetActive(false);
        }
        if (selectTaskMenu != null)
        {
            selectTaskMenu.SetActive(false);
        }
        if (startTrialMenu != null)
        {
            startTrialMenu.SetActive(false);
        }
        if (mainMenu != null)
        {
            mainMenu.SetActive(true);
        }
    }

    public void ShowPatientLoginMenu()
    {
        // Hide main menu and show patient login menu
        if (mainMenu != null)
        {
            mainMenu.SetActive(false);
        }
        if (patientLoginMenu != null)
        {
            patientLoginMenu.SetActive(true);
        }
    }

    public void ShowSelectTaskMenu()
    {
        // Hide patient login menu and show select task menu
        if (patientLoginMenu != null)
        {
            patientLoginMenu.SetActive(false);
        }
        if (selectTaskMenu != null)
        {
            selectTaskMenu.SetActive(true);
        }
    }

    public void ShowDifficultyMenu()
    {
        // Hide the select task menu and show the difficulty menu
        if (selectTaskMenu != null)
        {
            selectTaskMenu.SetActive(false);
        }
        if (difficultyButtons != null)
        {
            difficultyButtons.SetActive(true);
        }
    }

    public void ShowStartTrialMenu()
    {
        // Hide the difficulty menu and show the start trial menu
        if (difficultyButtons != null)
        {
            difficultyButtons.SetActive(false);
        }
        if (startTrialMenu != null)
        {
            startTrialMenu.SetActive(true);
        }
    }

    public void SetDifficulty(string difficulty)
    {
        Debug.Log("Difficulty set to: " + difficulty);
        // Add logic for game difficulty 

        if (difficultyButtons != null)
        {
            difficultyButtons.SetActive(false);
        }
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}
