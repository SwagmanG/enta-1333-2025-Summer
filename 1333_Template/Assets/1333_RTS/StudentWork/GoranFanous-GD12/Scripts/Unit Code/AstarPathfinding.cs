using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstarPathfinding : MonoBehaviour
{
    public static AstarPathfinding Instance { get; private set; }

    [SerializeField] private float stepDelaySeconds = 0.01f;

    private GridManager gridManager;

    // Tracks which units are currently pathfinding
    private Dictionary<UnitController, Coroutine> activePathCoroutines = new();

    public GridManager GridManager => gridManager;

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

    public void RequestPathfinding(Vector3 startWorldPos, Vector3 goalWorldPos, UnitController requestingUnit)
    {
        if (requestingUnit == null)
        {
            Debug.LogWarning("AstarPathfinding: Requesting unit is null.");
            return;
        }

        Debug.Log($"RequestPathfinding: Request received from unit {requestingUnit.name} from {startWorldPos} to {goalWorldPos}");

        if (activePathCoroutines.TryGetValue(requestingUnit, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            Debug.Log("RequestPathfinding: Stopping existing pathfinding coroutine for unit.");
            StopCoroutine(existingCoroutine);
        }

        Coroutine newPathCoroutine = StartCoroutine(FindPathCoroutine(startWorldPos, goalWorldPos, requestingUnit));
        activePathCoroutines[requestingUnit] = newPathCoroutine;
    }

    private IEnumerator FindPathCoroutine(Vector3 startWorldPos, Vector3 goalWorldPos, UnitController unit)
    {
        Debug.Log($"FindPathCoroutine: Starting pathfinding for unit {unit.name}.");

        Vector2Int startGridPos = gridManager.GetGridPosFromWorld(startWorldPos);
        Vector2Int goalGridPos = gridManager.GetGridPosFromWorld(goalWorldPos);

        Debug.Log($"FindPathCoroutine: Start grid pos: {startGridPos}, Goal grid pos: {goalGridPos}");

        GridNode startNode = gridManager.GetNode(startGridPos.x, startGridPos.y);
        GridNode goalNode = gridManager.GetNode(goalGridPos.x, goalGridPos.y);

        if (startNode == null || goalNode == null)
        {
            Debug.LogWarning("FindPathCoroutine: Start or goal node invalid.");
            activePathCoroutines.Remove(unit);
            yield break;
        }

        if (!goalNode.Walkable || gridManager.IsOccupied(goalNode))
        {
            Debug.Log("FindPathCoroutine: Goal node blocked or occupied, searching for adjacent walkable nodes.");

            List<GridNode> buildingNodes = GetBuildingFootprintNodes(goalNode);

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

            if (candidateGoalNodes.Count == 0)
            {
                Debug.LogWarning("FindPathCoroutine: No walkable adjacent nodes found around building.");
                activePathCoroutines.Remove(unit);
                yield break;
            }

            goalNode = FindClosestNodeToPosition(candidateGoalNodes, startNode.WorldPosition);
            Debug.Log($"FindPathCoroutine: New goal node selected at {goalNode.WorldPosition}");
        }

        bool isStartNodeBlocked = !startNode.Walkable || (gridManager.IsOccupied(startNode) && !IsNodeOccupiedByUnit(startNode, unit));
        if (isStartNodeBlocked)
        {
            Debug.LogWarning("FindPathCoroutine: Start node is blocked.");
            activePathCoroutines.Remove(unit);
            yield break;
        }

        Dictionary<GridNode, (int GCost, int HCost, int FCost, Vector3 CameFromWorldPos)> costMap = new();
        foreach (var node in gridManager.GridNodes)
        {
            costMap[node] = (int.MaxValue, int.MaxValue, int.MaxValue, Vector3.zero);
        }

        int initialHeuristic = CalculateHeuristic(startNode, goalNode);
        costMap[startNode] = (0, initialHeuristic, initialHeuristic, Vector3.zero);

        List<GridNode> openSet = new() { startNode };
        HashSet<GridNode> closedSet = new();

        while (openSet.Count > 0)
        {
            GridNode currentNode = GetNodeWithLowestFCost(openSet, costMap);

            if (currentNode == goalNode)
            {
                Debug.Log("FindPathCoroutine: Goal node reached, reconstructing path.");
                List<GridNode> finalPath = ReconstructPath(startNode, goalNode, costMap);
                unit.OnPathFound(finalPath);
                activePathCoroutines.Remove(unit);
                yield break;
            }


            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (var neighbor in gridManager.GetWalkableNeighbors(currentNode, unit))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                int tentativeGCost = costMap[currentNode].GCost + neighbor.TerrainTypes.MovementCost;

                if (tentativeGCost < costMap[neighbor].GCost)
                {
                    int heuristic = CalculateHeuristic(neighbor, goalNode);
                    costMap[neighbor] = (tentativeGCost, heuristic, tentativeGCost + heuristic, currentNode.WorldPosition);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }

            yield return new WaitForSeconds(stepDelaySeconds);
        }

        Debug.LogWarning("FindPathCoroutine: No path found.");
        activePathCoroutines.Remove(unit);
    }

    private List<GridNode> GetBuildingFootprintNodes(GridNode startNode)
    {
        List<GridNode> footprintNodes = new();
        HashSet<GridNode> visited = new();
        Queue<GridNode> queue = new();
        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            footprintNodes.Add(current);

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

        int dx = Mathf.Abs(fromPos.x - toPos.x);
        int dy = Mathf.Abs(fromPos.y - toPos.y);

        return (dx + dy) * 5;
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
        GridNode bestNode = nodes[0];
        var bestCost = costMap[bestNode];

        for (int i = 1; i < nodes.Count; i++)
        {
            var node = nodes[i];
            var cost = costMap[node];

            if (cost.FCost < bestCost.FCost || (cost.FCost == bestCost.FCost && cost.HCost < bestCost.HCost))
            {
                bestNode = node;
                bestCost = cost;
            }
        }

        return bestNode;
    }
}
