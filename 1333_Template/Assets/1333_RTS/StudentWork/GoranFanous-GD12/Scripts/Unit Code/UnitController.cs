using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS_1333;

// Main controller for individual units - handles movement, pathfinding, targeting, and combat
// This is where all the unit AI logic lives
public class UnitController : MonoBehaviour
{
    // Basic unit identity and references
    public ArmyType armyType;
    public UnitType unitType;
    public AstarPathfinding AstarPathfinding;

    // Health and survival tracking
    private int currentHealth;

    // Movement system variables
    private Coroutine movementCoroutine;
    private List<GridNode> currentPath;
    private int currentPathIndex;
    private GridManager gridManager;

    // Grid occupation tracking - units need to mark their final position as occupied
    private GridNode finalOccupiedNode;
    private bool finalOccupiedNodeOriginalWalkable;

    // Combat and targeting system
    private BuildingHealth currentTarget;
    private float attackCooldown;
    private float attackTimer = 0f;
    private GridNode currentTargetNode;  // Node adjacent to building to move to and attack from

    // Pathfinding state management - prevents spam requests
    private bool isRequestingPath = false;

    // Rotation settings
    private float rotationSpeed = 180f; // Degrees per second

    // Damage flash effect
    private Renderer unitRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    // Initial setup - find dependencies and configure unit stats
    private void Awake()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
            Debug.LogError("GridManager missing!");
        else
            Debug.Log("UnitController Awake: GridManager found.");

        // Set up unit stats from the ScriptableObject data
        currentHealth = unitType != null ? unitType.MaxHp : 10;
        attackCooldown = unitType != null ? 1f / unitType.AttackSpeed : 1f;

        // Set up damage flash effect
        unitRenderer = GetComponent<Renderer>();
        if (unitRenderer != null && unitRenderer.material != null)
        {
            originalColor = unitRenderer.material.color;
        }

