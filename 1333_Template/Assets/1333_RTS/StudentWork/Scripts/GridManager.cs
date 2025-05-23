using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridSettings gridSettings;

    [SerializeField] private TerrainType defaultTerrainType;

    [SerializeField] private List<TerrainType> terrainTypes;

    public GridNode[,] gridNodes;
    public GridSettings GridSettings => gridSettings;

    private GridNode[,] gridNode;
    [Header("Debug for editor playmode only")]
    [SerializeField] private List<GridNode> AllNodes = new();

    public bool isInitialized { get; private set; } = false;

    public void InitializeGrid()
    {
        gridNodes = new GridNode[gridSettings.GridSizeX, gridSettings.GridSizeY];
        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                Vector3 worldPos = gridSettings.UseXZPlane
                    ? new Vector3(x, 0, y) * gridSettings.NodeSize
                    : new Vector3(x, y, 0) * gridSettings.NodeSize;

                //Optional can comment out just handles random terrain types
                TerrainType ChosenTerrain = terrainTypes[Random.Range(0, terrainTypes.Count)];

                GridNode node = new GridNode
                {
                    Name = $"Cell_",
                    WorldPosition = worldPos,
                    TerrainTypes = ChosenTerrain,
                    Walkable = ChosenTerrain.IsWalkable,
                };
                gridNodes[x, y] = node;
            }
        }

    }

   public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
        {
            return gridNodes[x, y];
        }
        return null;
    }

   public Vector2Int GetGridPosFromWorld(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridSettings.NodeSize);
        int y = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPos.z / gridSettings.NodeSize)
            : Mathf.RoundToInt(worldPos.y / gridSettings.NodeSize);

        return new Vector2Int(x, y);
    }

    private void OnDrawGizmos()
    {
        if (gridNodes == null || gridSettings == null) return;

        

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                GridNode node = gridNodes[x, y];
                Gizmos.color = node.Walkable? node.TerrainTypes.GizmoColor : Color.red;
                Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * gridSettings.NodeSize * 0.9f);
            }

        }

    }
}
