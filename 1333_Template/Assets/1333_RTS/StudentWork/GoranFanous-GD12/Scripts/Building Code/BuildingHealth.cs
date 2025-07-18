using UnityEngine;
using RTS_1333;

/// <summary>
/// Manages health and damage of a building.
/// Does NOT handle grid node occupation.
/// </summary>
public class BuildingHealth : MonoBehaviour
{
    [SerializeField] private BuildingSettings buildingSettings;   // Settings containing building size and health info
    [SerializeField] private ArmyType armyType;                   // The army this building belongs to

    private int currentHealth;

    // Public getters for external access
    public ArmyType ArmyType => armyType;
    public BuildingSettings BuildingSettings => buildingSettings;
    public int CurrentHealth => currentHealth;

    private void Start()
    {
        if (buildingSettings == null)
        {
            Debug.LogError($"BuildingSettings not assigned on {gameObject.name}");
            return;
        }

        currentHealth = buildingSettings.MaxHealth;
    }

    /// <summary>
    /// Apply damage to the building and check for destruction.
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply.</param>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"{buildingSettings.BuildingName} took {damageAmount} damage. Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }

    /// <summary>
    /// Called when building is destroyed.
    /// Notifies BuildingManager to free grid nodes, then destroys itself.
    /// </summary>
    private void OnDestroyed()
    {
        Debug.Log($"{buildingSettings.BuildingName} destroyed!");

        // Notify BuildingManager to free grid nodes
        BuildingManager.Instance?.OnBuildingDestroyed(this);

        // Destroy game object
        Destroy(gameObject);
    }
}
