using System.Collections;
using UnityEngine;

public class CastleResourceManager : MonoBehaviour
{
    [Header("Population Generation Settings")]
    [SerializeField] private float generationIntervalSeconds = 5f;
    [SerializeField] private int populationPerInterval = 5;
    [SerializeField] private int basePopulationCap = 50;
    [SerializeField] private float populationGrowthMultiplier = 1f;

    [Header("Gold Generation Settings")]
    [SerializeField] private int goldPerInterval = 10;
    [SerializeField] private float goldGrowthMultiplier = 1f;

    [Header("Food Production Settings")]
    [SerializeField] private int foodPerPopulationGain = 5;

    [Header("Resource Modifiers")]
    [SerializeField] private float populationModifier = 1f;
    [SerializeField] private float foodModifier = 1f;
    [SerializeField] private float goldModifier = 1f;

    private Coroutine resourceGenerationCoroutine;
    private bool isActive = false;

    public void ActivateResourceGeneration()
    {
        if (isActive) return;
        isActive = true;
        resourceGenerationCoroutine = StartCoroutine(ResourceGenerationLoop());
    }

    private IEnumerator ResourceGenerationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(generationIntervalSeconds);

            if (ResourceManager.RMInstance == null) continue;

            int currentPopulation = ResourceManager.RMInstance.CurrentPopulation;
            int maxPopulation = ResourceManager.RMInstance.MaxPopulation;

            if (currentPopulation < maxPopulation)
            {
                int basePopulationGain = Mathf.FloorToInt(populationPerInterval * populationGrowthMultiplier);
                int availableSpace = maxPopulation - currentPopulation;
                int finalPopulationGain = Mathf.Min(basePopulationGain, availableSpace);
                int modifiedPopulationGain = Mathf.FloorToInt(finalPopulationGain * populationModifier);

                ResourceManager.RMInstance.AddPopulation(modifiedPopulationGain);

                int modifiedFoodGain = Mathf.FloorToInt(foodPerPopulationGain * foodModifier);
                ResourceManager.RMInstance.AddFood(modifiedFoodGain);

                Debug.Log($"Castle generated {modifiedPopulationGain} population and {modifiedFoodGain} food.");
            }

            int baseGoldGain = Mathf.FloorToInt(goldPerInterval * goldGrowthMultiplier);
            int modifiedGoldGain = Mathf.FloorToInt(baseGoldGain * goldModifier);

            ResourceManager.RMInstance.AddGold(modifiedGoldGain);

            Debug.Log($"Castle generated {modifiedGoldGain} gold.");
        }
    }

    // Modifier accessors
    public float GetPopulationGrowthMultiplier() => populationGrowthMultiplier;
    public void SetPopulationGrowthMultiplier(float multiplier) => populationGrowthMultiplier = multiplier;

    public float GetGoldGrowthMultiplier() => goldGrowthMultiplier;
    public void SetGoldGrowthMultiplier(float multiplier) => goldGrowthMultiplier = multiplier;

    public int GetBasePopulationCap() => basePopulationCap;

    public float GetPopulationModifier() => populationModifier;
    public void SetPopulationModifier(float modifier) => populationModifier = modifier;
    public void AddToPopulationModifier(float delta) => populationModifier += delta;

    public float GetFoodModifier() => foodModifier;
    public void SetFoodModifier(float modifier) => foodModifier = modifier;
    public void AddToFoodModifier(float delta) => foodModifier += delta;

    public float GetGoldModifier() => goldModifier;
    public void SetGoldModifier(float modifier) => goldModifier = modifier;
    public void AddToGoldModifier(float delta) => goldModifier += delta;
}
