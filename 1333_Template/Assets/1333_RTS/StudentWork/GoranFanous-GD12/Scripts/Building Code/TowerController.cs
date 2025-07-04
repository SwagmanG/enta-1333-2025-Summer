using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls tower behavior, including scanning for nearby enemy units within a radius,
/// targeting one at a time, and damaging them over time. Only towers can kill enemies.
/// </summary>
public class TowerController : MonoBehaviour
{
    [Header("Tower Configuration")]
    [SerializeField] private float attackRange = 5f;                // Attack radius in world units
    [SerializeField] private float attackInterval = 1f;             // Time delay between consecutive attacks in seconds
    [SerializeField] private int damagePerShot = 10;                // Amount of damage dealt per attack

    private GridManager gridManager;                                // Reference to the GridManager instance to access the grid
    private UnitController currentTarget;                           // The currently targeted enemy unit
    private float attackTimer = 0f;                                 // Timer to track time elapsed since last attack

    private void Awake()
    {
        // Attempt to find the GridManager instance in the scene
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("TowerController: GridManager not found in scene!");
            }
        }
    }

    private void Update()
    {
        // Increment the attack timer by the time elapsed since last frame
        attackTimer += Time.deltaTime;

        // If no valid target or target is out of range or destroyed, find a new one
        if (currentTarget == null || !IsValidTarget(currentTarget))
        {
            currentTarget = FindNearestEnemyInRange();
        }

        // If there is a target and the attack cooldown has passed, attack
        if (currentTarget != null && attackTimer >= attackInterval)
        {
            AttackTarget(currentTarget);
            attackTimer = 0f; // Reset attack timer after attacking
        }
    }

    /// <summary>
    /// Finds the closest enemy unit within the tower's attack range.
    /// </summary>
    /// <returns>The nearest enemy UnitController, or null if none in range.</returns>
    private UnitController FindNearestEnemyInRange()
    {
        UnitController[] allUnits = GameObject.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        UnitController nearestEnemy = null;
        float closestDistance = float.MaxValue;

        // Iterate through all units and find the closest enemy within attack range
        foreach (UnitController unit in allUnits)
        {
            // Skip if not an enemy unit
            if (unit.armyType != ArmyType.Enemy)
                continue;

            // Calculate distance from this tower to the unit
            float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);

            // Check if this unit is closer than the previously recorded closest and inside attack range
            if (distanceToUnit <= attackRange && distanceToUnit < closestDistance)
            {
                nearestEnemy = unit;
                closestDistance = distanceToUnit;
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// Checks if the specified unit is still a valid attack target (exists, alive, and in range).
    /// </summary>
    /// <param name="unit">The unit to validate.</param>
    /// <returns>True if valid target; otherwise false.</returns>
    private bool IsValidTarget(UnitController unit)
    {
        if (unit == null) return false;

        float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);

        // Valid if within attack range and is an enemy unit
        return distanceToUnit <= attackRange && unit.armyType == ArmyType.Enemy;
    }

    /// <summary>
    /// Applies damage to the targeted enemy unit and draws a debug line to show the attack.
    /// </summary>
    /// <param name="enemy">The enemy unit to attack.</param>
    private void AttackTarget(UnitController enemy)
    {
        if (enemy == null) return;

        // Inflict damage on the enemy
        enemy.TakeDamage(damagePerShot);

        // Draw a red line in the Scene view for visualizing the attack
        Debug.DrawLine(transform.position + Vector3.up * 1f, enemy.transform.position + Vector3.up * 1f, Color.red, 0.2f);
    }

    /// <summary>
    /// Draw Gizmos in the Scene view to visualize tower's attack radius and current target.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw yellow wire sphere representing the attack radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw a red line to the current target if one exists
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 1f, currentTarget.transform.position + Vector3.up * 1f);
        }
    }
}
