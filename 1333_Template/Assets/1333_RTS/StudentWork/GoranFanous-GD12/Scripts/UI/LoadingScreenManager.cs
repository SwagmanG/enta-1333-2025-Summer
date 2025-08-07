using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour
{
    // Singleton instance - ensures we have one loading manager that persists across scenes
    public static LoadingScreenManager LoadInstance;

    // UI elements for the loading screen display
    public GameObject LoadingScreenObject;  // The main loading screen panel
    public Slider ProgressBar;              // Visual progress indicator for the player

    private void Awake()
    {
        // Standard singleton pattern with scene persistence
        // This ensures our loading screen survives scene transitions
        if (LoadInstance != null && LoadInstance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            LoadInstance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // === PUBLIC SCENE SWITCHING METHOD ===
    // This is what other scripts call to switch scenes with a loading screen

    // Start the scene switching process with loading screen display
    public void SwitchToScene(int SceneID)
    {
        LoadingScreenObject.SetActive(true);  // Show the loading screen
        ProgressBar.value = 0;                // Reset progress bar to start
        StartCoroutine(SwitchSceneAsync(SceneID));  // Begin async loading
    }

    // === ASYNCHRONOUS SCENE LOADING ===
    // Handle the actual scene loading in the background while showing progress

    // Load the new scene asynchronously and update the progress bar
    IEnumerator SwitchSceneAsync(int SceneID)
    {
        // Start loading the scene in the background
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneID);

        // Keep updating the progress bar until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            // Update progress bar with loading progress (0.0 to 1.0)
            ProgressBar.value = asyncLoad.progress;
            yield return null;  // Wait one frame before checking again
        }

        // Give a brief pause so players can see the loading completed
        // This prevents the loading screen from flashing by too quickly
        yield return new WaitForSeconds(0.2f);

        // Hide the loading screen now that we're done
        LoadingScreenObject.SetActive(false);
    }
}