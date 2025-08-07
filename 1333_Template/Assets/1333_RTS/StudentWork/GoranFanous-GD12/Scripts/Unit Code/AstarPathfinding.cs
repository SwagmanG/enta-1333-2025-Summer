using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Main A* pathfinding system - handles all the route-finding logic for units
// This is a singleton since we only want one pathfinding manager in the scene
public class AstarPathfinding : MonoBehaviour
{
    public static AstarPathfinding Instance { get; private set; }

    // Small delay between pathfinding steps - helps prevent frame drops on complex paths
    [SerializeField] private float stepDelaySeconds = 0.01f;

    private GridManager gridManager;

    // Keep track of which units are currently calculating paths
    // This prevents multiple pathfinding requests from the same unit overlapping
    private Dictionary<UnitController, Coroutine> activePathCoroutines = new();

    public GridManager GridManager => gridManager;

    // Standard singleton setup - find the grid manager and make sure everything's connected
    private void Awake()
    {
        Instance = this;
        Debug.Log("AstarPathfinding: Instance set.");

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            Debug.LogError("AstarPathfinding: GridManager not found in the scene!");
        else
            Debug.Log("AstarPathfinding: GridManager found.");
    }

    // This is the main entry point - units call this when they want to move somewhere
    // We handle stopping any existing pathfinding for the same unit to avoid conflicts
    public void RequestPathfinding(Vector3 startWorldPos, Vector3 goalWorldPos, UnitController requestingUnit)
    {
        if (requestingUnit == null)
        {
            Debug.LogWarning("AstarPathfinding: Requesting unit is null.");
            return;
        }

        Debug.Log($"RequestPathfinding: Request received from unit {requestingUnit.name} from {startWorldPos} to {goalWorldPos}");

        // If this unit is already pathfinding, stop the old one first
        // This handles cases where players spam-click or units get new orders mid-path
        if (activePathCoroutines.TryGetValue(requestingUnit, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            Debug.Log("RequestPathfinding: Stopping existing pathfinding coroutine for unit.");
            StopCoroutine(existingCoroutine);
        }

        // Start the new pathfinding request and track it
        Coroutine newPathCoroutine = StartCoroutine(FindPathCoroutine(startWorldPos, goalWorldPos, requestingUnit));
        activePathCoroutines[requestingUnit] = newPathCoroutine;
    }

    // The main pathfinding algorithm - this does all the heavy lifting
    // Using a coroutine so we can spread the work across multiple frames
    private IEnumerator FindPathCoroutine(Vector3 startWorldPos, Vector3 goalWorldPos, UnitController unit)
    {
        Debug.Log($"FindPathCoroutine: Starting pathfinding for unit {unit.name}.");

        // Convert world positions to grid coordinates
        Vector2Int startGridPos = gridManager.GetGridPosFromWorld(startWorldPos);
        Vector2Int goalGridPos = gridManager.GetGridPosFromWorld(goalWorldPos);

        Debug.Log($"FindPathCoroutine: Start grid pos: {startGridPos}, Goal grid pos: {goalGridPos}");

        GridNode startNode = gridManager.GetNode(startGridPos.x, startGridPos.y);
        GridNode goalNode = gridManager.GetNode(goalGridPos.x, goalGridPos.y);

        // Basic validation - make sure we have valid start and end points
        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning("FindPathCoroutine: Start or goal node invalid.");
            activePathCoroutines.Remove(unit);
            yield break;
        }

        // Handle the case where the goal is a building or blocked area
        // Instead of failing, we'll find the nearest walkable spot around it
        if (!goalNode.Walkable || gridManager.IsOccupied(goalNode))
        {
            Debug.Log("FindPathCoroutine: Goal node blocked or occupied, searching for adjacent walkable nodes.");

            // Get all the nodes that make up the building's footprint
            List<GridNode> buildingNodes = GetBuildingFootprintNodes(goalNode);

            // Find all walkable nodes adjacent to the building
            List<GridNode> candidateGoalNodes = new List<GridNode>();
            foreach (var buildingNode in buildingNodes)
            {
                foreach (var neighbor in gridManager.GetWalkableNeighbors(buildingNode, unit))
                {
                    if (!candidateGoalNodes.Contains(neighbor) && !gridManager.IsOccupied(neighbor))
                    {
                        candidateGoalNodes.Add(neighbor);
                        Debug.Log($"FindPathCoroutine: Candidate node added at {neighbor.WorldPosition}");
                    }
                }
            }

            // If we can't find anywhere to go near the building, give up
            if (candidateGoalNodes.Count == 0)
            {
                Debug.LogWarning("FindPathCoroutine: No walkable adjacent nodes found around building.");
                activePathCoroutines.Remove(unit);
                yield break;
            }

            // Pick the closest valid spot to where we started
            goalNode = FindClosestNodeToPosition(candidateGoalNodes, startNode.WorldPosition);
            Debug.Log($"FindPathCoroutine: New goal node selected at {goalNode.WorldPosition}");
        }

        // Make sure our starting position is actually valid
        // We allow the unit to be on its current node, but not if another unit is there
        bool isStartNodeBlocked = !startNode.Walkable || (gridManager.IsOccupied(startNode) && !IsNodeOccupiedByUnit(startNode, unit));
        if (isStartNodeBlocked)
        {
            Debug.LogWarning("FindPathCoroutine: Start node is blocked.");
            activePathCoroutines.Remove(unit);
            yield break;
        }

        // Initialize the cost tracking for A* algorithm
        // Each node tracks G cost (distance from start), H cost (heuristic to goal), F cost (G+H), and parent
        Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromWorldPos)> costMap = new();
        foreach (var node in gridManager.GridNodes)
        {
            costMap[node] = (int.MaxValue, int.MaxValue, int.MaxValue, Vector3.zero);
        }

