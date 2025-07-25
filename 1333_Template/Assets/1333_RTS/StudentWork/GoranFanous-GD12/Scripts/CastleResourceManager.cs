using System.Collections;
using UnityEngine;

/// <summary>
/// Handles passive population generation from the castle over time.
/// </summary>
public class CastleResourceManager : MonoBehaviour
{
    [Header("Population Generation Settings")]
    [SerializeField] private float generationIntervalSeconds = 3f; // Time between each population tick
    [SerializeField] private int populationPerInterval = 5;        // Population added per tick
    [SerializeField] private int basePopulationCap = 50;           // Base maximum population provided by the castle
    [SerializeField] private float populationGrowthMultiplier = 1f; // Population growth multiplier (modifiable later)

    private Coroutine populationGenerationCoroutine;

    private void Start()
    {
        populationGenerationCoroutine = StartCoroutine(PopulationGenerationLoop());
    }

    private IEnumerator PopulationGenerationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(generationIntervalSeconds);

            if (ResourceManager.Instance == null)
            {
                Debug.LogWarning("CastleResourceManager: Missing ResourceManager instance.");
                continue;
            }

            int currentPopulation = ResourceManager.Instance.CurrentPopulation;
            int maxPopulation = ResourceManager.Instance.MaxPopulation;

            if (currentPopulation < maxPopulation)
            {
                int adjustedPopulation = Mathf.FloorToInt(populationPerInterval * populationGrowthMultiplier);
                int availableSpace = maxPopulation - currentPopulation;
                int populationToAdd = Mathf.Min(adjustedPopulation, availableSpace);

                ResourceManager.Instance.AddPopulation(populationToAdd);
                Debug.Log($"Castle generated {populationToAdd} population (x{populationGrowthMultiplier} multiplier).");
            }
        }
    }

    public float GetPopulationGrowthMultiplier() => populationGrowthMultiplier;

    public void SetPopulationGrowthMultiplier(float multiplier)
    {
        populationGrowthMultiplier = multiplier;
    }

    public int GetBasePopulationCap() => basePopulationCap;
}
