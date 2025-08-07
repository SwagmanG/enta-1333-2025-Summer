using UnityEngine;
using TMPro;
using UnityEngine.Audio;

public class GameUiManager : MonoBehaviour
{
    public static GameUiManager Instance { get; private set; }

    [Header("Build UI References")]
    public GameObject buildPanel;
    public TextMeshProUGUI buildingNameText;
    public TextMeshProUGUI controlsText;
    public TextMeshProUGUI costText;

    [Header("Resource UI References")]
    public TextMeshProUGUI populationText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI foodText;

    [Header("Audio Sliders Panel")]
    [SerializeField] private GameObject audioSliderPanel; // Parent panel of sliders
    [SerializeField] private AudioMixer audioMixer;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverOverlay;
    [SerializeField] private GameObject victoryOverlay;

    private void Awake()
    {
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
    
    public void TriggerGameOver()
    {
        if (gameOverOverlay != null)
            gameOverOverlay.SetActive(true);

        Time.timeScale = 0f;
    }

    public void TriggerVictory()
    {
        if (victoryOverlay != null)
            victoryOverlay.SetActive(true);

        Time.timeScale = 0f;
    }

    public void ShowBuildUI()
    {
        if (buildPanel != null)
        {
            buildPanel.SetActive(true);
            UpdateBuildingPreviewUI(); 
        }
    }


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


    public void HideBuildUI()
    {
        if (buildPanel != null)
            buildPanel.SetActive(false);
    }

    public void UpdateBuildUI(string buildingName, int populationCost, int goldCost)
    {
        if (buildPanel != null)
            buildPanel.SetActive(true);

        if (buildingNameText != null)
            buildingNameText.text = $"Placing: {buildingName}";

        if (controlsText != null)
            controlsText.text = "Q / E to cycle | R to rotate";

        if (costText != null)
            costText.text = $"Population Cost: {populationCost} | Gold Cost: {goldCost}";
    }

    public void UpdateBuildingPreviewUI()
    {
        if (!BuildModeController.BMCInstance.IsInBuildMode)
        {
            HideBuildUI();
            return;
        }

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

    public void UpdateResourceUI(int population, int maxPop, int gold, int maxGold, int food, int maxFood)
    {
        if (populationText != null)
            populationText.text = $"Population: {population} / {maxPop}";

        if (goldText != null)
            goldText.text = $"Gold: {gold} / {maxGold}";

        if (foodText != null)
            foodText.text = $"Food: {food} / {maxFood}";
    }
}
