using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the grid used for A* pathfinding, node occupation, reservation,
/// terrain assignment, and collision resolution for units.
/// </summary>
public class GridManager : MonoBehaviour
{
    // Configuration settings for how our grid should be set up
    [Header("Grid Configuration")]
    [SerializeField] private GridSettings gridSettings;
    [SerializeField] private TerrainType defaultTerrainType;
    [SerializeField] private List<TerrainType> terrainTypes;
    [SerializeField] private bool useRandomTerrain = true;

    // Visual representation settings - helps us see the grid in the scene
    [Header("Grid Visualization")]
    [SerializeField] private GameObject gridCubePrefab; // <-- New: Assign a cube prefab with collider

    // The actual 2D array that holds all our grid nodes - this is the heart of our pathfinding system
    public GridNode[,] GridNodes;

    // Public accessor for grid settings - lets other scripts get our configuration without changing it
    public GridSettings GridSettings => gridSettings;

    // These dictionaries keep track of which nodes are currently being used
    private Dictionary<GridNode, UnitController> occupiedNodes = new(); // Nodes with units standing on them
    private Dictionary<GridNode, UnitController> reservedNodes = new(); // Nodes that units are planning to move to
    private Dictionary<GridNode, TerrainType> originalTerrainTypes = new(); // Backup of original terrain before units occupy nodes

    /// <summary>
    /// Sets up the entire grid system - creates all nodes and assigns terrain types
    /// This is like building the foundation of our pathfinding system
    /// </summary>
    public void InitializeGrid()
    {
        // Create our 2D grid based on the configured size
        GridNodes = new GridNode[gridSettings.GridSizeX, gridSettings.GridSizeY];
        originalTerrainTypes.Clear();

        // Get only the terrain types that units can actually walk on
        List<TerrainType> walkableTerrains = terrainTypes.FindAll(t => t.IsWalkable);

        // Loop through every position in our grid and create a node
        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                // Convert grid coordinates to world position
                Vector3 worldPosition = gridSettings.UseXZPlane
                    ? new Vector3(x, 0, y) * gridSettings.NodeSize
                    : new Vector3(x, y, 0) * gridSettings.NodeSize;

                // Pick a terrain type - either random from walkable terrains or use the default
                TerrainType selectedTerrain = (useRandomTerrain && walkableTerrains.Count > 0)
                    ? walkableTerrains[Random.Range(0, walkableTerrains.Count)]
                    : defaultTerrainType;

                // Create the actual grid node with all its properties
                GridNode node = new GridNode
                {
                    Name = $"Cell_{x}_{y}",
                    WorldPosition = worldPosition,
                    TerrainTypes = selectedTerrain,
                    Walkable = selectedTerrain.IsWalkable
                };

                GridNodes[x, y] = node;
                originalTerrainTypes[node] = selectedTerrain;

                // Create a visual cube in the scene so we can see our grid
                if (gridCubePrefab != null)
                {
                    GameObject cube = Instantiate(gridCubePrefab, worldPosition, Quaternion.identity, transform);
                    cube.name = node.Name;

                    // Resize based on node size
                    //cube.transform.localScale = Vector3.one * gridSettings.NodeSize * 0.9f;

                    // Color the cube based on terrain properties - helps with debugging
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

        // Clear out any leftover occupation data from previous grid setups
        occupiedNodes.Clear();
        reservedNodes.Clear();
    }

    // === GRID ACCESS METHODS ===
    // These methods help other scripts interact with our grid safely

    // Get a specific node by its grid coordinates - returns null if out of bounds
    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
            return GridNodes[x, y];
        return null;
    }

