using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AstarPathfinding : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private float stepDelay = 0.05f; // Delay between pathfinding steps for visualization

    private List<GridNode> finalPathToGoal; // Stores the final computed path from start to goal
    private List<GridNode> visualizedOpenNodes; // Stores nodes in the open set for debug visualization
    private HashSet<GridNode> visualizedClosedNodes; // Stores nodes in the closed set for debug visualization
    private GridNode currentlyExpandedNode; // The node currently being processed in the algorithm

    public bool ShowDebugVisualization = true; // Toggle to show open/closed/expanded node visualization
    public bool ShowFinalPathVisualization = true; // Toggle to show the final computed path

    // Public entry point to start the pathfinding process
    public void VisualizePath(Vector3 worldStartPosition, Vector3 worldGoalPosition, UnitController unit)
    {
        StopAllCoroutines(); // Stop any previously running pathfinding
        StartCoroutine(FindPathRoutine(worldStartPosition, worldGoalPosition, unit));
    }

    // Main coroutine that performs the A* pathfinding logic step-by-step
    private IEnumerator FindPathRoutine(Vector3 worldStartPosition, Vector3 worldGoalPosition, UnitController unit)
    {
        // Convert world positions to grid coordinates
        Vector2Int startGridPosition = gridManager.GetGridPosFromWorld(worldStartPosition);
        Vector2Int goalGridPosition = gridManager.GetGridPosFromWorld(worldGoalPosition);

        GridNode startNode = gridManager.GetNode(startGridPosition.x, startGridPosition.y);
        GridNode goalNode = gridManager.GetNode(goalGridPosition.x, goalGridPosition.y);

        // Validate start and goal nodes
        if (startNode == null || goalNode == null || !startNode.Walkable || !goalNode.Walkable)
        {
            Debug.LogWarning("Invalid or unwalkable start or goal node.");
            yield break;
        }

        // Setup open and closed sets for A* search
        List<GridNode> openSet = new List<GridNode> { startNode };
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        // Initialize visualization state
        visualizedOpenNodes = new List<GridNode>();
        visualizedClosedNodes = new HashSet<GridNode>();
        currentlyExpandedNode = null;

        // Reset cost values on all nodes before starting
        foreach (GridNode node in gridManager.gridNodes)
        {
            node.GCost = int.MaxValue;
            node.HCost = int.MaxValue;
            node.FCost = int.MaxValue;
            node.Connection = Vector3.zero;
        }

        // Set up starting node
        startNode.GCost = 0;
        startNode.HCost = CalculateHeuristic(startNode, goalNode);
        startNode.FCost = startNode.HCost;

        // Main A* loop
        while (openSet.Count > 0)
        {
            GridNode nodeWithLowestFCost = GetLowestFCostNode(openSet);
            currentlyExpandedNode = nodeWithLowestFCost;

            // If goal is reached, reconstruct path and pass it to the unit
            if (nodeWithLowestFCost == goalNode)
            {
                finalPathToGoal = ReconstructPath(startNode, goalNode);
                visualizedOpenNodes = new List<GridNode>(openSet);
                visualizedClosedNodes = new HashSet<GridNode>(closedSet);

                if (finalPathToGoal != null && unit != null)
                {
                    unit.FollowPath(finalPathToGoal); // Tell unit to follow the computed path
                }

                yield break; // Exit coroutine
            }

            // Move current node from open to closed set
            openSet.Remove(nodeWithLowestFCost);
            closedSet.Add(nodeWithLowestFCost);

            // Check all valid neighbors
            foreach (GridNode neighborNode in GetWalkableNeighbors(nodeWithLowestFCost))
            {
                if (closedSet.Contains(neighborNode))
                    continue;

                int movementCostToNeighbor = nodeWithLowestFCost.GCost + neighborNode.TerrainTypes.MovementCost;

                if (movementCostToNeighbor < neighborNode.GCost)
                {
                    neighborNode.Connection = nodeWithLowestFCost.WorldPosition;
                    neighborNode.GCost = movementCostToNeighbor;
                    neighborNode.HCost = CalculateHeuristic(neighborNode, goalNode);
                    neighborNode.FCost = neighborNode.GCost + neighborNode.HCost;

                    if (!openSet.Contains(neighborNode))
                        openSet.Add(neighborNode);
                }
            }

            // Update visualizations after expanding the node
            visualizedOpenNodes = new List<GridNode>(openSet);
            visualizedClosedNodes = new HashSet<GridNode>(closedSet);

            yield return new WaitForSeconds(stepDelay);
        }

        Debug.LogWarning("No path found.");
        finalPathToGoal = null;
    }

    // Calculates the Manhattan distance between two nodes as heuristic
    private int CalculateHeuristic(GridNode fromNode, GridNode toNode)
    {
        Vector2Int fromPos = gridManager.GetGridPosFromWorld(fromNode.WorldPosition);
        Vector2Int toPos = gridManager.GetGridPosFromWorld(toNode.WorldPosition);
        int distanceX = Mathf.Abs(fromPos.x - toPos.x);
        int distanceY = Mathf.Abs(fromPos.y - toPos.y);
        return (distanceX + distanceY) * 5;
    }

    // Gets all walkable orthogonal (non-diagonal) neighbors
    private List<GridNode> GetWalkableNeighbors(GridNode centerNode)
    {
        List<GridNode> neighbors = new List<GridNode>();
        Vector2Int centerGridPos = gridManager.GetGridPosFromWorld(centerNode.WorldPosition);
        int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int neighborX = centerGridPos.x + directions[i, 0];
            int neighborY = centerGridPos.y + directions[i, 1];
            GridNode neighbor = gridManager.GetNode(neighborX, neighborY);
            if (neighbor != null && neighbor.Walkable)
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    // Traces the path back from goal to start using node.Connection
    private List<GridNode> ReconstructPath(GridNode startNode, GridNode goalNode)
    {
        List<GridNode> reconstructedPath = new List<GridNode>();
        GridNode currentNode = goalNode;

        while (currentNode != startNode)
        {
            reconstructedPath.Add(currentNode);
            Vector2Int previousNodePos = gridManager.GetGridPosFromWorld(currentNode.Connection);
            currentNode = gridManager.GetNode(previousNodePos.x, previousNodePos.y);
        }

        reconstructedPath.Add(startNode);
        reconstructedPath.Reverse(); // Reverse to get path from start to goal
        return reconstructedPath;
    }

    // Finds the node with the lowest F cost (and lowest H as tie-breaker)
    private GridNode GetLowestFCostNode(List<GridNode> nodeList)
    {
        GridNode bestNode = nodeList[0];
        for (int i = 1; i < nodeList.Count; i++)
        {
            if (nodeList[i].FCost < bestNode.FCost ||
                (nodeList[i].FCost == bestNode.FCost && nodeList[i].HCost < bestNode.HCost))
            {
                bestNode = nodeList[i];
            }
        }
        return bestNode;
    }

    // Draws debug Gizmos in the scene view for visual feedback
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (ShowDebugVisualization)
        {
            // Red cubes for closed set
            if (visualizedClosedNodes != null)
            {
                Gizmos.color = Color.red;
                foreach (GridNode node in visualizedClosedNodes)
                    Gizmos.DrawCube(node.WorldPosition + Vector3.up * 0.05f, Vector3.one * 0.25f);
            }

            // Blue spheres for open set
            if (visualizedOpenNodes != null)
            {
                Gizmos.color = Color.blue;
                foreach (GridNode node in visualizedOpenNodes)
                    Gizmos.DrawSphere(node.WorldPosition + Vector3.up * 0.1f, 0.15f);
            }

            // Yellow sphere for currently expanded node
            if (currentlyExpandedNode != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(currentlyExpandedNode.WorldPosition + Vector3.up * 0.25f, 0.2f);

#if UNITY_EDITOR
                // Label movement cost of walkable neighbors
                List<GridNode> neighbors = GetWalkableNeighbors(currentlyExpandedNode);
                foreach (GridNode neighbor in neighbors)
                {
                    if (neighbor != null)
                    {
                        Vector3 labelPosition = neighbor.WorldPosition + Vector3.up * 0.5f;
                        string costText = neighbor.TerrainTypes != null
                            ? $"{neighbor.TerrainTypes.MovementCost}"
                            : "NoCost";
                        Handles.Label(labelPosition, costText);
                    }
                }
#endif
            }
        }

        if (ShowFinalPathVisualization)
        {
            // Cyan spheres and lines for final path
            if (finalPathToGoal != null)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < finalPathToGoal.Count - 1; i++)
                {
                    Gizmos.DrawSphere(finalPathToGoal[i].WorldPosition + Vector3.up * 0.3f, 0.15f);
                    Gizmos.DrawLine(finalPathToGoal[i].WorldPosition + Vector3.up * 0.3f,
                                    finalPathToGoal[i + 1].WorldPosition + Vector3.up * 0.3f);
                }

                if (finalPathToGoal.Count > 0)
                    Gizmos.DrawSphere(finalPathToGoal[^1].WorldPosition + Vector3.up * 0.3f, 0.2f);
            }
        }
    }
}
