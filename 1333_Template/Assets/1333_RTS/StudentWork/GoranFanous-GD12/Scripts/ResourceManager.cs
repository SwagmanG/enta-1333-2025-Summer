using System.Collections;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    // -------------------- Singleton Pattern --------------------
    public static ResourceManager RMInstance { get; private set; }

    // -------------------- External References --------------------
    [Header("References")]
    public GameUiManager uiManager; // Used to update the resource UI every frame

    // -------------------- Population Settings --------------------
    [Header("Population Settings")]
    [SerializeField] private int currentPopulationCount = 0;   // How much population is currently being used
    [SerializeField] private int maximumPopulationCap = 50;    // Starting maximum population allowed

    // -------------------- Gold Settings --------------------
    [Header("Gold Settings")]
    [SerializeField] private int currentGoldAmount = 0;        // Current gold the player has
    [SerializeField] private int maximumGoldCap = 9999;        // Maximum gold limit

    // -------------------- Food Settings --------------------
    [Header("Food Settings")]
    [SerializeField] private int currentFoodAmount = 0;        // Current food the player has
    [SerializeField] private int maximumFoodCap = 9999;        // Maximum food limit

    // -------------------- Property Getters --------------------
    // These are used by other systems to read current values safely
    public int CurrentPopulation => Mathf.Max(0, currentPopulationCount);
    public int MaxPopulation => maximumPopulationCap;
    public int CurrentGold => Mathf.Max(0, currentGoldAmount);
    public int MaxGold => maximumGoldCap;
    public int CurrentFood => Mathf.Max(0, currentFoodAmount);
    public int MaxFood => maximumFoodCap;

    // -------------------- Singleton Setup --------------------
    private void Awake()
    {
        // Ensure only one ResourceManager exists
        if (RMInstance != null && RMInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        RMInstance = this;
    }

    // -------------------- UI Syncing --------------------
    private void Update()
    {
        // Continuously push the latest values to the UI every frame
        uiManager.UpdateResourceUI(
            CurrentPopulation, MaxPopulation,
            CurrentGold, MaxGold,
            CurrentFood, MaxFood
        );
    }

    // -------------------- Population Management --------------------
    public void AddPopulation(int amountToAdd)
    {
        // Adds population up to the max cap
        currentPopulationCount = Mathf.Clamp(currentPopulationCount + amountToAdd, 0, maximumPopulationCap);
    }

    public void SpendPopulation(int amountToSpend)
    {
        // Reduces population, never goes below zero
        currentPopulationCount = Mathf.Max(0, currentPopulationCount - amountToSpend);
    }

    public void IncreaseMaxPopulation(int increaseAmount)
    {
        // Increase max population cap (e.g., from upgrades or buildings)
        maximumPopulationCap += increaseAmount;
    }

    public void SetMaxPopulation(int newMaxCap)
    {
        // Directly set the population cap
        maximumPopulationCap = Mathf.Max(0, newMaxCap);
    }

    // -------------------- Gold Management --------------------
    public void AddGold(int amountToAdd)
    {
        // Adds gold, clamped to the maximum limit
        currentGoldAmount = Mathf.Clamp(currentGoldAmount + amountToAdd, 0, maximumGoldCap);
    }

    public bool SpendGold(int amountToSpend)
    {
        // Try to spend gold; returns false if not enough
        if (currentGoldAmount >= amountToSpend)
        {
            currentGoldAmount -= amountToSpend;
            return true;
        }

        return false;
    }

    public void IncreaseMaxGold(int increaseAmount)
    {
        // Increase the gold cap (e.g., from upgrades)
        maximumGoldCap += increaseAmount;
    }

    public void SetMaxGold(int newMaxCap)
    {
        // Directly set the gold cap
        maximumGoldCap = Mathf.Max(0, newMaxCap);
    }

    // -------------------- Food Management --------------------
    public void AddFood(int amountToAdd)
    {
        // Adds food, clamped to the maximum limit
        currentFoodAmount = Mathf.Clamp(currentFoodAmount + amountToAdd, 0, maximumFoodCap);
    }

    public void SpendFood(int amountToSpend)
    {
        // Reduce food, never below zero
        currentFoodAmount = Mathf.Max(0, currentFoodAmount - amountToSpend);
    }

    public void IncreaseMaxFood(int increaseAmount)
    {
        // Increase the food cap (e.g., through upgrades)
        maximumFoodCap += increaseAmount;
    }

    public void SetMaxFood(int newMaxCap)
    {
        // Directly set the food cap
        maximumFoodCap = Mathf.Max(0, newMaxCap);
    }
}
