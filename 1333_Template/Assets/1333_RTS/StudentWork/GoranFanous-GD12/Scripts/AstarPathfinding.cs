using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstarPathfinding : MonoBehaviour
{
    // Singleton instance for easy global access
    public static AstarPathfinding Instance { get; private set; }

    [SerializeField] private float stepDelaySeconds = 0.05f;  // Delay between each step of the pathfinding algorithm (for visualization or throttling)
   private GridManager gridManager;

    // Keeps track of active pathfinding coroutines keyed by unit to avoid overlapping pathfind requests
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

        // Find GridManager in scene if not assigned in inspector
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
    /// Requests a pathfinding operation for a unit from start to goal positions.
    /// </summary>
    public void RequestPathfinding(Vector3 worldStartPosition, Vector3 worldGoalPosition, UnitController requestingUnit)
    {
        if (requestingUnit == null) return;

        // Stop any existing pathfinding coroutine for this unit before starting a new one
        if (activePathfindingCoroutines.TryGetValue(requestingUnit, out Coroutine existingCoroutine))
        {
            if (existingCoroutine != null)
                StopCoroutine(existingCoroutine);
        }

        Coroutine newPathfindingCoroutine = StartCoroutine(FindPathCoroutine(worldStartPosition, worldGoalPosition, requestingUnit));
        activePathfindingCoroutines[requestingUnit] = newPathfindingCoroutine;
    }

    /// <summary>
    /// Coroutine that performs the A* pathfinding algorithm step-by-step.
    /// </summary>
    private IEnumerator FindPathCoroutine(Vector3 worldStartPosition, Vector3 worldGoalPosition, UnitController requestingUnit)
    {
        // Convert world positions to grid coordinates
        Vector2Int startGridCoordinates = gridManager.GetGridPosFromWorld(worldStartPosition);
        Vector2Int goalGridCoordinates = gridManager.GetGridPosFromWorld(worldGoalPosition);

        GridNode startNode = gridManager.GetNode(startGridCoordinates.x, startGridCoordinates.y);
        GridNode goalNode = gridManager.GetNode(goalGridCoordinates.x, goalGridCoordinates.y);

        // Validate nodes exist on the grid
        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning("AstarPathfinding: Invalid start or goal node - nodes do not exist.");
            activePathfindingCoroutines.Remove(requestingUnit);
            yield break;
        }

        // If the goal node is currently occupied, try to find the closest unoccupied neighbor
        if (gridManager.IsOccupied(goalNode))
        {
            List<GridNode> walkableNeighbors = gridManager.GetWalkableNeighbors(goalNode, requestingUnit);
            if (walkableNeighbors.Count == 0)
            {
                Debug.LogWarning("AstarPathfinding: Goal node and all neighbors are occupied. No reachable path.");
                activePathfindingCoroutines.Remove(requestingUnit);
                yield break;
            }

            // Pick the neighbor node closest to the start node
            goalNode = FindClosestNodeToPosition(walkableNeighbors, startNode.WorldPosition);
        }

        // Check if start node is walkable and not occupied by other units (it's okay if occupied by this unit)
        bool isStartNodeBlocked = !startNode.Walkable || (gridManager.IsOccupied(startNode) && !IsNodeOccupiedByUnit(startNode, requestingUnit));
        if (isStartNodeBlocked)
        {
            Debug.LogWarning("AstarPathfinding: Start node is blocked or not walkable.");
            activePathfindingCoroutines.Remove(requestingUnit);
            yield break;
        }

        // Initialize the dictionary storing node states: G-cost, H-cost, F-cost, and connection (came from) position
        Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromPosition)> nodeCosts = new();

        foreach (GridNode gridNode in gridManager.gridNodes)
        {
            nodeCosts[gridNode] = (int.MaxValue, int.MaxValue, int.MaxValue, Vector3.zero);
        }

        // Set the start node costs
        int initialHeuristic = CalculateHeuristicCost(startNode, goalNode);
        nodeCosts[startNode] = (0, initialHeuristic, initialHeuristic, Vector3.zero);

        // Open set contains nodes to be evaluated; closed set contains nodes already evaluated
        List<GridNode> openSet = new() { startNode };
        HashSet<GridNode> closedSet = new();

        // Main loop for the A* algorithm
        while (openSet.Count > 0)
        {
            GridNode currentNode = GetNodeWithLowestFCost(openSet, nodeCosts);

            // Check if we have reached the goal node
            if (currentNode == goalNode)
            {
                // Reconstruct the path from start to goal based on node connections
                List<GridNode> finalPath = ReconstructPath(startNode, goalNode, nodeCosts);

                // IMPORTANT: Do NOT mark nodes as occupied here.
                // The unit will mark the final node occupied upon arrival.
                requestingUnit.FollowPath(finalPath);

                activePathfindingCoroutines.Remove(requestingUnit);
                yield break;
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Evaluate all walkable neighbors of the current node
            foreach (GridNode neighborNode in gridManager.GetWalkableNeighbors(currentNode, requestingUnit))
            {
                if (closedSet.Contains(neighborNode))
                    continue;

                int tentativeGCost = nodeCosts[currentNode].GCost + neighborNode.TerrainTypes.MovementCost;

                if (tentativeGCost < nodeCosts[neighborNode].GCost)
                {
                    int heuristicCost = CalculateHeuristicCost(neighborNode, goalNode);
                    nodeCosts[neighborNode] = (
                        tentativeGCost,
                        heuristicCost,
                        tentativeGCost + heuristicCost,
                        currentNode.WorldPosition
                    );

                    if (!openSet.Contains(neighborNode))
                        openSet.Add(neighborNode);
                }
            }

            yield return new WaitForSeconds(stepDelaySeconds);
        }

        Debug.LogWarning("AstarPathfinding: No path found.");
        activePathfindingCoroutines.Remove(requestingUnit);
    }

    /// <summary>
    /// Finds the node closest to the specified world position among a list of nodes.
    /// </summary>
    private GridNode FindClosestNodeToPosition(List<GridNode> nodes, Vector3 targetWorldPosition)
    {
        GridNode closestNode = null;
        float shortestDistance = float.MaxValue;

        foreach (GridNode candidateNode in nodes)
        {
            float distanceToTarget = Vector3.Distance(candidateNode.WorldPosition, targetWorldPosition);
            if (distanceToTarget < shortestDistance)
            {
                shortestDistance = distanceToTarget;
                closestNode = candidateNode;
            }
        }

        return closestNode;
    }

    /// <summary>
    /// Checks whether the specified node is occupied by the given unit.
    /// </summary>
    private bool IsNodeOccupiedByUnit(GridNode node, UnitController unit)
    {
        Vector2Int unitGridCoordinates = gridManager.GetGridPosFromWorld(unit.transform.position);
        GridNode unitCurrentNode = gridManager.GetNode(unitGridCoordinates.x, unitGridCoordinates.y);
        return node == unitCurrentNode;
    }

    /// <summary>
    /// Calculates the heuristic cost (Manhattan distance multiplied by a cost factor) between two nodes.
    /// </summary>
    private int CalculateHeuristicCost(GridNode fromNode, GridNode toNode)
    {
        Vector2Int fromCoordinates = gridManager.GetGridPosFromWorld(fromNode.WorldPosition);
        Vector2Int toCoordinates = gridManager.GetGridPosFromWorld(toNode.WorldPosition);
        return (Mathf.Abs(fromCoordinates.x - toCoordinates.x) + Mathf.Abs(fromCoordinates.y - toCoordinates.y)) * 5;
    }

    /// <summary>
    /// Reconstructs the path from the start node to the end node by following the 'came from' connections.
    /// </summary>
    private List<GridNode> ReconstructPath(GridNode startNode, GridNode endNode, Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromPosition)> nodeCosts)
    {
        List<GridNode> path = new();
        GridNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            Vector2Int cameFromGridCoordinates = gridManager.GetGridPosFromWorld(nodeCosts[currentNode].CameFromPosition);
            currentNode = gridManager.GetNode(cameFromGridCoordinates.x, cameFromGridCoordinates.y);
        }

        path.Add(startNode);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Returns the node with the lowest F cost from a list of nodes.
    /// If there is a tie, returns the node with the lower H cost.
    /// </summary>
    private GridNode GetNodeWithLowestFCost(List<GridNode> nodesToSearch, Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromPosition)> nodeCosts)
    {
        GridNode nodeWithLowestFCost = nodesToSearch[0];

        for (int index = 1; index < nodesToSearch.Count; index++)
        {
            GridNode currentNode = nodesToSearch[index];

            if (nodeCosts[currentNode].FCost < nodeCosts[nodeWithLowestFCost].FCost ||
                (nodeCosts[currentNode].FCost == nodeCosts[nodeWithLowestFCost].FCost && nodeCosts[currentNode].HCost < nodeCosts[nodeWithLowestFCost].HCost))
            {
                nodeWithLowestFCost = currentNode;
            }
        }

        return nodeWithLowestFCost;
    }
}
