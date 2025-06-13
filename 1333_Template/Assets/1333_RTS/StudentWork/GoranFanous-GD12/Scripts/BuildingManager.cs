using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject buildingPrefab;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!BuildModeController.Instance.IsInBuildMode)
            return;

        if (Input.GetMouseButtonDown(0)) // Left click
        {
            TryPlaceBuildingAtMouseClick();
        }
    }

    private void TryPlaceBuildingAtMouseClick()
    {
        if (buildingPrefab == null || gridManager == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 clickWorldPosition = hit.point;
            Vector2Int clickedGridCoords = gridManager.GetGridPosFromWorld(clickWorldPosition);

            BuildingType buildingType = buildingPrefab.GetComponent<BuildingType>();
            if (buildingType == null || buildingType.buildingSettings == null)
            {
                Debug.LogError("Building prefab missing BuildingType or BuildingSettings component.");
                return;
            }

            int buildingWidth = buildingType.buildingSettings.BuildSizeX;
            int buildingHeight = buildingType.buildingSettings.BuildSizeY;
            float nodeSize = gridManager.GridSettings.NodeSize;

            // Validate placement
            if (!CanPlaceBuildingAt(clickedGridCoords.x, clickedGridCoords.y, buildingWidth, buildingHeight))
            {
                Debug.Log("Cannot place building here, space is blocked or out of bounds.");
                return;
            }

            Vector3 placementWorldPosition = CalculateWorldPosition(
                clickedGridCoords, buildingWidth, buildingHeight, nodeSize
            );

            GameObject newBuilding = Instantiate(buildingPrefab, placementWorldPosition, Quaternion.identity);
            newBuilding.transform.localScale = Vector3.one * buildingType.buildingSettings.BuildScale;

            // Mark occupied nodes as unwalkable
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

    private Vector3 CalculateWorldPosition(Vector2Int origin, int width, int height, float nodeSize)
    {
        float halfWidthOffset = (width - 1) * 0.5f * nodeSize;
        float halfHeightOffset = (height - 1) * 0.5f * nodeSize;

        Vector3 basePosition = gridManager.GridSettings.UseXZPlane
            ? new Vector3(origin.x, 0f, origin.y) * nodeSize
            : new Vector3(origin.x, origin.y, 0f) * nodeSize;

        return gridManager.GridSettings.UseXZPlane
            ? basePosition + new Vector3(halfWidthOffset, 0f, halfHeightOffset)
            : basePosition + new Vector3(halfWidthOffset, halfHeightOffset, 0f);
    }
}