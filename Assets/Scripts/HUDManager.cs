
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Difficulty Settings")]
    public GameObject difficultyButtons;
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("Score Display")]
    public TextMeshProUGUI scoreText;

    private int score;

    void Start()
    {
        // Initialize the score
        score = 0;
        UpdateScoreText();

        // Add listeners to the buttons
        easyButton.onClick.AddListener(() => SetDifficulty("Easy"));
        mediumButton.onClick.AddListener(() => SetDifficulty("Medium"));
        hardButton.onClick.AddListener(() => SetDifficulty("Hard"));
    }

    public void SetDifficulty(string difficulty)
    {
        Debug.Log("Difficulty set to: " + difficulty);
        // Add your logic here to change the game's difficulty

        // Deactivate the difficulty buttons
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
