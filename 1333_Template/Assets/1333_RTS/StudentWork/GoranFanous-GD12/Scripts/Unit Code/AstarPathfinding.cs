using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstarPathfinding : MonoBehaviour
{
    // Singleton instance for easy global access
    public static AstarPathfinding Instance { get; private set; }

    [SerializeField] private float stepDelaySeconds = 0.05f; // Delay between each pathfinding step (for throttling or visualization)

    private GridManager gridManager;

    // Tracks running pathfinding coroutines to prevent overlapping tasks per unit
    private Dictionary<UnitController, Coroutine> activePathfindingCoroutines = new();

    public GridManager GridManager => gridManager;

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        // Auto-assign GridManager if not set in inspector
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }

        if (gridManager == null)
        {
            Debug.LogError("GridManager not found in the scene!");
        }
    }

    /// <summary>
    /// Requests a path for a given unit from start to goal position in world space.
    /// </summary>
    public void RequestPathfinding(Vector3 worldStartPosition, Vector3 worldGoalPosition, UnitController requestingUnit)
    {
        if (requestingUnit == null) return;

        // Cancel existing coroutine if already pathfinding for this unit
        if (activePathfindingCoroutines.TryGetValue(requestingUnit, out Coroutine existingCoroutine))
        {
            if (existingCoroutine != null)
                StopCoroutine(existingCoroutine);
        }

        // Start a new coroutine for this unit's path
        Coroutine newPathfindingCoroutine = StartCoroutine(FindPathCoroutine(worldStartPosition, worldGoalPosition, requestingUnit));
        activePathfindingCoroutines[requestingUnit] = newPathfindingCoroutine;
    }

    /// <summary>
    /// Coroutine that performs A* pathfinding step-by-step.
    /// </summary>
    private IEnumerator FindPathCoroutine(Vector3 worldStartPosition, Vector3 worldGoalPosition, UnitController requestingUnit)
    {
        // Convert world positions to grid coordinates
        Vector2Int startGridPosition = gridManager.GetGridPosFromWorld(worldStartPosition);
        Vector2Int goalGridPosition = gridManager.GetGridPosFromWorld(worldGoalPosition);

        GridNode startNode = gridManager.GetNode(startGridPosition.x, startGridPosition.y);
        GridNode goalNode = gridManager.GetNode(goalGridPosition.x, goalGridPosition.y);

        // Validate grid nodes exist
        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning("AstarPathfinding: Invalid start or goal node - nodes do not exist.");
            activePathfindingCoroutines.Remove(requestingUnit);
            yield break;
        }

        // If the goal node is occupied, find the closest walkable neighbor instead
        if (gridManager.IsOccupied(goalNode))
        {
            List<GridNode> walkableNeighbors = gridManager.GetWalkableNeighbors(goalNode, requestingUnit);
            if (walkableNeighbors.Count == 0)
            {
                Debug.LogWarning("AstarPathfinding: Goal node and all neighbors are occupied. No reachable path.");
                activePathfindingCoroutines.Remove(requestingUnit);
                yield break;
            }

            // Pick the neighbor closest to the start
            goalNode = FindClosestNodeToPosition(walkableNeighbors, startNode.WorldPosition);
        }

        // Make sure the start node is valid and not blocked
        bool isStartNodeBlocked = !startNode.Walkable || (gridManager.IsOccupied(startNode) && !IsNodeOccupiedByUnit(startNode, requestingUnit));
        if (isStartNodeBlocked)
        {
            Debug.LogWarning("AstarPathfinding: Start node is blocked or not walkable.");
            activePathfindingCoroutines.Remove(requestingUnit);
            yield break;
        }

        // Set up initial cost data for each node
        Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromPosition)> nodeCosts = new();

        foreach (GridNode node in gridManager.gridNodes)
        {
            nodeCosts[node] = (int.MaxValue, int.MaxValue, int.MaxValue, Vector3.zero);
        }

        int heuristicToGoal = CalculateHeuristicCost(startNode, goalNode);
        nodeCosts[startNode] = (0, heuristicToGoal, heuristicToGoal, Vector3.zero);

        List<GridNode> openSet = new() { startNode };
        HashSet<GridNode> closedSet = new();

        // A* Algorithm Main Loop
        while (openSet.Count > 0)
        {
            GridNode currentNode = GetNodeWithLowestFCost(openSet, nodeCosts);

            if (currentNode == goalNode)
            {
                List<GridNode> finalPath = ReconstructPath(startNode, goalNode, nodeCosts);
                requestingUnit.FollowPath(finalPath);
                activePathfindingCoroutines.Remove(requestingUnit);
                yield break;
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (GridNode neighbor in gridManager.GetWalkableNeighbors(currentNode, requestingUnit))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                int tentativeGCost = nodeCosts[currentNode].GCost + neighbor.TerrainTypes.MovementCost;

                if (tentativeGCost < nodeCosts[neighbor].GCost)
                {
                    int heuristic = CalculateHeuristicCost(neighbor, goalNode);
                    nodeCosts[neighbor] = (
                        tentativeGCost,
                        heuristic,
                        tentativeGCost + heuristic,
                        currentNode.WorldPosition
                    );

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }

            yield return new WaitForSeconds(stepDelaySeconds);
        }

        Debug.LogWarning("AstarPathfinding: No path found.");
        activePathfindingCoroutines.Remove(requestingUnit);
    }

    /// <summary>
    /// Finds the node in the list closest to a specific world position.
    /// </summary>
    private GridNode FindClosestNodeToPosition(List<GridNode> nodes, Vector3 targetWorldPosition)
    {
        GridNode closest = null;
        float minDistance = float.MaxValue;

        foreach (GridNode node in nodes)
        {
            float distance = Vector3.Distance(node.WorldPosition, targetWorldPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = node;
            }
        }

        return closest;
    }

    /// <summary>
    /// Checks if a node is currently occupied by a specific unit.
    /// </summary>
    private bool IsNodeOccupiedByUnit(GridNode node, UnitController unit)
    {
        Vector2Int unitGridPosition = gridManager.GetGridPosFromWorld(unit.transform.position);
        GridNode unitNode = gridManager.GetNode(unitGridPosition.x, unitGridPosition.y);
        return node == unitNode;
    }

    /// <summary>
    /// Uses Manhattan distance as the heuristic (adjusted with a factor).
    /// </summary>
    private int CalculateHeuristicCost(GridNode fromNode, GridNode toNode)
    {
        Vector2Int fromPosition = gridManager.GetGridPosFromWorld(fromNode.WorldPosition);
        Vector2Int toPosition = gridManager.GetGridPosFromWorld(toNode.WorldPosition);

        int deltaX = Mathf.Abs(fromPosition.x - toPosition.x); // Renamed dx to deltaX
        int deltaY = Mathf.Abs(fromPosition.y - toPosition.y); // Renamed dy to deltaY

        return (deltaX + deltaY) * 5; // Weighted heuristic
    }

    /// <summary>
    /// Reconstructs the final path by tracing each node's parent.
    /// </summary>
    private List<GridNode> ReconstructPath(GridNode startNode, GridNode endNode, Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromPosition)> nodeCosts)
    {
        List<GridNode> path = new();
        GridNode current = endNode;

        while (current != startNode)
        {
            path.Add(current);

            Vector2Int parentGridPosition = gridManager.GetGridPosFromWorld(nodeCosts[current].CameFromPosition);
            current = gridManager.GetNode(parentGridPosition.x, parentGridPosition.y); // Renamed nx, ny to parentGridPosition
        }

        path.Add(startNode);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Gets the node with the lowest F-cost in the open set.
    /// </summary>
    private GridNode GetNodeWithLowestFCost(List<GridNode> nodesToSearch, Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromPosition)> nodeCosts)
    {
        GridNode lowestFCostNode = nodesToSearch[0];

        for (int i = 1; i < nodesToSearch.Count; i++)
        {
            GridNode node = nodesToSearch[i];
            if (nodeCosts[node].FCost < nodeCosts[lowestFCostNode].FCost ||
               (nodeCosts[node].FCost == nodeCosts[lowestFCostNode].FCost && nodeCosts[node].HCost < nodeCosts[lowestFCostNode].HCost))
            {
                lowestFCostNode = node;
            }
        }

        return lowestFCostNode;
    }
}
