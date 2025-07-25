using System.Collections;
using UnityEngine;

public class CastleResourceManager : MonoBehaviour
{
    [Header("Population Generation")]
    [SerializeField] private float populationInterval = 3f; // Time between population increases
    [SerializeField] private int populationPerTick = 5;     // Amount generated per interval
    [SerializeField] private int baseMaxPopulation = 50;    // Max population cap from castle
    [SerializeField] private float populationModifier = 1f; // Modifier (can be increased later)

    private Coroutine generationCoroutine;

    private void Start()
    {
        generationCoroutine = StartCoroutine(GeneratePopulationRoutine());
    }

    private IEnumerator GeneratePopulationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(populationInterval);

            if (ResourceManager.Instance == null)
            {
                Debug.LogWarning("CastleResourceManager: ResourceManager instance not found.");
                continue;
            }

            int currentPop = ResourceManager.Instance.CurrentPopulation;
            int currentMax = ResourceManager.Instance.MaxPopulation;

            if (currentPop < currentMax)
            {
                int adjustedAmount = Mathf.FloorToInt(populationPerTick * populationModifier);
                int spaceLeft = currentMax - currentPop;
                int finalAmount = Mathf.Min(adjustedAmount, spaceLeft);

                ResourceManager.Instance.AddPopulation(finalAmount);
                Debug.Log($"Castle generated {finalAmount} population (modifier: {populationModifier}x)");
            }
        }
    }

    public float GetPopulationModifier() => populationModifier;

    public void SetPopulationModifier(float modifier)
    {
        populationModifier = modifier;
    }

    public int GetBaseMaxPopulation() => baseMaxPopulation;
}
