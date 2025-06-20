using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;      // Reference to the grid system
    [SerializeField] private GameObject buildingPrefab;    // The building prefab to place

    private Camera mainCamera; // Main scene camera used for raycasting mouse clicks

    private void Start()
    {
        // Cache the main camera for raycasting
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Only respond to input when in build mode
        if (!BuildModeController.Instance.IsInBuildMode)
            return;

        // On left-click, attempt to place the building at the clicked location
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuildingAtMouseClick();
        }
    }

    /// <summary>
    /// Tries to place the building prefab at the mouse click position if the placement is valid.
    /// </summary>
    private void TryPlaceBuildingAtMouseClick()
    {
        // Ensure references are assigned
        if (buildingPrefab == null || gridManager == null)
            return;

        // Raycast from camera to the mouse position into the 3D world
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Get the world position where the ray hit
            Vector3 clickWorldPosition = hit.point;

            // Convert world position to grid coordinates
            Vector2Int clickedGridCoords = gridManager.GetGridPosFromWorld(clickWorldPosition);

            // Get size and settings from the building prefab
            BuildingType buildingType = buildingPrefab.GetComponent<BuildingType>();
            if (buildingType == null || buildingType.buildingSettings == null)
            {
                Debug.LogError("Building prefab missing BuildingType or BuildingSettings component.");
                return;
            }

            int buildingWidth = buildingType.buildingSettings.BuildSizeX;
            int buildingHeight = buildingType.buildingSettings.BuildSizeY;
            float nodeSize = gridManager.GridSettings.NodeSize;

            // Check if building can be placed at the clicked grid position
            if (!CanPlaceBuildingAt(clickedGridCoords.x, clickedGridCoords.y, buildingWidth, buildingHeight))
            {
                Debug.Log("Cannot place building here, space is blocked or out of bounds.");
                return;
            }

            // Convert grid position to centered world position for placing the building visually
            Vector3 placementWorldPosition = CalculateWorldPosition( clickedGridCoords, buildingWidth, buildingHeight, nodeSize );

            // Instantiate the building at the calculated position
            GameObject newBuilding = Instantiate(buildingPrefab, placementWorldPosition, Quaternion.identity);
            newBuilding.transform.localScale = Vector3.one * buildingType.buildingSettings.BuildScale;

            // Mark all grid nodes under the building as unwalkable to block pathfinding
            for (int xOffset = 0; xOffset < buildingWidth; xOffset++)
            {
                for (int yOffset = 0; yOffset < buildingHeight; yOffset++)
                {
                    int nodeX = clickedGridCoords.x + xOffset;
                    int nodeY = clickedGridCoords.y + yOffset;

                    GridNode node = gridManager.GetNode(nodeX, nodeY);
                    if (node != null)
                    {
                        node.Walkable = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks whether a building can be placed at the specified grid position,
    /// ensuring all tiles within the building's area are walkable and in bounds.
    /// </summary>
    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        for (int xOffset = 0; xOffset < width; xOffset++)
        {
            for (int yOffset = 0; yOffset < height; yOffset++)
            {
                int checkX = startX + xOffset;
                int checkY = startY + yOffset;

                GridNode node = gridManager.GetNode(checkX, checkY);
                if (node == null || !node.Walkable)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Calculates the centered world position of a building given its top-left grid origin,
    /// dimensions, and node size. This ensures the prefab sits properly aligned in the world.
    /// </summary>
    private Vector3 CalculateWorldPosition(Vector2Int origin, int width, int height, float nodeSize)
    {
        // Calculate how much to offset the prefab from the origin to center it
        float halfWidthOffset = (width - 1) * 0.5f * nodeSize;
        float halfHeightOffset = (height - 1) * 0.5f * nodeSize;

        // Get base world position of the top-left grid cell
        Vector3 basePosition = gridManager.GridSettings.UseXZPlane
            ? new Vector3(origin.x, 0f, origin.y) * nodeSize
            : new Vector3(origin.x, origin.y, 0f) * nodeSize;

        // Add offset to center the building over the occupied grid cells
        return gridManager.GridSettings.UseXZPlane
            ? basePosition + new Vector3(halfWidthOffset, 0f, halfHeightOffset)
            : basePosition + new Vector3(halfWidthOffset, halfHeightOffset, 0f);
    }
}