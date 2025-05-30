using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    public List<GridNode> FindPath(Vector3 startWorldPos, Vector3 goalWorldPos)
    {
        Vector2Int startGridPos = gridManager.GetGridPosFromWorld(startWorldPos);
        Vector2Int goalGridPos = gridManager.GetGridPosFromWorld(goalWorldPos);

        GridNode startNode = gridManager.GetNode(startGridPos.x, startGridPos.y);
        GridNode goalNode = gridManager.GetNode(goalGridPos.x, goalGridPos.y);

        if (startNode == null || goalNode == null || !startNode.Walkable || !goalNode.Walkable)
        {
            Debug.LogWarning("Invalid start or goal node.");
            return null;
        }

        Queue<GridNode> queue = new Queue<GridNode>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            GridNode current = queue.Dequeue();

            if (current == goalNode)
            {
                return ReconstructPath(startNode, goalNode, cameFrom);
            }

            foreach (GridNode neighbor in GetNeighbors(current))
            {
                if (!neighbor.Walkable || visited.Contains(neighbor))
                    continue;

                queue.Enqueue(neighbor);
                visited.Add(neighbor);
                cameFrom[neighbor] = current;
            }
        }

        Debug.LogWarning("No path found.");
        return null;
    }

    private List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
        Vector2Int pos = gridManager.GetGridPosFromWorld(node.WorldPosition);
        int[,] directions = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

        for (int i = 0; i < 4; i++)
        {
            int neighborX = pos.x + directions[i, 0];
            int neighborY = pos.y + directions[i, 1];
            GridNode neighbor = gridManager.GetNode(neighborX, neighborY);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    private List<GridNode> ReconstructPath(GridNode start, GridNode goal, Dictionary<GridNode, GridNode> cameFrom)
    {
        List<GridNode> path = new List<GridNode>();
        GridNode current = goal;

        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && currentPath != null)
        {
            Gizmos.color = Color.cyan;
            foreach (GridNode node in currentPath)
            {
                Gizmos.DrawSphere(node.WorldPosition, 0.2f);
            }
        }
    }

    private List<GridNode> currentPath;

    public void VisualizePath(Vector3 start, Vector3 goal)
    {
        currentPath = FindPath(start, goal);
    }
}