    // Convert a world position back to grid coordinates - useful for finding which node a unit is standing on
    public Vector2Int GetGridPosFromWorld(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / gridSettings.NodeSize);
        int y = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPosition.z / gridSettings.NodeSize)
            : Mathf.RoundToInt(worldPosition.y / gridSettings.NodeSize);

        return new Vector2Int(x, y);
    }

    // === NODE STATUS CHECKING ===
    // Quick ways to check if nodes are available for movement

    // Check if a node has a unit currently standing on it
    public bool IsOccupied(GridNode node) => occupiedNodes.ContainsKey(node);

    // Check if a node is reserved by a unit that's planning to move there
    public bool IsReserved(GridNode node) => reservedNodes.ContainsKey(node);

    // Check if a node is blocked by a different unit - important for pathfinding
    public bool IsOccupiedOrReservedByOther(GridNode node, UnitController unit)
    {
        return (occupiedNodes.TryGetValue(node, out var occupier) && occupier != unit)
            || (reservedNodes.TryGetValue(node, out var reserver) && reserver != unit);
    }

    // === NODE OCCUPATION MANAGEMENT ===
    // These methods handle units claiming and releasing nodes

    // Mark a node as occupied by a specific unit
    public void MarkOccupied(GridNode node, UnitController unit)
    {
        if (node == null) return;

        // If this unit had reserved the node, clear the reservation since they're now occupying it
        if (reservedNodes.TryGetValue(node, out var reserver) && reserver == unit)
            reservedNodes.Remove(node);

        // Mark the node as occupied and temporarily remove terrain info
        if (!occupiedNodes.ContainsKey(node))
        {
            occupiedNodes[node] = unit;
            node.TerrainTypes = null;
        }
    }

    // Remove a unit from a node and restore the original terrain
    public void MarkUnoccupied(GridNode node, UnitController unit)
    {
        if (node == null) return;

        // Only let the unit that actually occupies the node mark it as unoccupied
        if (occupiedNodes.TryGetValue(node, out var occupier) && occupier == unit)
        {
            occupiedNodes.Remove(node);
            // Restore the original terrain type when the unit leaves
            if (originalTerrainTypes.TryGetValue(node, out var originalTerrain))
                node.TerrainTypes = originalTerrain;
        }
    }

    // === NODE RESERVATION SYSTEM ===
    // Prevents multiple units from trying to move to the same spot

    // Try to reserve a node for future movement - returns false if already taken
    public bool TryReserveNode(GridNode node, UnitController unit)
    {
        if (node == null || IsOccupiedOrReservedByOther(node, unit)) return false;

        reservedNodes[node] = unit;
        return true;
    }

    // Cancel a reservation when a unit changes its mind or reaches its destination
    public void ReleaseReservation(GridNode node, UnitController unit)
    {
        if (node != null && reservedNodes.TryGetValue(node, out var reserver) && reserver == unit)
            reservedNodes.Remove(node);
    }

    // === PATHFINDING HELPER METHODS ===
    // These methods support the A* pathfinding algorithm

    // Get all neighboring nodes that a unit can actually move to
    public List<GridNode> GetWalkableNeighbors(GridNode node, UnitController requestingUnit)
    {
        List<GridNode> neighbors = new();
        Vector2Int gridPosition = GetGridPosFromWorld(node.WorldPosition);

        // Only check cardinal directions (up, right, down, left) - no diagonal movement
        int[,] directions = new int[,] {
            { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }
        };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int nx = gridPosition.x + directions[i, 0];
            int ny = gridPosition.y + directions[i, 1];

            GridNode neighbor = GetNode(nx, ny);
            // Only include neighbors that exist, are walkable, and aren't blocked by other units
            if (neighbor != null && neighbor.Walkable && !IsOccupiedOrReservedByOther(neighbor, requestingUnit))
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    // Find available nodes around a center point - useful for spawning or repositioning units
    public List<GridNode> GetSurroundingAvailableNodes(GridNode centerNode, int layerDepth = 2)
    {
        List<GridNode> availableNodes = new();
        Vector2Int center = GetGridPosFromWorld(centerNode.WorldPosition);

        // Search in expanding rings around the center point
        for (int r = 1; r <= layerDepth; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    // Only check nodes on the outer edge of the current ring
                    if ((Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) || (dx == 0 && dy == 0)) continue;

                    int tx = center.x + dx;
                    int ty = center.y + dy;

                    GridNode node = GetNode(tx, ty);
                    if (node != null && node.Walkable && !IsOccupied(node) && !IsReserved(node))
                        availableNodes.Add(node);
                }
            }

            // Stop searching if we found any available nodes in this ring
            if (availableNodes.Count > 0) break;
        }

        return availableNodes;
    }

    // === COLLISION DETECTION AND RESOLUTION ===
    // These methods help resolve situations where units end up in the same space

    // Check if a unit is the only one in its current node
    private bool IsUnitAloneInNode(UnitController unit, GridNode node)
    {
        if (unit == null || node == null) return false;

        BoxCollider unitCol = unit.GetComponent<BoxCollider>();
        if (unitCol == null) return true;

        Vector3 center = unitCol.bounds.center;
        Vector3 halfExtents = unitCol.bounds.extents;

        // Check for any other units in the same physical space
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

    // Make sure a unit properly occupies the node it's standing on
    public void EnsureUnitOccupiesOwnNode(UnitController unit)
    {
        GridNode node = GetNodeFromWorld(unit.transform.position);
        if (node == null) return;

        if (occupiedNodes.TryGetValue(node, out var occupier))
        {
            // If another unit is occupying this node, try to move to a neighboring node
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
            // If the node is free and the unit is alone, claim it
            MarkOccupied(node, unit);
        }
    }

    // === HIGH-LEVEL MOVEMENT REQUESTS ===
    // Public interface for requesting unit movement

    // Request a unit to move to a specific node - handles reservation automatically
    public bool RequestMoveToNode(UnitController unit, GridNode targetNode)
    {
        if (TryReserveNode(targetNode, unit))
        {
            unit.RequestPath(targetNode.WorldPosition);
            return true;
        }

        return false;
    }

    // Cancel all reservations for a specific unit - useful when a unit stops or changes plans
    public void CancelReservation(UnitController unit)
    {
        List<GridNode> toRemove = new();
        foreach (var kvp in reservedNodes)
            if (kvp.Value == unit) toRemove.Add(kvp.Key);

        foreach (var node in toRemove)
            reservedNodes.Remove(node);
    }

    // Check for and resolve any collision issues for a unit
    public void CheckAndResolveCollisions(UnitController unit)
    {
        // Look for nearby units that might be colliding
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

    // === UTILITY METHODS ===
    // Helpful conversion and neighbor-finding methods

    // Convert world position to the corresponding grid node
    public GridNode GetNodeFromWorld(Vector3 worldPosition)
    {
        Vector2Int coords = GetGridPosFromWorld(worldPosition);
        return GetNode(coords.x, coords.y);
    }

    // Get all neighboring nodes (including non-walkable ones) - used by pathfinding algorithms
    public List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();

        if (node == null) return neighbors;

        Vector2Int gridPos = GetGridPosFromWorld(node.WorldPosition);

        // Directions including only cardinal (up, right, down, left)
        int[,] directions = new int[,] {
        { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }
    };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int nx = gridPos.x + directions[i, 0];
            int ny = gridPos.y + directions[i, 1];

            GridNode neighbor = GetNode(nx, ny);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    // === VISUAL DEBUG METHODS ===
    // Gizmo drawing for debugging the grid in the Unity editor

    private void OnDrawGizmos()
    {
        if (GridNodes == null || gridSettings == null) return;

        // Draw each node as a colored wireframe cube
        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                GridNode node = GridNodes[x, y];
                if (node == null) continue;

                Color color;

                // Color-code nodes based on their status
                if (!node.Walkable) color = Color.red; // Red for unwalkable
                else if (IsOccupied(node)) color = Color.blue; // Blue for occupied
                else if (IsReserved(node)) color = Color.cyan; // Cyan for reserved
                else
                {
                    // Use terrain color with alpha based on movement cost
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