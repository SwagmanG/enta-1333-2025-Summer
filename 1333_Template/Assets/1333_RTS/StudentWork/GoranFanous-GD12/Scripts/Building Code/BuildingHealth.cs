using UnityEngine;
using RTS_1333;

/// <summary>
/// Manages health and damage of a building.
/// Does NOT handle grid node occupation.
/// </summary>
public class BuildingHealth : MonoBehaviour
{
    [SerializeField] private BuildingSettings buildingConfig;   // Settings containing building size and health info
    [SerializeField] private ArmyType ownerArmyType;            // The army this building belongs to

    private int currentHitPoints;

    // Public getters for external access
    public ArmyType OwnerArmyType => ownerArmyType;
    public BuildingSettings BuildingConfig => buildingConfig;
    public int CurrentHitPoints => currentHitPoints;

    private void Start()
    {
        if (buildingConfig == null)
        {
            Debug.LogError($"BuildingSettings not assigned on {gameObject.name}");
            return;
        }

        currentHitPoints = buildingConfig.MaxHealth;
    }

    /// <summary>
    /// Applies damage to the building and checks for destruction.
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply.</param>
    public void ApplyDamage(int damageAmount)
    {
        currentHitPoints -= damageAmount;
        Debug.Log($"{buildingConfig.BuildingName} took {damageAmount} damage. Current HP: {currentHitPoints}");

        if (currentHitPoints <= 0)
        {
            HandleDestruction();
        }
    }

    /// <summary>
    /// Called when building is destroyed.
    /// Notifies BuildingManager to free grid nodes, then destroys this object.
    /// </summary>
    private void HandleDestruction()
    {
        Debug.Log($"{buildingConfig.BuildingName} destroyed!");

        // Notify BuildingManager to free grid nodes
        BuildingManager.Instance?.OnBuildingDestroyed(this);

        // Destroy the building game object
        Destroy(gameObject);
    }
}
