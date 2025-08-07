using UnityEngine;
using RTS_1333;
using System.Collections;

public class BuildingHealth : MonoBehaviour
{
    private int currentHitPoints;
    // Instantiating Variables
    public ArmyType OwnerArmyType;
    public BuildingSettings BuildingConfig;
    public int CurrentHitPoints;

    // Damage flash effect
    private Renderer buildingRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private void Start()
    {
        if (BuildingConfig == null)
        {
            Debug.LogError($"BuildingSettings not assigned on {gameObject.name}");
            return;
        }
        currentHitPoints = BuildingConfig.MaxHealth;

        // Set up damage flash effect
        buildingRenderer = GetComponent<Renderer>();
        if (buildingRenderer != null && buildingRenderer.material != null)
        {
            originalColor = buildingRenderer.material.color;
        }
    }

    /// <summary>
    /// Applies damage to the building and checks for destruction.
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply.</param>
    public void ApplyDamage(int damageAmount)
    {
        currentHitPoints -= damageAmount;
        Debug.Log($"{BuildingConfig.BuildingName} took {damageAmount} damage. Current HP: {currentHitPoints}");

        // Start damage flash effect
        if (buildingRenderer != null && flashCoroutine == null)
        {
            flashCoroutine = StartCoroutine(FlashDamageEffect());
        }

        if (currentHitPoints <= 0)
        {
            HandleDestruction();
        }
    }

    // Flash red when taking damage
    private IEnumerator FlashDamageEffect()
    {
        if (buildingRenderer == null || buildingRenderer.material == null)
        {
            flashCoroutine = null;
            yield break;
        }

        // Flash red briefly
        buildingRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        // Return to original color
        buildingRenderer.material.color = originalColor;
        yield return new WaitForSeconds(0.05f);

        flashCoroutine = null;
    }

    /// <summary>
    /// Called when building is destroyed.
    /// Notifies BuildingManager to free grid nodes, then destroys this object.
    /// </summary>
    private void HandleDestruction()
    {
        Debug.Log($"{BuildingConfig.BuildingName} destroyed!");
        // Notify BuildingManager to free grid nodes
        BuildingManager.BMInstance?.OnBuildingDestroyed(this);
        // Destroy the building game object
        Destroy(gameObject);
    }
}