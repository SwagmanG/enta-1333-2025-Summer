using System.Collections;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Population")]
    [SerializeField] private int currentPopulation = 0;
    [SerializeField] private int maxPopulation = 50; // Default cap

    public int CurrentPopulation => currentPopulation;
    public int MaxPopulation => maxPopulation;

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
    /// Increases the population (e.g., from castle). Caps at MaxPopulation.
    /// </summary>
    public void AddPopulation(int amount)
    {
        currentPopulation = Mathf.Min(currentPopulation + amount, maxPopulation);
    }

    /// <summary>
    /// Decreases population (e.g., from unit cost).
    /// </summary>
    public void SpendPopulation(int amount)
    {
        currentPopulation = Mathf.Max(0, currentPopulation - amount);
    }

    /// <summary>
    /// Increases max population cap (e.g., from buildings).
    /// </summary>
    public void IncreaseMaxPopulation(int amount)
    {
        maxPopulation += amount;
    }

    /// <summary>
    /// Sets max population to a specific value.
    /// </summary>
    public void SetMaxPopulation(int newMax)
    {
        maxPopulation = Mathf.Max(0, newMax);
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            alignment = TextAnchor.UpperRight,
            normal = { textColor = Color.white }
        };

        float width = 300f;
        float height = 30f;
        Rect rect = new Rect(Screen.width - width - 10, 10, width, height);

        GUI.Label(rect, $"Population: {currentPopulation} / {maxPopulation}", style);
    }
}
