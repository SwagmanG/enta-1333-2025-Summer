using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the grid used for A* pathfinding, node occupation, reservation,
/// terrain assignment, and collision resolution for units.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid Configuration")]
    // Settings describing grid size, node size, and plane orientation
    [SerializeField] private GridSettings gridSettings;
    // Default terrain type to assign if random terrain is disabled or unavailable
    [SerializeField] private TerrainType defaultTerrainType;
    // List of possible terrain types to randomly assign
    [SerializeField] private List<TerrainType> terrainTypes;
    // Flag indicating whether to assign terrain randomly on grid initialization
    [SerializeField] private bool useRandomTerrain = true;

    // 2D array holding all nodes in the grid
    public GridNode[,] gridNodes;
    // Property exposing grid settings publicly
    public GridSettings GridSettings => gridSettings;

    // Dictionary tracking which nodes are occupied by which units
    private Dictionary<GridNode, UnitController> occupiedNodes = new();
    // Dictionary tracking nodes reserved by units for movement planning
    private Dictionary<GridNode, UnitController> reservedNodes = new();
    // Dictionary to keep original terrain types to restore when unoccupied
    private Dictionary<GridNode, TerrainType> originalTerrainTypes = new();

    /// <summary>
    /// Initializes the grid nodes and assigns terrain types.
    /// </summary>
    public void InitializeGrid()
    {
        gridNodes = new GridNode[gridSettings.GridSizeX, gridSettings.GridSizeY];
        originalTerrainTypes.Clear();

        // Filter only walkable terrain types for random assignment
        List<TerrainType> walkableTerrains = terrainTypes.FindAll(t => t.IsWalkable);

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                // Calculate world position of the node based on grid layout
                Vector3 worldPosition = gridSettings.UseXZPlane
                    ? new Vector3(x, 0, y) * gridSettings.NodeSize
                    : new Vector3(x, y, 0) * gridSettings.NodeSize;

                // Select terrain either randomly from walkable types or use default
                TerrainType selectedTerrain = (useRandomTerrain && walkableTerrains.Count > 0)
                    ? walkableTerrains[Random.Range(0, walkableTerrains.Count)]
                    : defaultTerrainType;

                // Create and initialize new grid node with name, position, terrain and walkability
                GridNode node = new GridNode
                {
                    Name = $"Cell_{x}_{y}",
                    WorldPosition = worldPosition,
                    TerrainTypes = selectedTerrain,
                    Walkable = selectedTerrain.IsWalkable
                };

                // Assign node to grid and store original terrain for restoration
                gridNodes[x, y] = node;
                originalTerrainTypes[node] = selectedTerrain;
            }
        }

        // Clear any existing occupation or reservation data
        occupiedNodes.Clear();
        reservedNodes.Clear();
    }

    /// <summary>
    /// Returns the grid node at the given coordinates, or null if out of bounds.
    /// </summary>
    /// <param name="x">Grid X coordinate.</param>
    /// <param name="y">Grid Y coordinate.</param>
    /// <returns>The node at (x,y) or null if invalid position.</returns>
    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
            return gridNodes[x, y];
        return null;
    }

    /// <summary>
    /// Converts a world position into grid coordinates.
    /// </summary>
    /// <param name="worldPosition">World position in game space.</param>
    /// <returns>Corresponding grid coordinates as Vector2Int.</returns>
    public Vector2Int GetGridPosFromWorld(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / gridSettings.NodeSize);
        int y = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPosition.z / gridSettings.NodeSize)
            : Mathf.RoundToInt(worldPosition.y / gridSettings.NodeSize);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Checks if the given node is currently occupied by a unit.
    /// </summary>
    public bool IsOccupied(GridNode node) => occupiedNodes.ContainsKey(node);

    /// <summary>
    /// Checks if the given node is currently reserved by a unit.
    /// </summary>
    public bool IsReserved(GridNode node) => reservedNodes.ContainsKey(node);

    /// <summary>
    /// Checks if the node is occupied or reserved by any unit other than the specified one.
    /// </summary>
    /// <param name="node">Grid node to check.</param>
    /// <param name="unit">Unit to exclude from check.</param>
    /// <returns>True if node is occupied or reserved by another unit.</returns>
    public bool IsOccupiedOrReservedByOther(GridNode node, UnitController unit)
    {
        return (occupiedNodes.TryGetValue(node, out var occupier) && occupier != unit)
            || (reservedNodes.TryGetValue(node, out var reserver) && reserver != unit);
    }

    /// <summary>
    /// Marks a node as occupied by a given unit, removing any reservation by that unit.
    /// </summary>
    public void MarkOccupied(GridNode node, UnitController unit)
    {
        if (node == null) return;

        if (reservedNodes.TryGetValue(node, out var reserver) && reserver == unit)
            reservedNodes.Remove(node);

        if (!occupiedNodes.ContainsKey(node))
        {
            occupiedNodes[node] = unit;
            node.TerrainTypes = null; // Optionally override terrain for visuals
        }
    }

    /// <summary>
    /// Marks a node as unoccupied by a given unit and restores original terrain.
    /// </summary>
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

    /// <summary>
    /// Attempts to reserve a node for a unit, returns false if already occupied or reserved by others.
    /// </summary>
    public bool TryReserveNode(GridNode node, UnitController unit)
    {
        if (node == null || IsOccupiedOrReservedByOther(node, unit)) return false;

        reservedNodes[node] = unit;
        return true;
    }

    /// <summary>
    /// Releases a reservation on a node held by a unit.
    /// </summary>
    public void ReleaseReservation(GridNode node, UnitController unit)
    {
        if (node != null && reservedNodes.TryGetValue(node, out var reserver) && reserver == unit)
            reservedNodes.Remove(node);
    }

    /// <summary>
    /// Returns a list of walkable, unoccupied neighbors (up, down, left, right) for pathfinding.
    /// </summary>
    /// <param name="node">Center node.</param>
    /// <param name="requestingUnit">Unit requesting neighbors to exclude reservations/occupations by others.</param>
    public List<GridNode> GetWalkableNeighbors(GridNode node, UnitController requestingUnit)
    {
        List<GridNode> neighbors = new();

        // Get current grid coordinates of the node
        Vector2Int gridPosition = GetGridPosFromWorld(node.WorldPosition);

        // Directions to check: Up, Right, Down, Left (no diagonals)
        int[,] directions = new int[,] {
            { 0, 1 },   // Up
            { 1, 0 },   // Right
            { 0, -1 },  // Down
            { -1, 0 }   // Left
        };

        // Loop through each direction and check neighbor nodes
        for (int directionIndex = 0; directionIndex < directions.GetLength(0); directionIndex++)
        {
            int neighborX = gridPosition.x + directions[directionIndex, 0];
            int neighborY = gridPosition.y + directions[directionIndex, 1];

            GridNode neighborNode = GetNode(neighborX, neighborY);
            if (neighborNode != null && neighborNode.Walkable && !IsOccupiedOrReservedByOther(neighborNode, requestingUnit))
                neighbors.Add(neighborNode);
        }

        return neighbors;
    }

    /// <summary>
    /// Searches outward in layers around a center node to find available walkable and unoccupied nodes.
    /// </summary>
    /// <param name="centerNode">Node around which to search.</param>
    /// <param name="layerDepth">Maximum number of layers to search outward.</param>
    public List<GridNode> GetSurroundingAvailableNodes(GridNode centerNode, int layerDepth = 2)
    {
        List<GridNode> availableNodes = new();
        Vector2Int centerPosition = GetGridPosFromWorld(centerNode.WorldPosition);

        // Iterate through layers expanding outward from center node
        for (int radius = 1; radius <= layerDepth; radius++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    // Only consider nodes exactly on the current radius ring (edges of square)
                    bool isOnRingEdge = Mathf.Abs(offsetX) == radius || Mathf.Abs(offsetY) == radius;

                    if (!isOnRingEdge || (offsetX == 0 && offsetY == 0))
                        continue;

                    int targetX = centerPosition.x + offsetX;
                    int targetY = centerPosition.y + offsetY;

                    GridNode candidateNode = GetNode(targetX, targetY);
                    if (candidateNode != null && candidateNode.Walkable && !IsOccupied(candidateNode) && !IsReserved(candidateNode))
                        availableNodes.Add(candidateNode);
                }
            }

            if (availableNodes.Count > 0)
                break; // Stop if we found any available nodes at this radius
        }

        return availableNodes;
    }

    /// <summary>
    /// Checks if the given unit is physically alone inside the specified grid node using collider overlap.
    /// </summary>
    private bool IsUnitAloneInNode(UnitController unit, GridNode node)
    {
        if (unit == null || node == null) return false;

        BoxCollider unitCollider = unit.GetComponent<BoxCollider>();
        if (unitCollider == null)
        {
            Debug.LogWarning("Unit missing BoxCollider component.");
            return true; // Assume alone if no collider
        }

        Vector3 colliderCenter = unitCollider.bounds.center;
        Vector3 colliderHalfExtents = unitCollider.bounds.extents;

        // Check all colliders overlapping with the unit's collider
        Collider[] overlappingColliders = Physics.OverlapBox(colliderCenter, colliderHalfExtents, unit.transform.rotation);
        foreach (Collider collider in overlappingColliders)
        {
            if (collider.gameObject == unit.gameObject)
                continue; // Ignore self

            if (collider.TryGetComponent<UnitController>(out UnitController otherUnit))
            {
                GridNode otherUnitNode = GetNodeFromWorld(otherUnit.transform.position);
                if (otherUnitNode == node)
                    return false; // Found another unit in the same node
            }
        }

        return true; // No other units in the node
    }

    /// <summary>
    /// Ensures that the given unit occupies a node that is physically and logically its own.
    /// If the current node is occupied by another unit, attempts to move the unit to a free neighbor.
    /// </summary>
    public void EnsureUnitOccupiesOwnNode(UnitController unit)
    {
        GridNode currentNode = GetNodeFromWorld(unit.transform.position);
        if (currentNode == null) return;

        if (occupiedNodes.TryGetValue(currentNode, out var occupier))
        {
            // Node occupied by another unit, attempt to find free neighbor node
            if (occupier != unit)
            {
                List<GridNode> neighbors = GetWalkableNeighbors(currentNode, unit);
                if (neighbors.Count > 0)
                {
                    GridNode targetNode = neighbors[Random.Range(0, neighbors.Count)];
                    if (TryReserveNode(targetNode, unit))
                    {
                        unit.RequestPath(targetNode.WorldPosition);
                    }
                }
            }
        }
        else if (IsUnitAloneInNode(unit, currentNode))
        {
            // Occupy the current node if alone
            MarkOccupied(currentNode, unit);
        }
    }

    /// <summary>
    /// Attempts to reserve the target node and request a path for the unit.
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
    /// Cancels all reservations made by the given unit.
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
    /// Checks for collisions with other units and attempts to resolve by reoccupying correct nodes.
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

    /// <summary>
    /// Gets the grid node corresponding to the given world position.
    /// </summary>
    public GridNode GetNodeFromWorld(Vector3 worldPosition)
    {
        Vector2Int gridCoordinates = GetGridPosFromWorld(worldPosition);
        return GetNode(gridCoordinates.x, gridCoordinates.y);
    }

    /// <summary>
    /// Draws the grid nodes in the editor using Gizmos, showing walkability, occupation, reservation, and terrain cost.
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
                    color = Color.red; // Impassable nodes in red
                else if (IsOccupied(node))
                    color = Color.blue; // Occupied nodes in blue
                else if (IsReserved(node))
                    color = Color.cyan; // Reserved nodes in cyan
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
