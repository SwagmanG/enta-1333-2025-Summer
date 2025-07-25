using System.Collections;
using UnityEngine;

/// <summary>
/// Manages global population stats including current and maximum population.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Population Settings")]
    [SerializeField] private int currentPopulationCount = 0;
    [SerializeField] private int maximumPopulationCap = 50; // Default starting cap

    public int CurrentPopulation => currentPopulationCount;
    public int MaxPopulation => maximumPopulationCap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Adds population up to the max cap (e.g., from passive income like castles).
    /// </summary>
    public void AddPopulation(int amountToAdd)
    {
        currentPopulationCount = Mathf.Min(currentPopulationCount + amountToAdd, maximumPopulationCap);
    }

    /// <summary>
    /// Spends population (e.g., when units are created).
    /// </summary>
    public void SpendPopulation(int amountToSpend)
    {
        currentPopulationCount = Mathf.Max(0, currentPopulationCount - amountToSpend);
    }

    /// <summary>
    /// Increases the max population cap (e.g., from building upgrades).
    /// </summary>
    public void IncreaseMaxPopulation(int increaseAmount)
    {
        maximumPopulationCap += increaseAmount;
    }

    /// <summary>
    /// Directly sets the max population cap.
    /// </summary>
    public void SetMaxPopulation(int newMaxCap)
    {
        maximumPopulationCap = Mathf.Max(0, newMaxCap);
    }

    private void OnGUI()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.UpperRight,
            normal = { textColor = Color.white }
        };

        float labelWidth = 300f;
        float labelHeight = 30f;
        Rect labelRect = new Rect(Screen.width - labelWidth - 10, 10, labelWidth, labelHeight);

        GUI.Label(labelRect, $"Population: {currentPopulationCount} / {maximumPopulationCap}", labelStyle);
    }
}
