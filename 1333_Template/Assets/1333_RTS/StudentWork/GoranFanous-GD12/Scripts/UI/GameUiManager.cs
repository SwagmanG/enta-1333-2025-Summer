using UnityEngine;
using TMPro;
using UnityEngine.Audio;
using System.Collections;

public class GameUiManager : MonoBehaviour
{
    // Singleton instance - ensures we only have one UI manager throughout the game
    public static GameUiManager Instance { get; private set; }

    // UI elements for the building system - shows when player is in build mode
    [Header("Build UI References")]
    public GameObject BuildPanel;
    public TextMeshProUGUI BuildingNameText;
    public TextMeshProUGUI ControlsText;
    public TextMeshProUGUI CostText;

    // UI elements that display current resource amounts to the player
    [Header("Resource UI References")]
    public TextMeshProUGUI PopulationText;
    public TextMeshProUGUI GoldText;
    public TextMeshProUGUI FoodText;

    // Audio controls - lets players adjust volume settings
    [Header("Audio Sliders Panel")]
    [SerializeField] private GameObject audioSliderPanel; // Parent panel of sliders
    [SerializeField] private AudioMixer audioMixer;

    // Wave countdown UI
    [Header("Wave Countdown UI")]
    [SerializeField] private TextMeshProUGUI waveCountdownText;
    private Coroutine countdownCoroutine;

    // Game state overlays - shown when the game ends
    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverOverlay;
    [SerializeField] private GameObject victoryOverlay;

    private void Awake()
    {
        // Standard singleton pattern - destroy duplicates and keep this instance alive
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeAudioSliders(); // Ensure all sliders connect to the AudioMixer
    }

    // === GAME STATE UI METHODS ===
    // Handle showing game over and victory screens

    private IEnumerator ReturnToMainMenuAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    // Start and update the countdown for the next wave
    public void StartWaveCountdown(int waveIndex)
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(WaveCountdownRoutine(waveIndex));
    }

    // Coroutine to display countdown and trigger wave start
    private IEnumerator WaveCountdownRoutine(int waveIndex)
    {
        // Calculate time based on wave number
        float countdownTime = 60f + (waveIndex * 30f); // 1:00, 1:30, 2:00, etc.

        while (countdownTime > 0f)
        {
            int minutes = Mathf.FloorToInt(countdownTime / 60f);
            int seconds = Mathf.FloorToInt(countdownTime % 60f);

            if (waveCountdownText != null)
                waveCountdownText.text = $"{minutes:00}:{seconds:00}";

            countdownTime -= Time.deltaTime;
            yield return null;
        }

        if (waveCountdownText != null)
            waveCountdownText.text = "Wave starting...";

        yield return new WaitForSeconds(1f); // brief pause before starting

        FindFirstObjectByType<UnitSpawner>()?.ForceStartCurrentWave(); // start wave
    }


    // Show the game over screen and pause the game
    public void TriggerGameOver()
    {
        if (gameOverOverlay != null)
            gameOverOverlay.SetActive(true);

        // Unpause the game (so the coroutine can run), then start delayed return
        Time.timeScale = 1f;
        StartCoroutine(ReturnToMainMenuAfterDelay(3f));
    }

    // Wait a few seconds, then return to main menu scene
   



    // Show the victory screen and pause the game
    public void TriggerVictory()
    {
        if (victoryOverlay != null)
            victoryOverlay.SetActive(true);

        Time.timeScale = 0f;
    }

    // === BUILD MODE UI METHODS ===
    // Handle the UI that appears when players are placing buildings

    // Show the build UI panel and update it with current building info
    public void ShowBuildUI()
    {
        if (BuildPanel != null)
        {
            BuildPanel.SetActive(true);
            UpdateBuildingPreviewUI();
        }
    }

    // === AUDIO SYSTEM SETUP ===
    // Initialize audio sliders to work with our audio mixer

    // Set up all audio sliders to connect to the main audio mixer
    // Uses reflection to inject the mixer reference - bit of a hack but it works
    public void InitializeAudioSliders()
    {
        if (audioSliderPanel == null || audioMixer == null) return;

        AudioSlider[] sliders = audioSliderPanel.GetComponentsInChildren<AudioSlider>(true);
        foreach (AudioSlider slider in sliders)
        {
            // Inject AudioMixer if not already assigned in prefab
            var mixerField = typeof(AudioSlider).GetField("audioMixer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            mixerField?.SetValue(slider, audioMixer);

            // Force Start() logic
            slider.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }
    }

    // Hide the build UI when player exits build mode
    public void HideBuildUI()
    {
        if (BuildPanel != null)
            BuildPanel.SetActive(false);
    }

    // Update the build UI with specific building information
    public void UpdateBuildUI(string buildingName, int populationCost, int goldCost)
    {
        if (BuildPanel != null)
            BuildPanel.SetActive(true);

        if (BuildingNameText != null)
            BuildingNameText.text = $"Placing: {buildingName}";

        if (ControlsText != null)
            ControlsText.text = "Q / E to cycle | R to rotate";

        if (CostText != null)
            CostText.text = $"Population Cost: {populationCost} | Gold Cost: {goldCost}";
    }

    // Update the build UI based on the currently selected building
    // This gets called whenever the player switches between different buildings
    public void UpdateBuildingPreviewUI()
    {
        if (!BuildModeController.BMCInstance.IsInBuildMode)
        {
            HideBuildUI();
            return;
        }

        // Get info about the currently selected building from the building manager
        string buildingName = BuildingManager.BMInstance.GetCurrentBuildingName();
        int populationCost = BuildingManager.BMInstance.GetCurrentPopulationCost();
        int goldCost = BuildingManager.BMInstance.GetCurrentGoldCost();

        if (buildingName == "None")
        {
            HideBuildUI();
        }
        else
        {
            UpdateBuildUI(buildingName, populationCost, goldCost);
        }
    }

    // === RESOURCE UI METHODS ===
    // Keep the resource display updated with current amounts

    // Update all resource displays with current and maximum values
    public void UpdateResourceUI(int population, int maxPop, int gold, int maxGold, int food, int maxFood)
    {
        if (PopulationText != null)
            PopulationText.text = $"Population: {population} / {maxPop}";

        if (GoldText != null)
            GoldText.text = $"Gold: {gold} / {maxGold}";

        if (FoodText != null)
            FoodText.text = $"Food: {food} / {maxFood}";
    }
}