using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS_1333;

public class UnitController : MonoBehaviour
{
    public ArmyType armyType;
    public UnitType unitType;
    public AstarPathfinding AstarPathfinding;

    private int currentHealth;
    private Coroutine movementCoroutine;
    private List<GridNode> currentPath;
    private int currentPathIndex;
    private GridManager gridManager;
    private GridNode finalOccupiedNode;
    private bool finalOccupiedNodeOriginalWalkable;
    private BuildingHealth currentTarget;
    private float attackCooldown;
    private float attackTimer = 0f;

    private GridNode currentTargetNode;  // Node adjacent to building to move to and attack from

    // ADD THIS FLAG
    private bool isRequestingPath = false;

    private void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
            Debug.LogError("GridManager missing!");
        else
            Debug.Log("UnitController Awake: GridManager found.");

        currentHealth = unitType != null ? unitType.MaxHp : 10;
        attackCooldown = unitType != null ? 1f / unitType.AttackSpeed : 1f;

        Debug.Log($"UnitController Awake: Health={currentHealth}, AttackCooldown={attackCooldown}");
    }

    private void Start()
    {
        Debug.Log($"UnitController Start: ArmyType={armyType}");

        if (armyType == ArmyType.Enemy)
        {
            Debug.Log("UnitController Start: Starting SeekAndPathToStructureLoop coroutine.");
            StartCoroutine(SeekAndPathToStructureLoop());
        }
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (currentTarget != null && currentTargetNode != null)
        {
            if (IsTargetInAttackRange())
            {
                // We're in attack range - stop moving and attack
                if (attackTimer >= attackCooldown)
                {
                    int damage = unitType != null ? unitType.Damage : 1;
                    currentTarget.ApplyDamage(damage);
                    attackTimer = 0f;

                    // Stop movement since we're attacking
                    if (movementCoroutine != null)
                    {
                        StopCoroutine(movementCoroutine);
                        movementCoroutine = null;
                    }
                }
            }
            else
            {
                // We're NOT in attack range - we need to move closer
                // UPDATED CONDITION: Also check if we're already requesting a path
                if (movementCoroutine == null && !isRequestingPath && (currentPath == null || currentPathIndex >= currentPath.Count))
                {
                    Debug.Log("Update: Requesting path to target node.");
                    RequestPath(currentTargetNode.WorldPosition);
                }
            }
        }
    }

    private bool IsTargetInAttackRange()
    {
        if (currentTarget == null || currentTargetNode == null) return false;

        float attackRange = unitType != null ? unitType.Range : 1.5f;
        Vector3 unitPos = transform.position;

        return Vector3.Distance(unitPos, currentTargetNode.WorldPosition) <= attackRange;
    }

    private List<GridNode> GetBuildingFootprintNodes(BuildingHealth building)
    {
        Vector2Int basePos = gridManager.GetGridPosFromWorld(building.transform.position);

        BuildingType buildingType = building.GetComponent<BuildingType>();
        if (buildingType == null || buildingType.buildingSettings == null)
        {
            Debug.LogWarning("GetBuildingFootprintNodes: Missing BuildingType or BuildingSettings.");
            return new List<GridNode>();
        }

        int width = buildingType.buildingSettings.BuildingSizeX;
        int height = buildingType.buildingSettings.BuildingSizeY;

        List<GridNode> footprint = new();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = gridManager.GetNode(basePos.x + x, basePos.y + y);
                if (node != null)
                    footprint.Add(node);
            }
        }

        return footprint;
    }

    private List<GridNode> GetAdjacentBuildingOccupiedNodes(List<GridNode> footprintNodes)
    {
        HashSet<GridNode> adjacentNodes = new();

        int[] dx = { -1, 0, 1 };
        int[] dy = { -1, 0, 1 };

        HashSet<GridNode> footprintSet = new(footprintNodes);

        foreach (var node in footprintNodes)
        {
            Vector2Int coords = gridManager.GetGridPosFromWorld(node.WorldPosition);
            int x = coords.x;
            int y = coords.y;

            foreach (int offsetX in dx)
            {
                foreach (int offsetY in dy)
                {
                    if (offsetX == 0 && offsetY == 0) continue;

                    GridNode adjacent = gridManager.GetNode(x + offsetX, y + offsetY);
                    if (adjacent != null && !footprintSet.Contains(adjacent))
                    {
                        if (adjacent.Walkable && !gridManager.IsOccupied(adjacent))
                        {
                            adjacentNodes.Add(adjacent);
                        }
                    }
                }
            }
        }

        return new List<GridNode>(adjacentNodes);
    }

    private IEnumerator SeekAndPathToStructureLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (currentTarget == null || currentTarget.Equals(null))
            {
                Debug.Log("SeekAndPathToStructureLoop: Looking for new target...");

                BuildingHealth[] allStructures = Object.FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None);
                Debug.Log($"SeekAndPathToStructureLoop: Found {allStructures.Length} total structures");

                BuildingHealth closest = null;
                float closestDist = float.MaxValue;
                GridNode closestAttackNode = null;

                foreach (var structure in allStructures)
                {
                    if (structure == null || structure.OwnerArmyType != ArmyType.Player)
                        continue;

                    string nameLower = structure.BuildingConfig.BuildingName.ToLowerInvariant();

                    // Match unit attack type with building type
                    if (unitType.AttackType == AttackType.TowerBreaker && !nameLower.Contains("tower"))
                        continue;
                    if (unitType.AttackType == AttackType.CastleBreaker && !(nameLower.Contains("castle") || nameLower.Contains("barracks")))
                        continue;
                    if (unitType.AttackType == AttackType.HappyBreaker && !(nameLower.Contains("temple") || nameLower.Contains("library") || nameLower.Contains("market")))
                        continue;
                    if (unitType.AttackType == AttackType.FoodBreaker && !(nameLower.Contains("farm") || nameLower.Contains("granary")))
                        continue;

                    List<GridNode> footprint = GetBuildingFootprintNodes(structure);
                    if (footprint.Count == 0)
                        continue;

                    List<GridNode> adjacentOccupiedNodes = GetAdjacentBuildingOccupiedNodes(footprint);
                    if (adjacentOccupiedNodes.Count == 0)
                        continue;

                    Vector3 unitPos = transform.position;
                    float minDistToUnit = float.MaxValue;
                    GridNode candidateNode = null;

                    foreach (var node in adjacentOccupiedNodes)
                    {
                        if (gridManager.IsOccupied(node)) continue;

                        float dist = Vector3.Distance(unitPos, node.WorldPosition);
                        if (dist < minDistToUnit)
                        {
                            minDistToUnit = dist;
                            candidateNode = node;
                        }
                    }

                    if (candidateNode != null && minDistToUnit < closestDist)
                    {
                        closestDist = minDistToUnit;
                        closest = structure;
                        closestAttackNode = candidateNode;
                    }
                }

                if (closest != null && closestAttackNode != null)
                {
                    currentTarget = closest;
                    currentTargetNode = closestAttackNode;

                    Debug.Log($"SeekAndPathToStructureLoop: Selected target {closest.BuildingConfig.BuildingName} with attack node at {closestAttackNode.WorldPosition}");

                    RequestPath(currentTargetNode.WorldPosition);
                }
                else
                {
                    Debug.Log("SeekAndPathToStructureLoop: No valid targets found!");
                    currentTarget = null;
                    currentTargetNode = null;
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

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

    public bool RequestPath(Vector3 worldDestination)
    {
        Debug.Log($"RequestPath: Requesting path from {transform.position} to {worldDestination}");

        if (AstarPathfinding != null)
        {
            isRequestingPath = true; // SET FLAG HERE
            AstarPathfinding.RequestPathfinding(transform.position, worldDestination, this);
            return true;
        }
        else
        {
            Debug.LogError("RequestPath: AstarPathfinding is null!");
            return false;
        }
    }

    public void OnPathFound(List<GridNode> path)
    {
        isRequestingPath = false; // CLEAR FLAG HERE

        if (path == null || path.Count == 0)
        {
            Debug.Log("OnPathFound: No path found!");
            return;
        }

        Debug.Log($"OnPathFound: Path received with {path.Count} nodes.");

        // Debug: Print the path
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log($"OnPathFound: Path node {i}: {path[i].WorldPosition}");
        }

        FollowPath(path);
    }

    public void FollowPath(List<GridNode> pathToFollow)
    {
        Debug.Log("FollowPath: Called.");

        if (movementCoroutine != null)
        {
            Debug.Log("FollowPath: Stopping previous movement coroutine.");
            StopCoroutine(movementCoroutine);
        }

        currentPath = pathToFollow;
        currentPathIndex = 0;

        if (finalOccupiedNode != null)
        {
            finalOccupiedNode.Walkable = finalOccupiedNodeOriginalWalkable;
            gridManager.MarkUnoccupied(finalOccupiedNode, this);
            finalOccupiedNode = null;
        }

        movementCoroutine = StartCoroutine(FollowPathCoroutine());
    }

    private IEnumerator FollowPathCoroutine()
    {
        Debug.Log($"FollowPathCoroutine: Started with {currentPath.Count} nodes, MoveSpeed={unitType?.MoveSpeed}");

        while (currentPathIndex < currentPath.Count)
        {
            GridNode nextNode = currentPath[currentPathIndex];
            Vector3 targetPos = nextNode.WorldPosition + Vector3.up * 0.5f;

            Debug.Log($"FollowPathCoroutine: Moving to node {currentPathIndex} at {targetPos}, current pos: {transform.position}");

            float speed = unitType != null ? unitType.MoveSpeed : 3f;

            // Add safety check for speed
            if (speed <= 0)
            {
                Debug.LogError($"FollowPathCoroutine: Invalid speed {speed}! Using default speed of 3.");
                speed = 3f;
            }

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                Vector3 oldPos = transform.position;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

                // Log movement every 60 frames to see if it's actually moving
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"FollowPathCoroutine: Moving from {oldPos} to {transform.position} (delta: {transform.position - oldPos})");
                }

                yield return null;
            }

            Debug.Log($"FollowPathCoroutine: Reached node {currentPathIndex}");
            currentPathIndex++;
            yield return null;
        }

        Debug.Log("FollowPathCoroutine: Path complete.");

        GridNode finalNode = GetNodeUnderUnit();
        if (finalNode != null)
        {
            finalOccupiedNodeOriginalWalkable = finalNode.Walkable;
            finalNode.Walkable = false;
            gridManager.MarkOccupied(finalNode, this);
            finalOccupiedNode = finalNode;
        }

        movementCoroutine = null; // Clear the reference
    }

    private GridNode GetNodeUnderUnit()
    {
        Vector2Int coords = gridManager.GetGridPosFromWorld(transform.position);
        return gridManager.GetNode(coords.x, coords.y);
    }
}