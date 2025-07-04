using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS_1333;

public class UnitController : MonoBehaviour
{
    // The army faction this unit belongs to (Player or Enemy)
    public ArmyType armyType;

    // The type object defining stats like health, damage, range, etc.
    public UnitType unitType;

    // Current health value of this unit
    private int currentHealth;

    // Coroutine reference to the movement routine following a path
    private Coroutine movementCoroutine;

    // Index of the next node in the current path to move toward
    private int currentPathIndex;

    // List of grid nodes representing the current path to follow
    private List<GridNode> currentPath;

    // Reference to the GridManager for grid queries and operations
    private GridManager gridManager;

    // The grid node currently occupied by this unit at path end
    private GridNode finalOccupiedNode;

    // Stores original Walkable state of finalOccupiedNode before occupation
    private bool finalOccupiedNodeOriginalWalkable;

    // The target world position the unit is moving toward or pathfinding to
    private Vector3 targetWorldPosition;

    // Flag indicating whether the unit is currently trying to find an alternate path near goal
    private bool isAttemptingRepath = false;

    // Current building or structure targeted by this unit for attack
    private BuildingHealth currentTarget;

    // Time interval between attacks
    [SerializeField] private float attackCooldown = 1f;

    // Timer tracking time since last attack
    private float attackTimer = 0f;

    private void Awake()
    {
        // Find and cache the GridManager in the scene
        gridManager = FindFirstObjectByType<GridManager>();

        // Initialize health from UnitType, or default to 10 if null
        currentHealth = unitType != null ? unitType.MaxHp : 10;

        attackCooldown = 1f;
    }

    private void Start()
    {
        // Start enemy-specific behavior to seek and attack player structures
        if (armyType == ArmyType.Enemy)
            StartCoroutine(SeekAndPathToStructureLoop());
    }

