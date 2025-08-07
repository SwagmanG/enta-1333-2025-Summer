using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject audioPanel;
    

    

    private bool isGamePaused = false;

    void Start()
    {
        // Hide menus at start
        pauseMenuUI.SetActive(false);
        audioPanel.SetActive(false);
   
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }


    public void TogglePause()
    {
        if (isGamePaused)
        {
            ResumeGame();
            pauseButton.SetActive(true);
        }
        else
        {
            PauseGame();
            pauseButton.SetActive(false);
        }
    }

    private void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        audioPanel.SetActive(true);
        Time.timeScale = 0f;
        isGamePaused = true;
    }

    private void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        audioPanel.SetActive(false);
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
       
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        
        Application.Quit();


    }
}
