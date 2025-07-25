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
    [SerializeField] private float attackRadius = 5f;                 // Attack radius in world units
    [SerializeField] private float attackCooldownSeconds = 1f;        // Time delay between consecutive attacks in seconds
    [SerializeField] private int damagePerAttack = 10;                // Amount of damage dealt per attack

    private GridManager gridManagerInstance;                          // Reference to the GridManager instance to access the grid
    private UnitController currentAttackTarget;                       // The currently targeted enemy unit
    private float attackCooldownTimer = 0f;                           // Timer to track time elapsed since last attack

    private void Awake()
    {
        if (gridManagerInstance == null)
        {
            gridManagerInstance = FindFirstObjectByType<GridManager>();
            if (gridManagerInstance == null)
            {
                Debug.LogError("TowerController: GridManager not found in scene!");
            }
        }
    }

    private void Update()
    {
        // Update the cooldown timer by the time elapsed since last frame
        attackCooldownTimer += Time.deltaTime;

        // If no valid target or target is out of range or destroyed, find a new one
        if (currentAttackTarget == null || !IsTargetValid(currentAttackTarget))
        {
            currentAttackTarget = FindClosestEnemyWithinRange();
        }

        // If there is a target and the attack cooldown has elapsed, attack
        if (currentAttackTarget != null && attackCooldownTimer >= attackCooldownSeconds)
        {
            Attack(currentAttackTarget);
            attackCooldownTimer = 0f; // Reset cooldown timer after attacking
        }
    }

    /// <summary>
    /// Finds the closest enemy unit within the tower's attack radius, prioritizing TowerBreakers.
    /// </summary>
    /// <returns>The nearest enemy UnitController, or null if none in range.</returns>
    private UnitController FindClosestEnemyWithinRange()
    {
        UnitController[] allUnits = GameObject.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        UnitController nearestTowerBreaker = null;
        UnitController nearestEnemyUnit = null;
        float nearestBreakerDistance = float.MaxValue;
        float nearestEnemyDistance = float.MaxValue;

        foreach (UnitController unit in allUnits)
        {
            if (unit.armyType != ArmyType.Enemy)
                continue;

            float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);
            if (distanceToUnit > attackRadius)
                continue;

            // Check if it's a TowerBreaker and closer than current closest TowerBreaker
            if (unit.unitType.AttackType == AttackType.TowerBreaker && distanceToUnit < nearestBreakerDistance)
            {
                nearestTowerBreaker = unit;
                nearestBreakerDistance = distanceToUnit;
            }

            // Track any closest enemy unit
            if (distanceToUnit < nearestEnemyDistance)
            {
                nearestEnemyUnit = unit;
                nearestEnemyDistance = distanceToUnit;
            }
        }

        // Prioritize TowerBreaker if one is in range
        return nearestTowerBreaker != null ? nearestTowerBreaker : nearestEnemyUnit;
    }

    /// <summary>
    /// Checks if the specified unit is still a valid attack target (exists, alive, and in range).
    /// </summary>
    private bool IsTargetValid(UnitController unit)
    {
        if (unit == null) return false;

        float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);
        return distanceToUnit <= attackRadius && unit.armyType == ArmyType.Enemy;
    }

    /// <summary>
    /// Applies damage to the targeted enemy unit and draws a debug line to show the attack.
    /// </summary>
    private void Attack(UnitController enemyUnit)
    {
        if (enemyUnit == null) return;

        enemyUnit.TakeDamage(damagePerAttack);
        Debug.DrawLine(transform.position + Vector3.up * 1f, enemyUnit.transform.position + Vector3.up * 1f, Color.red, 0.2f);
    }

    /// <summary>
    /// Draw Gizmos in the Scene view to visualize tower's attack radius and current target.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        if (currentAttackTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 1f, currentAttackTarget.transform.position + Vector3.up * 1f);
        }
    }
}
