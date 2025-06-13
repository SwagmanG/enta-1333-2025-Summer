using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class GridManager : MonoBehaviour
{

    [SerializeField] private GridSettings gridSettings;

    [SerializeField] private TerrainType defaultTerrainType;

    [SerializeField] private List<TerrainType> terrainTypes;

    // Toggle to enable or disable random terrain assignment
    [SerializeField] private bool useRandomTerrain = true;

    public GridNode[,] gridNodes;

    public GridSettings GridSettings => gridSettings;

    private GridNode[,] gridNode;

    [Header("Debug for editor playmode only")]
    [SerializeField] private List<GridNode> AllNodes = new();

    public bool isInitialized { get; private set; } = false;
    public void InitializeGrid()
    {
        gridNodes = new GridNode[gridSettings.GridSizeX, gridSettings.GridSizeY];

        // Filter out non-walkable (danger) terrain types if randomization is on
        List<TerrainType> safeTerrainTypes = terrainTypes.FindAll(t => t.IsWalkable);

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                Vector3 worldPos = gridSettings.UseXZPlane
                    ? new Vector3(x, 0, y) * gridSettings.NodeSize
                    : new Vector3(x, y, 0) * gridSettings.NodeSize;

                TerrainType chosenTerrain;

                if (useRandomTerrain && safeTerrainTypes.Count > 0)
                {
                    chosenTerrain = safeTerrainTypes[Random.Range(0, safeTerrainTypes.Count)];
                }
                else
                {
                    chosenTerrain = defaultTerrainType;
                }

                GridNode node = new GridNode
                {
                    Name = $"Cell_",
                    WorldPosition = worldPos,
                    TerrainTypes = chosenTerrain,
                    Walkable = chosenTerrain.IsWalkable,
                };

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
