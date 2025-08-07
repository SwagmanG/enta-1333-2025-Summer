using System.Collections;
using UnityEngine;

public class CastleResourceManager : MonoBehaviour
{
    // -------------------- Population Settings --------------------
    [Header("Population Generation Settings")]
    [SerializeField] private float generationIntervalSeconds = 5f; // Time between each resource generation cycle
    [SerializeField] private int populationPerInterval = 5;        // How much population is generated each cycle
    [SerializeField] private int basePopulationCap = 50;           // Starting cap for total population
    [SerializeField] private float populationGrowthMultiplier = 1f; // Used to scale population growth dynamically

    // -------------------- Gold Settings --------------------
    [Header("Gold Generation Settings")]
    [SerializeField] private int goldPerInterval = 10;             // How much gold is generated each cycle
    [SerializeField] private float goldGrowthMultiplier = 1f;      // Multiplier to scale gold generation

    // -------------------- Food Settings --------------------
    [Header("Food Production Settings")]
    [SerializeField] private int foodPerPopulationGain = 5;        // How much food is produced per population gained

    // -------------------- General Modifiers --------------------
    [Header("Resource Modifiers")]
    [SerializeField] private float populationModifier = 1f;        // Scales population gain
    [SerializeField] private float foodModifier = 1f;              // Scales food gain
    [SerializeField] private float goldModifier = 1f;              // Scales gold gain

    private Coroutine resourceGenerationCoroutine;                 // Holds a reference to the running coroutine
    private bool isActive = false;                                 // Flag to make sure generation starts only once

    // -------------------- Entry Point to Start Resource Generation --------------------
    public void ActivateResourceGeneration()
    {
        if (isActive) return; // Don't start again if already running
        isActive = true;
        resourceGenerationCoroutine = StartCoroutine(ResourceGenerationLoop());
    }

    // -------------------- Core Resource Generation Logic --------------------
    private IEnumerator ResourceGenerationLoop()
    {
        while (true)
        {
            // Wait between each generation tick
            yield return new WaitForSeconds(generationIntervalSeconds);

            // Ensure the ResourceManager exists before proceeding
            if (ResourceManager.RMInstance == null) continue;

            // Fetch current and max population from ResourceManager
            int currentPopulation = ResourceManager.RMInstance.CurrentPopulation;
            int maxPopulation = ResourceManager.RMInstance.MaxPopulation;

            // Only add population if there's room
            if (currentPopulation < maxPopulation)
            {
                // Calculate raw population gain
                int basePopulationGain = Mathf.FloorToInt(populationPerInterval * populationGrowthMultiplier);

                // Limit gain to available space
                int availableSpace = maxPopulation - currentPopulation;
                int finalPopulationGain = Mathf.Min(basePopulationGain, availableSpace);

                // Apply population modifier
                int modifiedPopulationGain = Mathf.FloorToInt(finalPopulationGain * populationModifier);

                // Add population to the ResourceManager
                ResourceManager.RMInstance.AddPopulation(modifiedPopulationGain);

                // Calculate and add food based on population gain
                int modifiedFoodGain = Mathf.FloorToInt(foodPerPopulationGain * foodModifier);
                ResourceManager.RMInstance.AddFood(modifiedFoodGain);

                // Log the result
                Debug.Log($"Castle generated {modifiedPopulationGain} population and {modifiedFoodGain} food.");
            }

            // Always generate gold regardless of population status
            int baseGoldGain = Mathf.FloorToInt(goldPerInterval * goldGrowthMultiplier);
            int modifiedGoldGain = Mathf.FloorToInt(baseGoldGain * goldModifier);

            ResourceManager.RMInstance.AddGold(modifiedGoldGain);

            Debug.Log($"Castle generated {modifiedGoldGain} gold.");
        }
    }

    // -------------------- Modifier Getters & Setters --------------------
    // These allow other scripts to modify or read the current generation multipliers/modifiers.

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
