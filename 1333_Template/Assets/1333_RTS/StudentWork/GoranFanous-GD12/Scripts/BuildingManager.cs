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
            Vector3 clickPosition = hit.point;
            Vector2Int gridPos = gridManager.GetGridPosFromWorld(clickPosition);

            // Get building size from prefab
            BuildingType buildingType = buildingPrefab.GetComponent<BuildingType>();
            if (buildingType == null || buildingType.buildingSettings == null)
            {
                Debug.LogError("Building prefab missing BuildingType or BuildingSettings component.");
                return;
            }

            int buildWidth = buildingType.buildingSettings.BuildSizeX;
            int buildHeight = buildingType.buildingSettings.BuildSizeY;
            float nodeSize = gridManager.GridSettings.NodeSize;

            // Check if building fits at clicked grid position
            if (!CanPlaceBuildingAt(gridPos.x, gridPos.y, buildWidth, buildHeight))
            {
                Debug.Log("Cannot place building here, space is blocked or out of bounds.");
                return;
            }

            // Calculate world position centered on the building footprint
            Vector3 worldPosition = CalculateWorldPosition(gridPos, buildWidth, buildHeight, nodeSize);

            // Instantiate building prefab at calculated world position
            GameObject building = Instantiate(buildingPrefab, worldPosition, Quaternion.identity);
            building.transform.localScale = Vector3.one * buildingType.buildingSettings.BuildScale;

            // Mark grid nodes as unwalkable
            for (int dx = 0; dx < buildWidth; dx++)
            {
                for (int dy = 0; dy < buildHeight; dy++)
                {
                    Vector2Int nodePos = new Vector2Int(gridPos.x + dx, gridPos.y + dy);
                    GridNode node = gridManager.GetNode(nodePos.x, nodePos.y);
                    if (node != null)
                    {
                        node.Walkable = false;
                    }
                }
            }
        }
    }

    private bool CanPlaceBuildingAt(int startX, int startY, int sizeX, int sizeY)
    {
        for (int dx = 0; dx < sizeX; dx++)
        {
            for (int dy = 0; dy < sizeY; dy++)
            {
                GridNode node = gridManager.GetNode(startX + dx, startY + dy);
                if (node == null || !node.Walkable)
                    return false;
            }
        }
        return true;
    }

    private Vector3 CalculateWorldPosition(Vector2Int origin, int sizeX, int sizeY, float nodeSize)
    {
        float offsetX = (sizeX - 1) * 0.5f * nodeSize;
        float offsetY = (sizeY - 1) * 0.5f * nodeSize;

        Vector3 basePosition = gridManager.GridSettings.UseXZPlane
            ? new Vector3(origin.x, 0f, origin.y) * nodeSize
            : new Vector3(origin.x, origin.y, 0f) * nodeSize;

        return gridManager.GridSettings.UseXZPlane
            ? basePosition + new Vector3(offsetX, 0f, offsetY)
            : basePosition + new Vector3(offsetX, offsetY, 0f);
    }
}