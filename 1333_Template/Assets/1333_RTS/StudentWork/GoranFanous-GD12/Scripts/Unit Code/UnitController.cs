using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS_1333;

public class UnitController : MonoBehaviour
{
    public ArmyType armyType;
    public UnitType unitType;

    private int currentHealth;
    private Coroutine movementCoroutine;
    private int currentPathIndex;
    private List<GridNode> currentPath;
    private GridManager gridManager;
    private GridNode finalOccupiedNode;
    private bool finalOccupiedNodeOriginalWalkable;
    private Vector3 targetWorldPosition;
    private bool isAttemptingRepath = false;

    private BuildingHealth currentTarget;
    private float attackCooldown;
    private float attackTimer = 0f;

    private void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        currentHealth = unitType != null ? unitType.MaxHp : 10;
        attackCooldown = unitType != null ? 1f / unitType.AttackSpeed : 1f;
    }

    private void Start()
    {
        if (armyType == ArmyType.Enemy)
            StartCoroutine(SeekAndPathToStructureLoop());
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            float attackRange = unitType != null ? unitType.Range : 1.5f;

            if (distanceToTarget <= attackRange && attackTimer >= attackCooldown)
            {
                int damageAmount = unitType != null ? unitType.Damage : 1;
                currentTarget.TakeDamage(damageAmount);
                attackTimer = 0f;

                if (movementCoroutine != null)
                    StopCoroutine(movementCoroutine);
            }
        }
    }

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

                foreach (var structure in allStructures)
                {
                    if (structure == null || structure.ArmyType != ArmyType.Player)
                        continue;

                    string nameLower = structure.BuildingSettings.BuildingName.ToLowerInvariant();

                    if (unitType.AttackType == AttackType.TowerBreaker && !nameLower.Contains("tower"))
                        continue;

                    if (unitType.AttackType == AttackType.CastleBreaker)
                    {
                        if (!(nameLower.Contains("castle") || nameLower.Contains("keep") || nameLower.Contains("fortress")))
                            continue;
                    }

                    if (unitType.AttackType == AttackType.FoodBreaker &&
                        !(nameLower.Contains("farm") || nameLower.Contains("mill") || nameLower.Contains("granary")))
                        continue;

                    float distance = Vector3.Distance(transform.position, structure.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestStructure = structure;
                    }
                }

                if (closestStructure != null)
                {
                    currentTarget = closestStructure;

                    // Request path to a reachable node adjacent to the building
                    Vector3 pathTargetPos = GetClosestReachableNodePositionAroundBuilding(closestStructure);
                    RequestPath(pathTargetPos);
                }
            }

            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Finds the closest walkable node adjacent to the building footprint, within attack range.
    /// </summary>
    /// <param name="building">Target building.</param>
    /// <returns>World position of the node to pathfind to.</returns>
    private Vector3 GetClosestReachableNodePositionAroundBuilding(BuildingHealth building)
    {
        Vector2Int baseGridPos = BuildingManager.Instance.gridManager.GetGridPosFromWorld(building.transform.position);
        int width = building.BuildingSettings.BuildingSizeX;
        int height = building.BuildingSettings.BuildingSizeY;
        float nodeSize = BuildingManager.Instance.gridManager.GridSettings.NodeSize;

        List<GridNode> candidateNodes = new();

        // Check all nodes around building footprint perimeter (including diagonals)
        for (int x = baseGridPos.x - 1; x <= baseGridPos.x + width; x++)
        {
            for (int y = baseGridPos.y - 1; y <= baseGridPos.y + height; y++)
            {
                bool isOnPerimeter =
                    (x == baseGridPos.x - 1 || x == baseGridPos.x + width) ||
                    (y == baseGridPos.y - 1 || y == baseGridPos.y + height);

                if (!isOnPerimeter)
                    continue;

                GridNode node = BuildingManager.Instance.gridManager.GetNode(x, y);
                if (node != null && node.Walkable)
                {
                    // Check if within attack range
                    float distanceToBuilding = Vector3.Distance(node.WorldPosition, building.transform.position);
                    float attackRange = unitType != null ? unitType.Range : 1.5f;

                    if (distanceToBuilding <= attackRange + nodeSize * 0.5f)
                    {
                        candidateNodes.Add(node);
                    }
                }
            }
        }

        // If no candidates found within range, fallback to closest walkable perimeter node ignoring range
        if (candidateNodes.Count == 0)
        {
            for (int x = baseGridPos.x - 1; x <= baseGridPos.x + width; x++)
            {
                for (int y = baseGridPos.y - 1; y <= baseGridPos.y + height; y++)
                {
                    bool isOnPerimeter =
                        (x == baseGridPos.x - 1 || x == baseGridPos.x + width) ||
                        (y == baseGridPos.y - 1 || y == baseGridPos.y + height);

                    if (!isOnPerimeter)
                        continue;

                    GridNode node = BuildingManager.Instance.gridManager.GetNode(x, y);
                    if (node != null && node.Walkable)
                    {
                        candidateNodes.Add(node);
                    }
                }
            }
        }

        // Find closest candidate node to current position
        GridNode closestNode = null;
        float closestDist = float.MaxValue;
        Vector3 unitPos = transform.position;

        foreach (var candidate in candidateNodes)
        {
            float dist = Vector3.Distance(unitPos, candidate.WorldPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestNode = candidate;
            }
        }

        if (closestNode != null)
            return closestNode.WorldPosition;

        // If all else fails, return building center (likely unreachable)
        return building.transform.position;
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            if (finalOccupiedNode != null)
            {
                finalOccupiedNode.Walkable = finalOccupiedNodeOriginalWalkable;
                gridManager.MarkUnoccupied(finalOccupiedNode, this);
            }

            Destroy(gameObject);
        }
    }

    public void RequestPath(Vector3 worldDestinationPosition)
    {
        targetWorldPosition = worldDestinationPosition;
        AstarPathfinding.Instance?.RequestPathfinding(transform.position, worldDestinationPosition, this);
    }

    public void FollowPath(List<GridNode> pathToFollow)
    {
        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        currentPath = pathToFollow;
        currentPathIndex = 0;

        if (finalOccupiedNode != null)
        {
            finalOccupiedNode.Walkable = finalOccupiedNodeOriginalWalkable;
            gridManager.MarkUnoccupied(finalOccupiedNode, this);
        }

        isAttemptingRepath = false;
        movementCoroutine = StartCoroutine(FollowPathCoroutine());
    }

    private IEnumerator FollowPathCoroutine()
    {
        while (currentPathIndex < currentPath.Count)
        {
            GridNode nextNode = currentPath[currentPathIndex];
            Vector3 worldTargetPosition = nextNode.WorldPosition + Vector3.up * 0.5f;
            float waitDuration = 0f;

            while (gridManager.IsOccupied(nextNode) && !IsOnSameNode(nextNode))
            {
                waitDuration += 0.1f;

                if (waitDuration > 2f && currentPathIndex == currentPath.Count - 1 && !isAttemptingRepath)
                {
                    isAttemptingRepath = true;
                    yield return StartCoroutine(TryRepathAroundGoalNode());
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }

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

        if (armyType == ArmyType.Player)
        {
            GridNode finalNode = GetNodeUnderUnit();
            if (finalNode != null)
            {
                finalOccupiedNodeOriginalWalkable = finalNode.Walkable;
                finalNode.Walkable = false;
                gridManager.MarkOccupied(finalNode, this);
                finalOccupiedNode = finalNode;
            }
        }
    }

    private IEnumerator TryRepathAroundGoalNode()
    {
        Vector2Int goalGridCoordinates = gridManager.GetGridPosFromWorld(targetWorldPosition);
        GridNode goalNode = gridManager.GetNode(goalGridCoordinates.x, goalGridCoordinates.y);

        if (goalNode == null) yield break;

        List<GridNode> neighborNodes = gridManager.GetWalkableNeighbors(goalNode, this);
        neighborNodes.Sort((a, b) =>
            Vector3.Distance(transform.position, a.WorldPosition)
            .CompareTo(Vector3.Distance(transform.position, b.WorldPosition)));

        foreach (var alternateNode in neighborNodes)
        {
            if (!gridManager.IsOccupied(alternateNode))
            {
                RequestPath(alternateNode.WorldPosition);
                yield break;
            }
        }

        yield return new WaitForSeconds(0.5f);
        RequestPath(targetWorldPosition);
    }

    private GridNode GetNodeUnderUnit()
    {
        Vector2Int currentGridCoordinates = gridManager.GetGridPosFromWorld(transform.position);
        return gridManager.GetNode(currentGridCoordinates.x, currentGridCoordinates.y);
    }

    private bool IsOnSameNode(GridNode node)
    {
        return node == GetNodeUnderUnit();
    }
}
