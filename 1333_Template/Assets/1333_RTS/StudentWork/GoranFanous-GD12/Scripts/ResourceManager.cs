using System.Collections;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager RMInstance { get; private set; }

    [Header("References")]
    public GameUiManager uiManager;

    [Header("Population Settings")]
    [SerializeField] private int currentPopulationCount = 0;
    [SerializeField] private int maximumPopulationCap = 50;

    [Header("Gold Settings")]
    [SerializeField] private int currentGoldAmount = 0;
    [SerializeField] private int maximumGoldCap = 9999;

    [Header("Food Settings")]
    [SerializeField] private int currentFoodAmount = 0;
    [SerializeField] private int maximumFoodCap = 9999;

    public int CurrentPopulation => Mathf.Max(0, currentPopulationCount);
    public int MaxPopulation => maximumPopulationCap;
    public int CurrentGold => Mathf.Max(0, currentGoldAmount);
    public int MaxGold => maximumGoldCap;
    public int CurrentFood => Mathf.Max(0, currentFoodAmount);
    public int MaxFood => maximumFoodCap;

    private void Awake()
    {
        if (RMInstance != null && RMInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        RMInstance = this;
    }

    private void Update()
    {
        uiManager.UpdateResourceUI(
            CurrentPopulation, MaxPopulation,
            CurrentGold, MaxGold,
            CurrentFood, MaxFood
        );
    }


    // Population
    public void AddPopulation(int amountToAdd)
    {
        currentPopulationCount = Mathf.Clamp(currentPopulationCount + amountToAdd, 0, maximumPopulationCap);
    }

    public void SpendPopulation(int amountToSpend)
    {
        currentPopulationCount = Mathf.Max(0, currentPopulationCount - amountToSpend);
    }

    public void IncreaseMaxPopulation(int increaseAmount)
    {
        maximumPopulationCap += increaseAmount;
    }

    public void SetMaxPopulation(int newMaxCap)
    {
        maximumPopulationCap = Mathf.Max(0, newMaxCap);
    }

    // Gold
    public void AddGold(int amountToAdd)
    {
        currentGoldAmount = Mathf.Clamp(currentGoldAmount + amountToAdd, 0, maximumGoldCap);
    }

    public bool SpendGold(int amountToSpend)
    {
        if (currentGoldAmount >= amountToSpend)
        {
            currentGoldAmount -= amountToSpend;
            return true;
        }

        return false;
    }

    public void IncreaseMaxGold(int increaseAmount)
    {
        maximumGoldCap += increaseAmount;
    }

    public void SetMaxGold(int newMaxCap)
    {
        maximumGoldCap = Mathf.Max(0, newMaxCap);
    }

    // Food
    public void AddFood(int amountToAdd)
    {
        currentFoodAmount = Mathf.Clamp(currentFoodAmount + amountToAdd, 0, maximumFoodCap);
    }

    public void SpendFood(int amountToSpend)
    {
        currentFoodAmount = Mathf.Max(0, currentFoodAmount - amountToSpend);
    }

    public void IncreaseMaxFood(int increaseAmount)
    {
        maximumFoodCap += increaseAmount;
    }

    public void SetMaxFood(int newMaxCap)
    {
        maximumFoodCap = Mathf.Max(0, newMaxCap);
    }

    
}
