using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    // UI panel references - these get assigned in the inspector
    [Header("UI Panels")]
    [SerializeField] private GameObject PauseMenuUI;  // The main pause menu overlay
    [SerializeField] private GameObject PauseButton;  // The pause button that shows during gameplay
    [SerializeField] private GameObject AudioPanel;   // Audio settings panel within pause menu

    // Track whether the game is currently paused
    private bool isGamePaused = false;

    void Start()
    {
        // Make sure all menus are hidden when the game starts
        // This ensures we begin with a clean gameplay state
        PauseMenuUI.SetActive(false);
        AudioPanel.SetActive(false);
    }

    void Update()
    {
        // Listen for the escape key to toggle pause menu
        // This is a common expectation for PC players
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    // === PAUSE/RESUME FUNCTIONALITY ===
    // Handle switching between paused and unpaused game states

    // Toggle between paused and resumed states
    public void TogglePause()
    {
        if (isGamePaused)
        {
            ResumeGame();
            PauseButton.SetActive(true);   // Show pause button during gameplay
        }
        else
        {
            PauseGame();
            PauseButton.SetActive(false);  // Hide pause button when menu is open
        }
    }

    // Pause the game and show all pause menu elements
    private void PauseGame()
    {
        PauseMenuUI.SetActive(true);   // Show the pause menu overlay
        AudioPanel.SetActive(true);    // Show audio controls
        Time.timeScale = 0f;           // Freeze game time
        isGamePaused = true;
    }

    // Resume the game and hide all pause menu elements
    private void ResumeGame()
    {
        PauseMenuUI.SetActive(false);  // Hide the pause menu overlay
        AudioPanel.SetActive(false);   // Hide audio controls
        Time.timeScale = 1f;           // Restore normal game time
        isGamePaused = false;
    }

    // === MENU NAVIGATION METHODS ===
    // Handle player choices from the pause menu

    // Return to the main menu (assumed to be scene index 0)
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;  // Make sure time is restored before switching scenes
        SceneManager.LoadScene(0);
    }

    // Quit the entire application
    // Note: This only works in builds, not in the Unity editor
    public void QuitGame()
    {
        Application.Quit();
    }
}