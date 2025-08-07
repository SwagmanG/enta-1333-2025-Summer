using UnityEngine;
using RTS_1333;


public class BuildingHealth : MonoBehaviour
{

    private int currentHitPoints;

    // Instantiating Variables
    public ArmyType OwnerArmyType;
    public BuildingSettings BuildingConfig;
    public int CurrentHitPoints;

    private void Start()
    {
        if (BuildingConfig == null)
        {
            Debug.LogError($"BuildingSettings not assigned on {gameObject.name}");
            return;
        }

        currentHitPoints = BuildingConfig.MaxHealth;
    }

    /// <summary>
    /// Applies damage to the building and checks for destruction.
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply.</param>
    public void ApplyDamage(int damageAmount)
    {
        currentHitPoints -= damageAmount;
        Debug.Log($"{BuildingConfig.BuildingName} took {damageAmount} damage. Current HP: {currentHitPoints}");

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
        Debug.Log($"{BuildingConfig.BuildingName} destroyed!");

        // Notify BuildingManager to free grid nodes
        BuildingManager.BMInstance?.OnBuildingDestroyed(this);

        // Destroy the building game object
        Destroy(gameObject);
    }
}
