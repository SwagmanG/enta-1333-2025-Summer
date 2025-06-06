using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // Reference to the grid configuration data
    [SerializeField] private GridSettings gridSettings;

    // Default terrain type to use for nodes (can be used if random generation is disabled)
    [SerializeField] private TerrainType defaultTerrainType;

    // List of terrain types used for randomly assigning to nodes
    [SerializeField] private List<TerrainType> terrainTypes;

    // 2D array storing all grid nodes in the world
    public GridNode[,] gridNodes;

    // Expose GridSettings as a public property
    public GridSettings GridSettings => gridSettings;

    // Duplicate unused field – consider removing this if not used elsewhere
    private GridNode[,] gridNode;

    [Header("Debug for editor playmode only")]
    // List of all nodes used for debug or visual feedback in the editor
    [SerializeField] private List<GridNode> AllNodes = new();

    // Flag to determine whether the grid has been initialized
    public bool isInitialized { get; private set; } = false;

    // Initializes the grid structure and populates it with nodes
    public void InitializeGrid()
    {
        // Create the grid array with dimensions from settings
        gridNodes = new GridNode[gridSettings.GridSizeX, gridSettings.GridSizeY];

        // Loop through each grid position
        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                // Calculate the world position of the node based on the grid size and plane (XZ or XY)
                Vector3 worldPos = gridSettings.UseXZPlane
                    ? new Vector3(x, 0, y) * gridSettings.NodeSize
                    : new Vector3(x, y, 0) * gridSettings.NodeSize;

                // Pick a random terrain type for the node from the list
                TerrainType ChosenTerrain = terrainTypes[Random.Range(0, terrainTypes.Count)];

                // Create a new grid node and assign properties
                GridNode node = new GridNode
                {
                    Name = $"Cell_",
                    WorldPosition = worldPos,
                    TerrainTypes = ChosenTerrain,
                    Walkable = ChosenTerrain.IsWalkable,
                };

                // Store the node in the grid array
                gridNodes[x, y] = node;
            }
        }
    }

    // Safely returns a node at given x, y coordinates; returns null if out of bounds
    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
        {
            return gridNodes[x, y];
        }
        return null;
    }

    // Converts a world position to grid coordinates (x, y)
    public Vector2Int GetGridPosFromWorld(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridSettings.NodeSize);
        int y = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPos.z / gridSettings.NodeSize)
            : Mathf.RoundToInt(worldPos.y / gridSettings.NodeSize);

        return new Vector2Int(x, y);
    }

    // Draws visual representation of the grid in the Unity editor
    private void OnDrawGizmos()
    {
        // If grid hasn't been initialized, skip drawing
        if (gridNodes == null || gridSettings == null) return;

        // Loop through all grid cells
        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                GridNode node = gridNodes[x, y];
                if (node == null || node.TerrainTypes == null) continue;

                // Set terrain color based on terrain type
                Color terrainColor = node.TerrainTypes.GizmoColor;

                if (!node.Walkable)
                {
                    // Draw unwalkable tiles in red
                    Gizmos.color = Color.red;
                }
                else
                {
                    // Adjust transparency based on movement cost (lower cost = more visible)
                    float alpha = Mathf.InverseLerp(10f, 1f, node.TerrainTypes.MovementCost);
                    terrainColor.a = Mathf.Clamp01(alpha);
                    Gizmos.color = terrainColor;
                }

                // Draw a wire cube at the node's position to represent the tile
                Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * gridSettings.NodeSize * 0.9f);
            }
        }
    }
}