        // Set up the starting node with zero cost to itself
        int initialHeuristic = CalculateHeuristic(startNode, goalNode);
        costMap[startNode] = (0, initialHeuristic, initialHeuristic, Vector3.zero);

        // A* algorithm - open set contains nodes to evaluate, closed set contains evaluated nodes
        List<GridNode> openSet = new() { startNode };
        HashSet<GridNode> closedSet = new();

        // Main A* loop - keep going until we find the goal or run out of options
        while (openSet.Count > 0)
        {
            // Always pick the node with the best estimated total cost
            GridNode currentNode = GetNodeWithLowestFCost(openSet, costMap);

            // Success! We reached the goal, time to build the final path
            if (currentNode == goalNode)
            {
                Debug.Log("FindPathCoroutine: Goal node reached, reconstructing path.");
                List<GridNode> finalPath = ReconstructPath(startNode, goalNode, costMap);
                unit.OnPathFound(finalPath);
                activePathCoroutines.Remove(unit);
                yield break;
            }

            // Move current node from open to closed set
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Check all neighbors of the current node
            foreach (var neighbor in gridManager.GetWalkableNeighbors(currentNode, unit))
            {
                // Skip nodes we've already fully evaluated
                if (closedSet.Contains(neighbor))
                    continue;

                // Calculate what the cost would be to reach this neighbor through the current node
                int tentativeGCost = costMap[currentNode].GCost + neighbor.TerrainTypes.MovementCost;

                // If this path to the neighbor is better than any previous one, update it
                if (tentativeGCost < costMap[neighbor].GCost)
                {
                    int heuristic = CalculateHeuristic(neighbor, goalNode);
                    costMap[neighbor] = (tentativeGCost, heuristic, tentativeGCost + heuristic, currentNode.WorldPosition);

                    // Add to open set if it's not already there
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }

            // Small delay to prevent frame drops on complex paths
            yield return new WaitForSeconds(stepDelaySeconds);
        }

        // We've exhausted all possibilities and couldn't find a path
        Debug.LogWarning("FindPathCoroutine: No path found.");
        activePathCoroutines.Remove(unit);
    }

    // Find all nodes that are part of a building's footprint
    // Uses flood fill to get all connected non-walkable nodes
    private List<GridNode> GetBuildingFootprintNodes(GridNode startNode)
    {
        List<GridNode> footprintNodes = new();
        HashSet<GridNode> visited = new();
        Queue<GridNode> queue = new();
        queue.Enqueue(startNode);
        visited.Add(startNode);

        // Standard flood fill algorithm
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            footprintNodes.Add(current);

            // Only expand to other non-walkable nodes (part of the same building)
            foreach (var neighbor in gridManager.GetNeighbors(current))
            {
                if (!visited.Contains(neighbor) && !neighbor.Walkable)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return footprintNodes;
    }

    // Simple utility to find the closest node to a given position
    // Used when we need to pick the best goal node around a building
    private GridNode FindClosestNodeToPosition(List<GridNode> nodes, Vector3 pos)
    {
        GridNode closest = null;
        float minDist = float.MaxValue;

        foreach (var node in nodes)
        {
            float dist = Vector3.Distance(node.WorldPosition, pos);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }

        return closest;
    }

    // Check if a specific node is occupied by a specific unit
    // This helps us determine if a unit can start pathfinding from its current position
    private bool IsNodeOccupiedByUnit(GridNode node, UnitController unit)
    {
        Vector2Int unitGridPos = gridManager.GetGridPosFromWorld(unit.transform.position);
        GridNode unitNode = gridManager.GetNode(unitGridPos.x, unitGridPos.y);
        return node == unitNode;
    }

    // Manhattan distance heuristic for A* - works well for grid-based movement
    // Multiplied by 5 to give it some weight in the pathfinding decisions
    private int CalculateHeuristic(GridNode from, GridNode to)
    {
        Vector2Int fromPos = gridManager.GetGridPosFromWorld(from.WorldPosition);
        Vector2Int toPos = gridManager.GetGridPosFromWorld(to.WorldPosition);

        int dx = Mathf.Abs(fromPos.x - toPos.x);
        int dy = Mathf.Abs(fromPos.y - toPos.y);

        return (dx + dy) * 5;
    }

    // Walk backwards from the goal to the start using the parent pointers
    // This gives us the final path the unit should follow
    private List<GridNode> ReconstructPath(GridNode startNode, GridNode goalNode, Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromWorldPos)> costMap)
    {
        List<GridNode> path = new();
        GridNode current = goalNode;

        // Follow the chain of parent nodes back to the start
        while (current != startNode)
        {
            path.Add(current);
            Vector2Int parentGridPos = gridManager.GetGridPosFromWorld(costMap[current].CameFromWorldPos);
            current = gridManager.GetNode(parentGridPos.x, parentGridPos.y);
        }

        path.Add(startNode);
        path.Reverse(); // We built it backwards, so flip it around

        return path;
    }

    // Find the node with the best F cost (and use H cost as tiebreaker)
    // This is the core of A* - always expand the most promising node first
    private GridNode GetNodeWithLowestFCost(List<GridNode> nodes, Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromWorldPos)> costMap)
    {
        GridNode bestNode = nodes[0];
        var bestCost = costMap[bestNode];

        for (int i = 1; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var cost = costMap[node];

            // Prefer lower F cost, but if tied, prefer lower H cost (closer to goal)
            if (cost.FCost < bestCost.FCost || (cost.FCost == bestCost.FCost && cost.HCost < bestCost.HCost))
            {
                bestNode = node;
                bestCost = cost;
            }
        }

        return bestNode;
    }
}