using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS_1333;

/// <summary>
/// Controls tower behavior - automatically finds and attacks enemy units within range
/// </summary>
public class TowerController : MonoBehaviour
{
    [Header("Tower Configuration")]
    // How far the tower can see and attack enemies
    [SerializeField] private float attackRadius = 5f;                 // Attack radius (Node Units) 
    // How long the tower waits between shots
    [SerializeField] private float attackCooldown = 1f;               // Attack Cooldown
    // How much damage each arrow does to enemies
    [SerializeField] private int damagePerAttack = 10;                // Amount of damage dealt per attack

    [Header("Arrow Settings")]
    // The arrow object that gets shot at enemies
    [SerializeField] private GameObject arrowPrefab;                   // Arrow prefab to instantiate
    // How fast arrows fly through the air
    [SerializeField] private float arrowSpeed = 10f;                   // Speed at which arrow flies

    // Reference to the grid system (needed because towers are spawned as prefabs)
    private GridManager gridManagerInstance;                          // Reference to the GridManager instance to access the grid
    // The enemy we're currently trying to shoot
    private UnitController currentAttackTarget;                       // The currently targeted enemy unit
    // Tracks how much time has passed since we last shot an arrow
    private float attackCooldownTimer = 0f;                           // Timer to track time passed since last attack

    /// <summary>
    /// Find the grid manager when the tower is created
    /// </summary>
    private void Awake()
    {
        // We need to find the grid manager since towers are instantiated from prefabs
        if (gridManagerInstance == null)
        {
            gridManagerInstance = FindFirstObjectByType<GridManager>();
        }
    }

    /// <summary>
    /// Main tower logic - find targets and shoot at them
    /// </summary>
    private void Update()
    {
        // Keep track of how long it's been since we last attacked
        attackCooldownTimer += Time.deltaTime;

        // Check if our current target is still valid (alive, in range, etc.)
        if (currentAttackTarget == null || !IsTargetValid(currentAttackTarget))
        {
            // Need to find a new target
            currentAttackTarget = FindClosestEnemyWithinRange();
        }

        // If we have a target and enough time has passed, shoot at it
        if (currentAttackTarget != null && attackCooldownTimer >= attackCooldown)
        {
            ShootArrowAt(currentAttackTarget);
            attackCooldownTimer = 0f; // Reset the timer after shooting
        }
    }

    /// <summary>
    /// Look for the best enemy to attack within our range
    /// TowerBreakers get priority since they're specifically designed to destroy towers
    /// </summary>
    private UnitController FindClosestEnemyWithinRange()
    {
        // Get all units in the scene
        UnitController[] allUnits = GameObject.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
        UnitController nearestTowerBreaker = null;
        UnitController nearestEnemyUnit = null;
        float nearestBreakerDistance = float.MaxValue;
        float nearestEnemyDistance = float.MaxValue;

        // Check each unit to see if it's a valid target
        foreach (UnitController unit in allUnits)
        {
            // Skip units that aren't enemies
            if (unit.armyType != ArmyType.Enemy)
                continue;

            // Skip units that are too far away
            float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);
            if (distanceToUnit > attackRadius)
                continue;

            // TowerBreakers are high priority - they're specifically designed to attack towers
            if (unit.unitType.AttackType == AttackType.TowerBreaker && distanceToUnit < nearestBreakerDistance)
            {
                nearestTowerBreaker = unit;
                nearestBreakerDistance = distanceToUnit;
            }

            // Also keep track of the closest enemy in case there are no TowerBreakers
            if (distanceToUnit < nearestEnemyDistance)
            {
                nearestEnemyUnit = unit;
                nearestEnemyDistance = distanceToUnit;
            }
        }

        // Always target TowerBreakers first if they're in range, otherwise go for closest enemy
        return nearestTowerBreaker != null ? nearestTowerBreaker : nearestEnemyUnit;
    }

    /// <summary>
    /// Check if our current target is still worth shooting at
    /// </summary>
    private bool IsTargetValid(UnitController unit)
    {
        // No unit means no valid target
        if (unit == null) return false;

        // Make sure the unit is still close enough and is still an enemy
        float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);
        return distanceToUnit <= attackRadius && unit.armyType == ArmyType.Enemy;
    }

    /// <summary>
    /// Create and fire an arrow at the target enemy
    /// </summary>
    private void ShootArrowAt(UnitController enemyUnit)
    {
        // Can't shoot without a target or arrow prefab
        if (enemyUnit == null || arrowPrefab == null) return;

        // Create the arrow slightly above the tower so it looks like it's being shot from the top
        Vector3 spawnPos = transform.position + Vector3.up * 1f; // adjust Y offset as needed
        GameObject arrowInstance = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        // Start moving the arrow toward the target
        StartCoroutine(MoveArrowToTarget(arrowInstance, enemyUnit));
    }

    /// <summary>
    /// Handle arrow movement and collision with the target
    /// </summary>
    private IEnumerator MoveArrowToTarget(GameObject arrow, UnitController target)
    {
        // Safety check - make sure we have valid objects
        if (arrow == null || target == null)
            yield break;

        // Keep moving the arrow until it hits the target or something goes wrong
        while (arrow != null && target != null)
        {
            // Aim for the center of the target (a bit above ground level)
            Vector3 targetPos = target.transform.position + Vector3.up * 1f;
            Vector3 direction = (targetPos - arrow.transform.position).normalized;

            // Make the arrow point toward where it's flying
            arrow.transform.LookAt(targetPos);


            // Move the arrow forward at the specified speed
            arrow.transform.position += direction * arrowSpeed * Time.deltaTime;

            // Check if we're close enough to count as a hit
            if (Vector3.Distance(arrow.transform.position, targetPos) < 0.2f)
            {
                // Hit! Deal damage and clean up the arrow
                target.TakeDamage(damagePerAttack);
                Destroy(arrow);
                yield break;
            }

            // Wait for the next frame before continuing
            yield return null;
        }

        // Clean up the arrow if the target died or disappeared before we hit it
        if (arrow != null)
            Destroy(arrow);
    }

    /// <summary>
    /// Draw visual helpers in the scene view for debugging
    /// Shows the attack radius and current target line
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw a yellow circle showing how far the tower can attack
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // Draw a red line to the current target if we have one
        if (currentAttackTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 1f, currentAttackTarget.transform.position + Vector3.up * 1f);
        }
    }
}