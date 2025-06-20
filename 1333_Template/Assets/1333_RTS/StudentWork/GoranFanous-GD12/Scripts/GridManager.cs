using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the grid used for A* pathfinding, node occupation, reservation,
/// terrain assignment, and collision resolution for units.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private GridSettings gridSettings;
    [SerializeField] private TerrainType defaultTerrainType;
    [SerializeField] private List<TerrainType> terrainTypes;
    [SerializeField] private bool useRandomTerrain = true;

    public GridNode[,] gridNodes;
    public GridSettings GridSettings => gridSettings;

    // Track occupied and reserved grid nodes per unit
    private Dictionary<GridNode, UnitController> occupiedNodes = new();
    private Dictionary<GridNode, UnitController> reservedNodes = new();
    private Dictionary<GridNode, TerrainType> originalTerrainTypes = new();

    /// <summary>
    /// Initializes the grid nodes and assigns terrain types.
    /// </summary>
    public void InitializeGrid()
    {
        gridNodes = new GridNode[gridSettings.GridSizeX, gridSettings.GridSizeY];
        originalTerrainTypes.Clear();

        // Get all walkable terrain types for safe use
        List<TerrainType> walkableTerrains = terrainTypes.FindAll(t => t.IsWalkable);

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                Vector3 worldPosition = gridSettings.UseXZPlane
                    ? new Vector3(x, 0, y) * gridSettings.NodeSize
                    : new Vector3(x, y, 0) * gridSettings.NodeSize;

                TerrainType selectedTerrain = (useRandomTerrain && walkableTerrains.Count > 0)
                    ? walkableTerrains[Random.Range(0, walkableTerrains.Count)]
                    : defaultTerrainType;

                GridNode node = new GridNode
                {
                    Name = $"Cell_{x}_{y}",
                    WorldPosition = worldPosition,
                    TerrainTypes = selectedTerrain,
                    Walkable = selectedTerrain.IsWalkable
                };

                gridNodes[x, y] = node;
                originalTerrainTypes[node] = selectedTerrain;
            }
        }

        occupiedNodes.Clear();
        reservedNodes.Clear();
    }

    /// <summary>Returns the grid node at the given coordinates, or null if out of bounds.</summary>
    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
            return gridNodes[x, y];
        return null;
    }

    /// <summary>Converts a world position into grid coordinates.</summary>
    public Vector2Int GetGridPosFromWorld(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / gridSettings.NodeSize);
        int y = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPosition.z / gridSettings.NodeSize)
            : Mathf.RoundToInt(worldPosition.y / gridSettings.NodeSize);

        return new Vector2Int(x, y);
    }

    public bool IsOccupied(GridNode node) => occupiedNodes.ContainsKey(node);
    public bool IsReserved(GridNode node) => reservedNodes.ContainsKey(node);

    public bool IsOccupiedOrReservedByOther(GridNode node, UnitController unit)
    {
        return (occupiedNodes.TryGetValue(node, out var occupier) && occupier != unit)
            || (reservedNodes.TryGetValue(node, out var reserver) && reserver != unit);
    }

    public void MarkOccupied(GridNode node, UnitController unit)
    {
        if (node == null) return;

        if (reservedNodes.TryGetValue(node, out var reserver) && reserver == unit)
            reservedNodes.Remove(node);

        if (!occupiedNodes.ContainsKey(node))
        {
            occupiedNodes[node] = unit;
            node.TerrainTypes = null; // Optional visual override
        }
    }

    public void MarkUnoccupied(GridNode node, UnitController unit)
    {
        if (node == null) return;

        if (occupiedNodes.TryGetValue(node, out var occupier) && occupier == unit)
        {
            occupiedNodes.Remove(node);
            if (originalTerrainTypes.TryGetValue(node, out var originalTerrain))
                node.TerrainTypes = originalTerrain;
        }
    }

    public bool TryReserveNode(GridNode node, UnitController unit)
    {
        if (node == null || IsOccupiedOrReservedByOther(node, unit)) return false;

        reservedNodes[node] = unit;
        return true;
    }

    public void ReleaseReservation(GridNode node, UnitController unit)
    {
        if (node != null && reservedNodes.TryGetValue(node, out var reserver) && reserver == unit)
            reservedNodes.Remove(node);
    }

    /// <summary>
    /// Returns a list of adjacent walkable and unoccupied nodes (no diagonals).
    /// </summary>
    public List<GridNode> GetWalkableNeighbors(GridNode node, UnitController requestingUnit)
    {
        List<GridNode> neighbors = new();
        Vector2Int gridPos = GetGridPosFromWorld(node.WorldPosition);

        int[,] directions = new int[,] {
            { 0, 1 },   // Up
            { 1, 0 },   // Right
            { 0, -1 },  // Down
            { -1, 0 }   // Left
        };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int neighborX = gridPos.x + directions[i, 0];
            int neighborY = gridPos.y + directions[i, 1];

            GridNode neighbor = GetNode(neighborX, neighborY);
            if (neighbor != null && neighbor.Walkable && !IsOccupiedOrReservedByOther(neighbor, requestingUnit))
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    /// <summary>
    /// Searches surrounding layers around a node for available walkable nodes.
    /// </summary>
    public List<GridNode> GetSurroundingAvailableNodes(GridNode centerNode, int layerDepth = 2)
    {
        List<GridNode> availableNodes = new();
        Vector2Int centerPos = GetGridPosFromWorld(centerNode.WorldPosition);

        for (int radius = 1; radius <= layerDepth; radius++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    if ((Mathf.Abs(offsetX) != radius && Mathf.Abs(offsetY) != radius) || (offsetX == 0 && offsetY == 0))
                        continue;

                    int targetX = centerPos.x + offsetX;
                    int targetY = centerPos.y + offsetY;

                    GridNode candidate = GetNode(targetX, targetY);
                    if (candidate != null && candidate.Walkable && !IsOccupied(candidate) && !IsReserved(candidate))
                        availableNodes.Add(candidate);
                }
            }

            if (availableNodes.Count > 0)
                break;
        }

        return availableNodes;
    }

    /// <summary>
    /// Checks whether the given unit is physically alone inside the specified node.
    /// </summary>
    private bool IsUnitAloneInNode(UnitController unit, GridNode node)
    {
        if (unit == null || node == null) return false;

        BoxCollider collider = unit.GetComponent<BoxCollider>();
        if (collider == null)
        {
            Debug.LogWarning("Unit missing BoxCollider component.");
            return true;
        }

        Vector3 center = collider.bounds.center;
        Vector3 halfExtents = collider.bounds.extents;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, unit.transform.rotation);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject == unit.gameObject) continue;

            if (hit.TryGetComponent<UnitController>(out UnitController otherUnit))
            {
                GridNode otherNode = GetNodeFromWorld(otherUnit.transform.position);
                if (otherNode == node)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Ensures the unit is occupying its own node, physically and logically.
    /// </summary>
    public void EnsureUnitOccupiesOwnNode(UnitController unit)
    {
        GridNode currentNode = GetNodeFromWorld(unit.transform.position);
        if (currentNode == null) return;

        if (occupiedNodes.TryGetValue(currentNode, out var occupier))
        {
            if (occupier != unit)
            {
                List<GridNode> neighbors = GetWalkableNeighbors(currentNode, unit);
                if (neighbors.Count > 0)
                {
                    GridNode targetNode = neighbors[Random.Range(0, neighbors.Count)];
                    if (TryReserveNode(targetNode, unit))
                        unit.RequestPath(targetNode.WorldPosition);
                }
            }
        }
        else if (IsUnitAloneInNode(unit, currentNode))
        {
            MarkOccupied(currentNode, unit);
        }
    }

    /// <summary>
    /// Attempts to reserve a node and request a path for a unit.
    /// </summary>
    public bool RequestMoveToNode(UnitController unit, GridNode targetNode)
    {
        if (TryReserveNode(targetNode, unit))
        {
            unit.RequestPath(targetNode.WorldPosition);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cancels all reservations made by the unit.
    /// </summary>
    public void CancelReservation(UnitController unit)
    {
        List<GridNode> nodesToRemove = new();

        foreach (var kvp in reservedNodes)
        {
            if (kvp.Value == unit)
                nodesToRemove.Add(kvp.Key);
        }

        foreach (var node in nodesToRemove)
        {
            reservedNodes.Remove(node);
        }
    }

    /// <summary>
    /// Checks for overlapping units and attempts to resolve by re-occupying correct node.
    /// </summary>
    public void CheckAndResolveCollisions(UnitController unit)
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(unit.transform.position, 0.1f);
        foreach (Collider collider in nearbyColliders)
        {
            if (collider.gameObject != unit.gameObject &&
                collider.TryGetComponent<UnitController>(out var otherUnit))
            {
                EnsureUnitOccupiesOwnNode(unit);
                return;
            }
        }

        EnsureUnitOccupiesOwnNode(unit);
    }

    public GridNode GetNodeFromWorld(Vector3 position)
    {
        Vector2Int gridPos = GetGridPosFromWorld(position);
        return GetNode(gridPos.x, gridPos.y);
    }

    /// <summary>
    /// Draws the grid in the editor using Gizmos for debugging.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (gridNodes == null || gridSettings == null) return;

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                GridNode node = gridNodes[x, y];
                if (node == null) continue;

                Color color;
                if (!node.Walkable)
                    color = Color.red;
                else if (IsOccupied(node))
                    color = Color.blue;
                else if (IsReserved(node))
                    color = Color.cyan;
                else
                {
                    TerrainType terrain = node.TerrainTypes ?? defaultTerrainType;
                    float alpha = Mathf.InverseLerp(10f, 1f, terrain.MovementCost);
                    color = terrain.GizmoColor;
                    color.a = Mathf.Clamp01(alpha);
                }

                Gizmos.color = color;
                Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * gridSettings.NodeSize * 0.9f);
            }
        }
    }
}
