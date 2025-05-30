using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AstarPathfinding : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private float stepDelay = 0.05f; // Delay between steps during pathfinding visualization

    private List<GridNode> finalPathToGoal;                   // Final computed path from start to goal
    private List<GridNode> visualizedOpenNodes;              // Nodes in the open set for debug visualization
    private HashSet<GridNode> visualizedClosedNodes;         // Nodes in the closed set for debug visualization
    private GridNode currentlyExpandedNode;                  // Node currently being processed

   
    // Starts the A* pathfinding algorithm with visualization.
   
    public void VisualizePath(Vector3 worldStartPosition, Vector3 worldGoalPosition)
    {
        StopAllCoroutines(); // Stop any ongoing pathfinding
        StartCoroutine(FindPathRoutine(worldStartPosition, worldGoalPosition));
    }

  
    // Coroutine that performs A* pathfinding step by step.
   
    private IEnumerator FindPathRoutine(Vector3 worldStartPosition, Vector3 worldGoalPosition)
    {
        Vector2Int startGridPosition = gridManager.GetGridPosFromWorld(worldStartPosition);
        Vector2Int goalGridPosition = gridManager.GetGridPosFromWorld(worldGoalPosition);

        GridNode startNode = gridManager.GetNode(startGridPosition.x, startGridPosition.y);
        GridNode goalNode = gridManager.GetNode(goalGridPosition.x, goalGridPosition.y);

        if (startNode == null || goalNode == null || !startNode.Walkable || !goalNode.Walkable)
        {
            Debug.LogWarning("Invalid or unwalkable start or goal node.");
            yield break;
        }

        // Initialize the open and closed sets
        List<GridNode> openSet = new List<GridNode> { startNode };
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        // Clear visualization sets
        visualizedOpenNodes = new List<GridNode>();
        visualizedClosedNodes = new HashSet<GridNode>();
        currentlyExpandedNode = null;

        // Reset all node scores before beginning the search
        foreach (GridNode node in gridManager.gridNodes)
        {
            node.GCost = int.MaxValue;
            node.HCost = int.MaxValue;
            node.FCost = int.MaxValue;
            node.Connection = Vector3.zero;
        }

        // Initialize start node cost
        startNode.GCost = 0;
        startNode.HCost = CalculateHeuristic(startNode, goalNode);
        startNode.FCost = startNode.HCost;

        while (openSet.Count > 0)
        {
            GridNode nodeWithLowestFCost = GetLowestFCostNode(openSet);
            currentlyExpandedNode = nodeWithLowestFCost;

            if (nodeWithLowestFCost == goalNode)
            {
                finalPathToGoal = ReconstructPath(startNode, goalNode);
                visualizedOpenNodes = new List<GridNode>(openSet);
                visualizedClosedNodes = new HashSet<GridNode>(closedSet);
                yield break;
            }

            openSet.Remove(nodeWithLowestFCost);
            closedSet.Add(nodeWithLowestFCost);

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

            // Update sets for visualization after each node expansion
            visualizedOpenNodes = new List<GridNode>(openSet);
            visualizedClosedNodes = new HashSet<GridNode>(closedSet);

            yield return new WaitForSeconds(stepDelay);
        }

        Debug.LogWarning("No path found.");
        finalPathToGoal = null;
    }

    
    // Calculates Manhattan heuristic distance between two nodes.

    private int CalculateHeuristic(GridNode fromNode, GridNode toNode)
    {
        Vector2Int fromPos = gridManager.GetGridPosFromWorld(fromNode.WorldPosition);
        Vector2Int toPos = gridManager.GetGridPosFromWorld(toNode.WorldPosition);
        int distanceX = Mathf.Abs(fromPos.x - toPos.x);
        int distanceY = Mathf.Abs(fromPos.y - toPos.y);
        return (distanceX + distanceY) * 5;
    }

    
    // Returns right angle walkable neighbors of a given node.
  
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

    
    // Reconstructs the path from goal to start using backtracking.
    
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
        reconstructedPath.Reverse();
        return reconstructedPath;
    }

    
    // Returns the node with the lowest F cost from the list.
    
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

    
    // Draws debug gizmos for visualization of open/closed sets and the final path.

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw Closed Set - Red cubes
        if (visualizedClosedNodes != null)
        {
            Gizmos.color = Color.red;
            foreach (GridNode node in visualizedClosedNodes)
                Gizmos.DrawCube(node.WorldPosition + Vector3.up * 0.05f, Vector3.one * 0.25f);
        }

        // Draw Open Set - Blue spheres
        if (visualizedOpenNodes != null)
        {
            Gizmos.color = Color.blue;
            foreach (GridNode node in visualizedOpenNodes)
                Gizmos.DrawSphere(node.WorldPosition + Vector3.up * 0.1f, 0.15f);
        }

        // Draw Currently Expanded Node - Yellow
        if (currentlyExpandedNode != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentlyExpandedNode.WorldPosition + Vector3.up * 0.25f, 0.2f);

#if UNITY_EDITOR
            // Show movement cost for neighbors in editor
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

        // Draw Final Path - Cyan line
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