        Debug.Log($"UnitController Awake: Health={currentHealth}, AttackCooldown={attackCooldown}");
    }

    // Start the unit's behavior based on its army type
    // Enemy units automatically start seeking targets, player units wait for orders
    private void Start()
    {
        Debug.Log($"UnitController Start: ArmyType={armyType}");

        if (armyType == ArmyType.Enemy)
        {
            Debug.Log("UnitController Start: Starting SeekAndPathToStructureLoop coroutine.");
            StartCoroutine(SeekAndPathToStructureLoop());
        }
    }

    // Main update loop - handles combat timing and movement coordination
    private void Update()
    {
        attackTimer += Time.deltaTime;

        // Rotate towards target building if we have one
        if (currentTarget != null)
        {
            RotateTowardsTarget(currentTarget.transform.position);
        }

        // Only do combat logic if we have a target and know where to attack from
        if (currentTarget != null && currentTargetNode != null)
        {
            if (IsTargetInAttackRange())
            {
                // We're close enough to attack - stop moving and start shooting
                if (attackTimer >= attackCooldown)
                {
                    int damage = unitType != null ? unitType.Damage : 1;
                    currentTarget.ApplyDamage(damage);
                    attackTimer = 0f;

                    // Stop any movement since we're now in combat
                    if (movementCoroutine != null)
                    {
                        StopCoroutine(movementCoroutine);
                        movementCoroutine = null;
                    }
                }
            }
            else
            {
                // We're too far away - need to get closer to our attack position
                // Make sure we're not already moving or requesting a path to avoid conflicts
                if (movementCoroutine == null && !isRequestingPath && (currentPath == null || currentPathIndex >= currentPath.Count))
                {
                    Debug.Log("Update: Requesting path to target node.");
                    RequestPath(currentTargetNode.WorldPosition);
                }
            }
        }
    }

    // Smoothly rotate the unit to face a target position
    private void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Keep rotation only on the Y-axis (horizontal plane)

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // Check if we're close enough to attack our current target
    // Uses the unit's attack range and compares to distance to the target node
    private bool IsTargetInAttackRange()
    {
        if (currentTarget == null || currentTargetNode == null) return false;

        float attackRange = unitType != null ? unitType.Range : 1.5f;
        Vector3 unitPos = transform.position;

        return Vector3.Distance(unitPos, currentTargetNode.WorldPosition) <= attackRange;
    }

    // Get all the grid nodes that make up a building's footprint
    // Buildings can be larger than 1x1, so we need to check their actual size
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

        // Add all nodes within the building's rectangular footprint
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

    // Find all walkable nodes adjacent to a building that units can attack from
    // This is where units will try to position themselves for combat
    private List<GridNode> GetAdjacentBuildingOccupiedNodes(List<GridNode> footprintNodes)
    {
        HashSet<GridNode> adjacentNodes = new();

        // Check all 8 directions around each footprint node
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
                    if (offsetX == 0 && offsetY == 0) continue; // Skip the center node

                    GridNode adjacent = gridManager.GetNode(x + offsetX, y + offsetY);
                    // Only add nodes that are walkable, unoccupied, and not part of the building itself
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

    // Main AI loop for enemy units - constantly seeks new targets and moves to attack them
    // This runs in the background and handles target selection and initial pathfinding
    private IEnumerator SeekAndPathToStructureLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Check for targets twice per second

            // Only look for new targets if we don't have one or our current target is dead
            if (currentTarget == null || currentTarget.Equals(null))
            {
                Debug.Log("SeekAndPathToStructureLoop: Looking for new target...");

                BuildingHealth[] allStructures = Object.FindObjectsByType<BuildingHealth>(FindObjectsSortMode.None);
                Debug.Log($"SeekAndPathToStructureLoop: Found {allStructures.Length} total structures");

                BuildingHealth closest = null;
                float closestDist = float.MaxValue;
                GridNode closestAttackNode = null;

                // Evaluate each potential target
                foreach (var structure in allStructures)
                {
                    // Skip invalid or enemy buildings
                    if (structure == null || structure.OwnerArmyType != ArmyType.Player)
                        continue;

                    string nameLower = structure.BuildingConfig.BuildingName.ToLowerInvariant();

                    // Unit specialization - different units target different building types
                    // This creates more interesting tactical gameplay
                    if (unitType.AttackType == AttackType.TowerBreaker && !nameLower.Contains("tower"))
                        continue;
                    if (unitType.AttackType == AttackType.CastleBreaker && !(nameLower.Contains("castle") || nameLower.Contains("barracks")))
                        continue;
                    if (unitType.AttackType == AttackType.HappyBreaker && !(nameLower.Contains("temple") || nameLower.Contains("library") || nameLower.Contains("market")))
                        continue;
                    if (unitType.AttackType == AttackType.FoodBreaker && !(nameLower.Contains("farm") || nameLower.Contains("granary")))
                        continue;

                    // Get the building's footprint and find attack positions around it
                    List<GridNode> footprint = GetBuildingFootprintNodes(structure);
                    if (footprint.Count == 0)
                        continue;

                    List<GridNode> adjacentOccupiedNodes = GetAdjacentBuildingOccupiedNodes(footprint);
                    if (adjacentOccupiedNodes.Count == 0)
                        continue; // No valid attack positions

                    // Find the closest attack position to this unit
                    Vector3 unitPos = transform.position;
                    float minDistToUnit = float.MaxValue;
                    GridNode candidateNode = null;

                    foreach (var node in adjacentOccupiedNodes)
                    {
                        if (gridManager.IsOccupied(node)) continue; // Skip occupied spots

                        float dist = Vector3.Distance(unitPos, node.WorldPosition);
                        if (dist < minDistToUnit)
                        {
                            minDistToUnit = dist;
                            candidateNode = node;
                        }
                    }

                    // Track the overall closest target across all buildings
                    if (candidateNode != null && minDistToUnit < closestDist)
                    {
                        closestDist = minDistToUnit;
                        closest = structure;
                        closestAttackNode = candidateNode;
                    }
                }

                // If we found a good target, start moving toward it
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

    // Handle taking damage and death
    // When a unit dies, it needs to clean up its grid occupation
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Start damage flash effect
        if (unitRenderer != null && flashCoroutine == null)
        {
            flashCoroutine = StartCoroutine(FlashDamageEffect());
        }

        if (currentHealth <= 0)
        {
            // Clean up grid occupation before dying
            if (finalOccupiedNode != null)
            {
                finalOccupiedNode.Walkable = finalOccupiedNodeOriginalWalkable;
                gridManager.MarkUnoccupied(finalOccupiedNode, this);
            }

            Destroy(gameObject);
        }
    }

    // Flash red when taking damage
    private IEnumerator FlashDamageEffect()
    {
        if (unitRenderer == null || unitRenderer.material == null)
        {
            flashCoroutine = null;
            yield break;
        }

        // Flash red briefly
        unitRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        // Return to original color
        unitRenderer.material.color = originalColor;
        yield return new WaitForSeconds(0.05f);

        flashCoroutine = null;
    }

    // Request a path to a destination - this is the main interface for movement
    // Sets the pathfinding flag to prevent duplicate requests
    public bool RequestPath(Vector3 worldDestination)
    {
        Debug.Log($"RequestPath: Requesting path from {transform.position} to {worldDestination}");

        if (AstarPathfinding != null)
        {
            isRequestingPath = true; // Prevent duplicate path requests
            AstarPathfinding.RequestPathfinding(transform.position, worldDestination, this);
            return true;
        }
        else
        {
            Debug.LogError("RequestPath: AstarPathfinding is null!");
            return false;
        }
    }

    // Callback from the pathfinding system when a path is found
    // This is where we receive the calculated route and start moving
    public void OnPathFound(List<GridNode> path)
    {
        isRequestingPath = false; // Clear the pathfinding request flag

        if (path == null || path.Count == 0)
        {
            Debug.Log("OnPathFound: No path found!");
            return;
        }

        Debug.Log($"OnPathFound: Path received with {path.Count} nodes.");

        // Debug output to see the calculated path
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log($"OnPathFound: Path node {i}: {path[i].WorldPosition}");
        }

        FollowPath(path);
    }

    // Set up path following - stops any current movement and starts the new path
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

        // Clean up any previous grid occupation
        if (finalOccupiedNode != null)
        {
            finalOccupiedNode.Walkable = finalOccupiedNodeOriginalWalkable;
            gridManager.MarkUnoccupied(finalOccupiedNode, this);
            finalOccupiedNode = null;
        }

        movementCoroutine = StartCoroutine(FollowPathCoroutine());
    }

    // The actual movement execution - moves the unit along the calculated path
    // This runs as a coroutine so it can span multiple frames
    private IEnumerator FollowPathCoroutine()
    {
        Debug.Log($"FollowPathCoroutine: Started with {currentPath.Count} nodes, MoveSpeed={unitType?.MoveSpeed}");

        while (currentPathIndex < currentPath.Count)
        {
            GridNode nextNode = currentPath[currentPathIndex];
            Vector3 targetPos = nextNode.WorldPosition + Vector3.up * 0.5f; // Lift slightly above ground

            Debug.Log($"FollowPathCoroutine: Moving to node {currentPathIndex} at {targetPos}, current pos: {transform.position}");

            float speed = unitType != null ? unitType.MoveSpeed : 3f;

            // Safety check - make sure we have a valid movement speed
            if (speed <= 0)
            {
                Debug.LogError($"FollowPathCoroutine: Invalid speed {speed}! Using default speed of 3.");
                speed = 3f;
            }

            // Move toward the current target node
            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                Vector3 oldPos = transform.position;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

                // Periodic debug output to verify movement is actually happening
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"FollowPathCoroutine: Moving from {oldPos} to {transform.position} (delta: {transform.position - oldPos})");
                }

                yield return null; // Wait for next frame
            }

            Debug.Log($"FollowPathCoroutine: Reached node {currentPathIndex}");
            currentPathIndex++;
            yield return null;
        }

        Debug.Log("FollowPathCoroutine: Path complete.");

        // When we reach our destination, mark our final position as occupied
        // This prevents other units from trying to stand in the same spot
        GridNode finalNode = GetNodeUnderUnit();
        if (finalNode != null)
        {
            finalOccupiedNodeOriginalWalkable = finalNode.Walkable;
            finalNode.Walkable = false;
            gridManager.MarkOccupied(finalNode, this);
            finalOccupiedNode = finalNode;
        }

        movementCoroutine = null; // Clear the coroutine reference
    }

    // Helper method to find which grid node the unit is currently standing on
    private GridNode GetNodeUnderUnit()
    {
        Vector2Int coords = gridManager.GetGridPosFromWorld(transform.position);
        return gridManager.GetNode(coords.x, coords.y);
    }
}