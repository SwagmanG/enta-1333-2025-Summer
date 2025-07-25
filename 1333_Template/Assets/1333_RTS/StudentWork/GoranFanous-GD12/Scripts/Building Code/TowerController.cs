using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS_1333; // Assuming UnitType and AttackType are defined in this namespace

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
    /// Finds the closest enemy unit within the tower's attack range, prioritizing TowerBreakers.
    /// </summary>
    /// <returns>The nearest enemy UnitController, or null if none in range.</returns>
    private UnitController FindNearestEnemyInRange()
    {
        UnitController[] allUnits = GameObject.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        UnitController nearestTowerBreaker = null;
        UnitController nearestEnemy = null;
        float closestBreakerDistance = float.MaxValue;
        float closestEnemyDistance = float.MaxValue;

        foreach (UnitController unit in allUnits)
        {
            if (unit.armyType != ArmyType.Enemy)
                continue;

            float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);
            if (distanceToUnit > attackRange)
                continue;

            // Check if it's a TowerBreaker
            if (unit.unitType.AttackType == AttackType.TowerBreaker && distanceToUnit < closestBreakerDistance)
            {
                nearestTowerBreaker = unit;
                closestBreakerDistance = distanceToUnit;
            }

            // Track any closest enemy
            if (distanceToUnit < closestEnemyDistance)
            {
                nearestEnemy = unit;
                closestEnemyDistance = distanceToUnit;
            }
        }

        // Prioritize TowerBreaker if one is in range
        return nearestTowerBreaker != null ? nearestTowerBreaker : nearestEnemy;
    }

    /// <summary>
    /// Checks if the specified unit is still a valid attack target (exists, alive, and in range).
    /// </summary>
    private bool IsValidTarget(UnitController unit)
    {
        if (unit == null) return false;

        float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);
        return distanceToUnit <= attackRange && unit.armyType == ArmyType.Enemy;
    }

    /// <summary>
    /// Applies damage to the targeted enemy unit and draws a debug line to show the attack.
    /// </summary>
    private void AttackTarget(UnitController enemy)
    {
        if (enemy == null) return;

        enemy.TakeDamage(damagePerShot);
        Debug.DrawLine(transform.position + Vector3.up * 1f, enemy.transform.position + Vector3.up * 1f, Color.red, 0.2f);
    }

    /// <summary>
    /// Draw Gizmos in the Scene view to visualize tower's attack radius and current target.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 1f, currentTarget.transform.position + Vector3.up * 1f);
        }
    }
}
