using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstarPathfinding : MonoBehaviour
{
    public static AstarPathfinding Instance { get; private set; }

    [SerializeField] private float stepDelaySeconds = 0.05f;

    private GridManager gridManager;

    // Tracks which units are currently pathfinding
    private Dictionary<UnitController, Coroutine> activePathCoroutines = new();

    public GridManager GridManager => gridManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager == null)
            Debug.LogError("GridManager not found in the scene!");
    }

    public void RequestPathfinding(Vector3 startWorldPos, Vector3 goalWorldPos, UnitController requestingUnit)
    {
        if (requestingUnit == null) return;

        // Cancel any existing pathfinding for this unit
        if (activePathCoroutines.TryGetValue(requestingUnit, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        Coroutine newPathCoroutine = StartCoroutine(FindPathCoroutine(startWorldPos, goalWorldPos, requestingUnit));
        activePathCoroutines[requestingUnit] = newPathCoroutine;
    }

    private IEnumerator FindPathCoroutine(Vector3 startWorldPos, Vector3 goalWorldPos, UnitController unit)
    {
        Vector2Int startGridPos = gridManager.GetGridPosFromWorld(startWorldPos);
        Vector2Int goalGridPos = gridManager.GetGridPosFromWorld(goalWorldPos);

        GridNode startNode = gridManager.GetNode(startGridPos.x, startGridPos.y);
        GridNode goalNode = gridManager.GetNode(goalGridPos.x, goalGridPos.y);

        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning("AstarPathfinding: Start or goal node is invalid.");
            activePathCoroutines.Remove(unit);
            yield break;
        }

        if (gridManager.IsOccupied(goalNode))
        {
            List<GridNode> alternativeGoals = gridManager.GetWalkableNeighbors(goalNode, unit);
            if (alternativeGoals.Count == 0)
            {
                Debug.LogWarning("AstarPathfinding: Goal and neighbors are occupied.");
                activePathCoroutines.Remove(unit);
                yield break;
            }

            goalNode = FindClosestNodeToPosition(alternativeGoals, startNode.WorldPosition);
        }

        bool isStartNodeBlocked = !startNode.Walkable || (gridManager.IsOccupied(startNode) && !IsNodeOccupiedByUnit(startNode, unit));
        if (isStartNodeBlocked)
        {
            Debug.LogWarning("AstarPathfinding: Start node is blocked.");
            activePathCoroutines.Remove(unit);
            yield break;
        }

        Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromWorldPos)> nodeCostMap = new();
        foreach (GridNode node in gridManager.gridNodes)
        {
            nodeCostMap[node] = (int.MaxValue, int.MaxValue, int.MaxValue, Vector3.zero);
        }

        int startHeuristic = CalculateHeuristic(startNode, goalNode);
        nodeCostMap[startNode] = (0, startHeuristic, startHeuristic, Vector3.zero);

        List<GridNode> openSet = new() { startNode };
        HashSet<GridNode> closedSet = new();

        while (openSet.Count > 0)
        {
            GridNode currentNode = GetNodeWithLowestFCost(openSet, nodeCostMap);

            if (currentNode == goalNode)
            {
                List<GridNode> finalPath = ReconstructPath(startNode, goalNode, nodeCostMap);
                unit.FollowPath(finalPath);
                activePathCoroutines.Remove(unit);
                yield break;
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (GridNode neighbor in gridManager.GetWalkableNeighbors(currentNode, unit))
            {
                if (closedSet.Contains(neighbor)) continue;

                int tentativeGCost = nodeCostMap[currentNode].GCost + neighbor.TerrainTypes.MovementCost;

                if (tentativeGCost < nodeCostMap[neighbor].GCost)
                {
                    int heuristic = CalculateHeuristic(neighbor, goalNode);
                    nodeCostMap[neighbor] = (
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
        activePathCoroutines.Remove(unit);
    }

    private GridNode FindClosestNodeToPosition(List<GridNode> nodeList, Vector3 targetPos)
    {
        GridNode closestNode = null;
        float shortestDistance = float.MaxValue;

        foreach (GridNode node in nodeList)
        {
            float distance = Vector3.Distance(node.WorldPosition, targetPos);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestNode = node;
            }
        }

        return closestNode;
    }

    private bool IsNodeOccupiedByUnit(GridNode node, UnitController unit)
    {
        Vector2Int unitGridPos = gridManager.GetGridPosFromWorld(unit.transform.position);
        GridNode unitNode = gridManager.GetNode(unitGridPos.x, unitGridPos.y);
        return node == unitNode;
    }

    private int CalculateHeuristic(GridNode from, GridNode to)
    {
        Vector2Int fromPos = gridManager.GetGridPosFromWorld(from.WorldPosition);
        Vector2Int toPos = gridManager.GetGridPosFromWorld(to.WorldPosition);

        int deltaX = Mathf.Abs(fromPos.x - toPos.x);
        int deltaY = Mathf.Abs(fromPos.y - toPos.y);

        return (deltaX + deltaY) * 5; // Manhattan Distance * weight
    }

    private List<GridNode> ReconstructPath(GridNode startNode, GridNode goalNode, Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromWorldPos)> costMap)
    {
        List<GridNode> path = new();
        GridNode current = goalNode;

        while (current != startNode)
        {
            path.Add(current);
            Vector2Int parentGridPos = gridManager.GetGridPosFromWorld(costMap[current].CameFromWorldPos);
            current = gridManager.GetNode(parentGridPos.x, parentGridPos.y);
        }

        path.Add(startNode);
        path.Reverse();
        return path;
    }

    private GridNode GetNodeWithLowestFCost(List<GridNode> nodes, Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromWorldPos)> costMap)
    {
        GridNode lowestFCostNode = nodes[0];

        for (int i = 1; i < nodes.Count; i++)
        {
            GridNode node = nodes[i];
            var currentCost = costMap[node];
            var bestCost = costMap[lowestFCostNode];

            if (currentCost.FCost < bestCost.FCost || (currentCost.FCost == bestCost.FCost && currentCost.HCost < bestCost.HCost))
            {
                lowestFCostNode = node;
            }
        }

        return lowestFCostNode;
    }
}
