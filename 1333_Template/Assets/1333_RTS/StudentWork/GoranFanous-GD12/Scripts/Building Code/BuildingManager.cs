using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;  // Reference to the grid system managing nodes

    [Header("Building Prefabs")]
    [Tooltip("List of building prefabs to cycle through")]
    [SerializeField] private List<GameObject> buildingPrefabs;

    // Index of the currently selected building prefab from the list
    private int currentBuildingIndex = 0;

    // Cached reference to the main camera for raycasting mouse clicks
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Only process building placement input if in build mode
        if (!BuildModeController.Instance.IsInBuildMode)
            return;

        HandleBuildingCycleInput();

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuildingAtMouseClick();
        }
    }

    /// <summary>
    /// Handles cycling through building prefabs with Q and E keys.
    /// </summary>
    private void HandleBuildingCycleInput()
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentBuildingIndex++;
            if (currentBuildingIndex >= buildingPrefabs.Count)
                currentBuildingIndex = 0; // Wrap around to first prefab
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            currentBuildingIndex--;
            if (currentBuildingIndex < 0)
                currentBuildingIndex = buildingPrefabs.Count - 1; // Wrap around to last prefab
        }
    }

    /// <summary>
    /// Attempts to place the selected building prefab at the mouse click position on the grid.
    /// </summary>
    private void TryPlaceBuildingAtMouseClick()
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0 || gridManager == null)
            return;

        GameObject buildingPrefab = buildingPrefabs[currentBuildingIndex];
        if (buildingPrefab == null)
            return;

        // Raycast from the mouse position into the world to find a placement position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
        {
            Vector3 clickedWorldPosition = hitInfo.point;

            // Convert the clicked world position to grid coordinates
            Vector2Int clickedGridCoordinates = gridManager.GetGridPosFromWorld(clickedWorldPosition);

            BuildingType buildingTypeComponent = buildingPrefab.GetComponent<BuildingType>();
            if (buildingTypeComponent == null || buildingTypeComponent.buildingSettings == null)
            {
                Debug.LogError("Building prefab missing BuildingType or BuildingSettings component.");
                return;
            }

            // Get the size of the building in grid nodes
            int buildingWidth = buildingTypeComponent.buildingSettings.BuildingSizeX;
            int buildingHeight = buildingTypeComponent.buildingSettings.BuildingSizeY;

            // Size of each grid node in world units
            float gridNodeSize = gridManager.GridSettings.NodeSize;

            // Check if the building can be placed at the target location (not blocked or out of bounds)
            if (!CanPlaceBuildingAt(clickedGridCoordinates.x, clickedGridCoordinates.y, buildingWidth, buildingHeight))
            {
                Debug.Log("Cannot place building here, space is blocked or out of bounds.");
                return;
            }

            // Calculate the exact world position to place the building, centered on the grid area it occupies
            Vector3 placementWorldPosition = CalculateWorldPosition(clickedGridCoordinates, buildingWidth, buildingHeight, gridNodeSize);

            // Instantiate the building prefab at the calculated position with no rotation
            GameObject newBuilding = Instantiate(buildingPrefab, placementWorldPosition, Quaternion.identity);

            // Scale the building according to its settings
            newBuilding.transform.localScale = Vector3.one * buildingTypeComponent.buildingSettings.BuildScale;

            // Mark all grid nodes occupied by this building as unwalkable (blocked for pathfinding)
            for (int offsetX = 0; offsetX < buildingWidth; offsetX++)
            {
                for (int offsetY = 0; offsetY < buildingHeight; offsetY++)
                {
                    int currentNodeX = clickedGridCoordinates.x + offsetX;
                    int currentNodeY = clickedGridCoordinates.y + offsetY;

                    GridNode node = gridManager.GetNode(currentNodeX, currentNodeY);
                    if (node != null)
                    {
                        node.Walkable = false;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if the building can be placed at the specified grid origin, given its width and height.
    /// Returns true if all nodes are walkable and exist.
    /// </summary>
    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        for (int offsetX = 0; offsetX < width; offsetX++)
        {
            for (int offsetY = 0; offsetY < height; offsetY++)
            {
                int checkNodeX = startX + offsetX;
                int checkNodeY = startY + offsetY;

                GridNode node = gridManager.GetNode(checkNodeX, checkNodeY);
                if (node == null || !node.Walkable)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Calculates the world position for the building placement based on grid origin and building size.
    /// Takes into account whether the grid uses the XZ plane or XY plane.
    /// </summary>
    private Vector3 CalculateWorldPosition(Vector2Int gridOrigin, int buildingWidth, int buildingHeight, float nodeSize)
    {
        float halfWidthOffset = (buildingWidth - 1) * 0.5f * nodeSize;
        float halfHeightOffset = (buildingHeight - 1) * 0.5f * nodeSize;

        Vector3 basePosition = gridManager.GridSettings.UseXZPlane
            ? new Vector3(gridOrigin.x, 0f, gridOrigin.y) * nodeSize
            : new Vector3(gridOrigin.x, gridOrigin.y, 0f) * nodeSize;

        return gridManager.GridSettings.UseXZPlane
            ? basePosition + new Vector3(halfWidthOffset, 0f, halfHeightOffset)
            : basePosition + new Vector3(halfWidthOffset, halfHeightOffset, 0f);
    }

    /// <summary>
    /// Displays the name of the currently selected building on the screen during build mode.
    /// </summary>
    private void OnGUI()
    {
        if (!BuildModeController.Instance.IsInBuildMode)
            return;

        if (buildingPrefabs == null || buildingPrefabs.Count == 0)
            return;

        GameObject currentPrefab = buildingPrefabs[currentBuildingIndex];
        if (currentPrefab == null)
            return;

        BuildingType buildingTypeComponent = currentPrefab.GetComponent<BuildingType>();
        string buildingName = buildingTypeComponent != null && buildingTypeComponent.buildingSettings != null
            ? buildingTypeComponent.buildingSettings.BuildingName
            : "Unknown";

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            normal = { textColor = Color.white },
            alignment = TextAnchor.UpperCenter
        };

        float labelWidth = 300f;
        float labelHeight = 30f;

        Rect labelRect = new Rect((Screen.width - labelWidth) / 2, 10, labelWidth, labelHeight);
        GUI.Label(labelRect, $"Placing Building: {buildingName}", style);
    }
}
