using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS_1333;

public class TowerController : MonoBehaviour
{
    [Header("Tower Configuration")]
    [SerializeField] private float attackRadius = 5f;                 // Attack radius (Node Units) 
    [SerializeField] private float attackCooldown = 1f;               // Attack Cooldown
    [SerializeField] private int damagePerAttack = 10;                // Amount of damage dealt per attack

    [Header("Arrow Settings")]
    [SerializeField] private GameObject arrowPrefab;                   // Arrow prefab to instantiate
    [SerializeField] private float arrowSpeed = 10f;                   // Speed at which arrow flies

    private GridManager gridManagerInstance;                          // Reference to the GridManager instance to access the grid
    private UnitController currentAttackTarget;                       // The currently targeted enemy unit
    private float attackCooldownTimer = 0f;                           // Timer to track time passed since last attack

    private void Awake()
    {
        // Referencing the gridmanager via FindObject because the tower is a prefab.
        if (gridManagerInstance == null)
        {
            gridManagerInstance = FindFirstObjectByType<GridManager>();
        }
    }

    private void Update()
    {
        // Update the cooldown timer by the time passed since last frame
        attackCooldownTimer += Time.deltaTime;

        // If no valid target or target is out of range or destroyed, find a new one
        if (currentAttackTarget == null || !IsTargetValid(currentAttackTarget))
        {
            currentAttackTarget = FindClosestEnemyWithinRange();
        }

        // If there is a target and the attack cooldown has passed then attack
        if (currentAttackTarget != null && attackCooldownTimer >= attackCooldown)
        {
            ShootArrowAt(currentAttackTarget);
            attackCooldownTimer = 0f; // Reset cooldown timer after attacking
        }
    }

    // Finds the closest enemy unit in range.
    // Prioritizes TowerBreakers.
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

    // Checks if the specified unit is still a valid attack target (exists, alive, and in range.
    private bool IsTargetValid(UnitController unit)
    {
        if (unit == null) return false;

        float distanceToUnit = Vector3.Distance(transform.position, unit.transform.position);
        return distanceToUnit <= attackRadius && unit.armyType == ArmyType.Enemy;
    }

    // Shoots an arrow towards the enemy unit
    private void ShootArrowAt(UnitController enemyUnit)
    {
        if (enemyUnit == null || arrowPrefab == null) return;

        // Instantiate arrow at tower's position (with a bit of vertical offset if needed)
        Vector3 spawnPos = transform.position + Vector3.up * 1f; // adjust Y offset as needed
        GameObject arrowInstance = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        // Start the arrow movement coroutine
        StartCoroutine(MoveArrowToTarget(arrowInstance, enemyUnit));
    }

    private IEnumerator MoveArrowToTarget(GameObject arrow, UnitController target)
    {
        if (arrow == null || target == null)
            yield break;

        while (arrow != null && target != null)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1f; // aim for center height of target
            Vector3 direction = (targetPos - arrow.transform.position).normalized;

            // Rotate arrow to face the target position
            arrow.transform.LookAt(targetPos);
            

            // Move arrow toward target
            arrow.transform.position += direction * arrowSpeed * Time.deltaTime;

            // Check distance to target
            if (Vector3.Distance(arrow.transform.position, targetPos) < 0.2f)
            {
                // Deal damage and destroy arrow
                target.TakeDamage(damagePerAttack);
                Destroy(arrow);
                yield break;
            }

            yield return null;
        }


        // If arrow or target gets destroyed before impact, clean up arrow if needed
        if (arrow != null)
            Destroy(arrow);
    }

    // Draw Gizmos in the Scene view to visualize tower's attack radius and current target.
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