    private void Update()
    {
        // Increment attack timer with delta time
        attackTimer += Time.deltaTime;

        // If there is a current target, check distance and attack if possible
        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            float attackRange = unitType != null ? unitType.Range : 1.5f;

            if (distanceToTarget <= attackRange && attackTimer >= attackCooldown)
            {
                int damageAmount = unitType != null ? unitType.Damage : 1;

                currentTarget.TakeDamage(damageAmount);
                attackTimer = 0f;

                // Stop movement while attacking
                if (movementCoroutine != null)
                    StopCoroutine(movementCoroutine);
            }
        }
    }

    /// <summary>
    /// Coroutine for enemy units to find and path toward nearest player towers or castles.
    /// Runs continuously to update target and path.
    /// </summary>
    private IEnumerator SeekAndPathToStructureLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f);

            if (currentTarget == null || currentTarget.Equals(null))
            {
                BuildingHealth[] allStructures = Object.FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None);
                BuildingHealth closestStructure = null;
                float closestDistance = float.MaxValue;

                // Find closest player structure of type tower or castle
                foreach (var structure in allStructures)
                {
                    if (structure == null || structure.ArmyType != ArmyType.Player)
                        continue;

                    string buildingNameLower = structure.BuildingSettings.BuildingName.ToLower();

                    if (!buildingNameLower.Contains("tower") && !buildingNameLower.Contains("castle"))
                        continue;

                    float distanceToStructure = Vector3.Distance(transform.position, structure.transform.position);

                    if (distanceToStructure < closestDistance)
                    {
                        closestDistance = distanceToStructure;
                        closestStructure = structure;
                    }
                }

                if (closestStructure != null)
                {
                    currentTarget = closestStructure;
                    RequestPath(currentTarget.transform.position);
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Apply damage to this unit and destroy if health falls below zero.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            // Restore walkability on final node when unit dies
            if (finalOccupiedNode != null)
            {
                finalOccupiedNode.Walkable = finalOccupiedNodeOriginalWalkable;
                gridManager.MarkUnoccupied(finalOccupiedNode, this);
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Requests a pathfinding operation to the specified world position.
    /// </summary>
    public void RequestPath(Vector3 worldDestinationPosition)
    {
        targetWorldPosition = worldDestinationPosition;
        AstarPathfinding.Instance?.RequestPathfinding(transform.position, worldDestinationPosition, this);
    }

    /// <summary>
    /// Starts following the provided path of nodes.
    /// </summary>
    public void FollowPath(List<GridNode> pathToFollow)
    {
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        currentPath = pathToFollow;
        currentPathIndex = 0;

        // Restore walkability on previous final node before occupying new path
        if (finalOccupiedNode != null)
        {
            finalOccupiedNode.Walkable = finalOccupiedNodeOriginalWalkable;
            gridManager.MarkUnoccupied(finalOccupiedNode, this);
        }

        isAttemptingRepath = false;
        movementCoroutine = StartCoroutine(FollowPathCoroutine());
    }

    /// <summary>
    /// Coroutine that moves the unit along the path node by node.
    /// Waits if nodes are occupied, tries to repath near goal node if stuck.
    /// </summary>
    private IEnumerator FollowPathCoroutine()
    {
        while (currentPathIndex < currentPath.Count)
        {
            GridNode nextNode = currentPath[currentPathIndex];

            // Target position is node world position plus a small vertical offset
            Vector3 worldTargetPosition = nextNode.WorldPosition + Vector3.up * 0.5f;

            float waitDuration = 0f;

            // Wait while the next node is occupied by another unit, unless we're already on it
            while (gridManager.IsOccupied(nextNode) && !IsOnSameNode(nextNode))
            {
                waitDuration += 0.1f;

                // If waited too long at final path node, attempt to repath once around goal node
                if (waitDuration > 2f && currentPathIndex == currentPath.Count - 1 && !isAttemptingRepath)
                {
                    isAttemptingRepath = true;
                    yield return StartCoroutine(TryRepathAroundGoalNode());
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // Move the unit toward the target position until close enough
            while (Vector3.Distance(transform.position, worldTargetPosition) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    worldTargetPosition,
                    (unitType != null ? unitType.MoveSpeed : 3f) * Time.deltaTime
                );

                yield return null;
            }

            currentPathIndex++;
            yield return null;
        }

        // If player army, mark final node occupied and unwalkable
        if (armyType == ArmyType.Player)
        {
            GridNode finalNodeUnderUnit = GetNodeUnderUnit();
            if (finalNodeUnderUnit != null)
            {
                finalOccupiedNodeOriginalWalkable = finalNodeUnderUnit.Walkable;
                finalNodeUnderUnit.Walkable = false;
                gridManager.MarkOccupied(finalNodeUnderUnit, this);
                finalOccupiedNode = finalNodeUnderUnit;
            }
        }
    }

    /// <summary>
    /// Attempts to find an alternate path around the goal node if it's blocked.
    /// </summary>
    private IEnumerator TryRepathAroundGoalNode()
    {
        Vector2Int goalGridCoordinates = gridManager.GetGridPosFromWorld(targetWorldPosition);

        GridNode goalNode = gridManager.GetNode(goalGridCoordinates.x, goalGridCoordinates.y);

        if (goalNode == null)
            yield break;

        // Get neighbors that are walkable for this unit
        List<GridNode> neighborNodes = gridManager.GetWalkableNeighbors(goalNode, this);

        // Sort neighbor nodes by distance from this unit's current position
        neighborNodes.Sort((nodeA, nodeB) =>
            Vector3.Distance(transform.position, nodeA.WorldPosition)
            .CompareTo(Vector3.Distance(transform.position, nodeB.WorldPosition))
        );

        // Request path to first unoccupied neighbor node
        foreach (var alternateNode in neighborNodes)
        {
            if (!gridManager.IsOccupied(alternateNode))
            {
                RequestPath(alternateNode.WorldPosition);
                yield break;
            }
        }

        // If no alternate node found, wait and retry original goal
        yield return new WaitForSeconds(0.5f);
        RequestPath(targetWorldPosition);
    }

    /// <summary>
    /// Returns the grid node the unit is currently standing on.
    /// </summary>
    private GridNode GetNodeUnderUnit()
    {
        Vector2Int currentGridCoordinates = gridManager.GetGridPosFromWorld(transform.position);
        return gridManager.GetNode(currentGridCoordinates.x, currentGridCoordinates.y);
    }

    /// <summary>
    /// Checks if the given node is the same node the unit currently occupies.
    /// </summary>
    private bool IsOnSameNode(GridNode node)
    {
        return node == GetNodeUnderUnit();
    }
}
