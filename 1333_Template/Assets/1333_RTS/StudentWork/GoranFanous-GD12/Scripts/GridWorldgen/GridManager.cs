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

    [Header("Grid Visualization")]
    [SerializeField] private GameObject gridCubePrefab; // <-- New: Assign a cube prefab with collider

    public GridNode[,] gridNodes;
    public GridSettings GridSettings => gridSettings;

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

                // --- NEW: Spawn a visible cube mesh for this node ---
                if (gridCubePrefab != null)
                {
                    GameObject cube = Instantiate(gridCubePrefab, worldPosition, Quaternion.identity, transform);
                    cube.name = node.Name;

                    // Resize based on node size
                    //cube.transform.localScale = Vector3.one * gridSettings.NodeSize * 0.9f;

                    // Optional: apply terrain color
                    if (cube.TryGetComponent<Renderer>(out var renderer))
                    {
                        Color color = selectedTerrain.GizmoColor;
                        float alpha = Mathf.InverseLerp(10f, 1f, selectedTerrain.MovementCost);
                        color.a = Mathf.Clamp01(alpha);

                        renderer.material.color = color;
                    }
                }
            }
        }

        occupiedNodes.Clear();
        reservedNodes.Clear();
    }

    // --- Everything below remains unchanged ---

    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
            return gridNodes[x, y];
        return null;
    }

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
            node.TerrainTypes = null;
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

    public List<GridNode> GetWalkableNeighbors(GridNode node, UnitController requestingUnit)
    {
        List<GridNode> neighbors = new();
        Vector2Int gridPosition = GetGridPosFromWorld(node.WorldPosition);

        int[,] directions = new int[,] {
            { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }
        };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int nx = gridPosition.x + directions[i, 0];
            int ny = gridPosition.y + directions[i, 1];

            GridNode neighbor = GetNode(nx, ny);
            if (neighbor != null && neighbor.Walkable && !IsOccupiedOrReservedByOther(neighbor, requestingUnit))
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    public List<GridNode> GetSurroundingAvailableNodes(GridNode centerNode, int layerDepth = 2)
    {
        List<GridNode> availableNodes = new();
        Vector2Int center = GetGridPosFromWorld(centerNode.WorldPosition);

        for (int r = 1; r <= layerDepth; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if ((Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) || (dx == 0 && dy == 0)) continue;

                    int tx = center.x + dx;
                    int ty = center.y + dy;

                    GridNode node = GetNode(tx, ty);
                    if (node != null && node.Walkable && !IsOccupied(node) && !IsReserved(node))
                        availableNodes.Add(node);
                }
            }

            if (availableNodes.Count > 0) break;
        }

        return availableNodes;
    }

    private bool IsUnitAloneInNode(UnitController unit, GridNode node)
    {
        if (unit == null || node == null) return false;

        BoxCollider unitCol = unit.GetComponent<BoxCollider>();
        if (unitCol == null) return true;

        Vector3 center = unitCol.bounds.center;
        Vector3 halfExtents = unitCol.bounds.extents;

        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, unit.transform.rotation);
        foreach (var col in overlaps)
        {
            if (col.gameObject == unit.gameObject) continue;
            if (col.TryGetComponent<UnitController>(out var other) &&
                GetNodeFromWorld(other.transform.position) == node)
                return false;
        }

        return true;
    }

    public void EnsureUnitOccupiesOwnNode(UnitController unit)
    {
        GridNode node = GetNodeFromWorld(unit.transform.position);
        if (node == null) return;

        if (occupiedNodes.TryGetValue(node, out var occupier))
        {
            if (occupier != unit)
            {
                var neighbors = GetWalkableNeighbors(node, unit);
                if (neighbors.Count > 0)
                {
                    var target = neighbors[Random.Range(0, neighbors.Count)];
                    if (TryReserveNode(target, unit))
                        unit.RequestPath(target.WorldPosition);
                }
            }
        }
        else if (IsUnitAloneInNode(unit, node))
        {
            MarkOccupied(node, unit);
        }
    }

    public bool RequestMoveToNode(UnitController unit, GridNode targetNode)
    {
        if (TryReserveNode(targetNode, unit))
        {
            unit.RequestPath(targetNode.WorldPosition);
            return true;
        }

        return false;
    }

    public void CancelReservation(UnitController unit)
    {
        List<GridNode> toRemove = new();
        foreach (var kvp in reservedNodes)
            if (kvp.Value == unit) toRemove.Add(kvp.Key);

        foreach (var node in toRemove)
            reservedNodes.Remove(node);
    }

    public void CheckAndResolveCollisions(UnitController unit)
    {
        Collider[] nearby = Physics.OverlapSphere(unit.transform.position, 0.1f);
        foreach (var col in nearby)
        {
            if (col.gameObject != unit.gameObject &&
                col.TryGetComponent<UnitController>(out var other))
            {
                EnsureUnitOccupiesOwnNode(unit);
                return;
            }
        }

        EnsureUnitOccupiesOwnNode(unit);
    }

    public GridNode GetNodeFromWorld(Vector3 worldPosition)
    {
        Vector2Int coords = GetGridPosFromWorld(worldPosition);
        return GetNode(coords.x, coords.y);
    }

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

                if (!node.Walkable) color = Color.red;
                else if (IsOccupied(node)) color = Color.blue;
                else if (IsReserved(node)) color = Color.cyan;
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