using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Building Prefabs")]
    [Tooltip("List of building prefabs to cycle through")]
    [SerializeField] private List<GameObject> buildingPrefabs;

    private int currentBuildingIndex = 0;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!BuildModeController.Instance.IsInBuildMode)
            return;

        HandleBuildingCycleInput();

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuildingAtMouseClick();
        }
    }

    private void HandleBuildingCycleInput()
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0)
            return;

        bool changed = false;

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentBuildingIndex++;
            if (currentBuildingIndex >= buildingPrefabs.Count)
                currentBuildingIndex = 0;
            changed = true;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            currentBuildingIndex--;
            if (currentBuildingIndex < 0)
                currentBuildingIndex = buildingPrefabs.Count - 1;
            changed = true;
        }

        if (changed)
        {
            AudioManager.Instance?.PlaySFX("Cycle Buildings");
            Debug.LogWarning("Cycle sound");
        }
    }

    private void TryPlaceBuildingAtMouseClick()
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0 || gridManager == null)
            return;

        GameObject buildingPrefab = buildingPrefabs[currentBuildingIndex];
        if (buildingPrefab == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
        {
            Vector3 clickedWorldPosition = hitInfo.point;
            Vector2Int clickedGridCoordinates = gridManager.GetGridPosFromWorld(clickedWorldPosition);

            BuildingType buildingTypeComponent = buildingPrefab.GetComponent<BuildingType>();
            if (buildingTypeComponent == null || buildingTypeComponent.buildingSettings == null)
            {
                Debug.LogError("Building prefab missing BuildingType or BuildingSettings component.");
                return;
            }

            int buildingWidth = buildingTypeComponent.buildingSettings.BuildingSizeX;
            int buildingHeight = buildingTypeComponent.buildingSettings.BuildingSizeY;
            float gridNodeSize = gridManager.GridSettings.NodeSize;

            if (!CanPlaceBuildingAt(clickedGridCoordinates.x, clickedGridCoordinates.y, buildingWidth, buildingHeight))
            {
                Debug.Log("Cannot place building here, space is blocked or out of bounds.");
                return;
            }

            Vector3 placementWorldPosition = CalculateWorldPosition(clickedGridCoordinates, buildingWidth, buildingHeight, gridNodeSize);

            GameObject newBuilding = Instantiate(buildingPrefab, placementWorldPosition, Quaternion.identity);
            newBuilding.transform.localScale = Vector3.one * buildingTypeComponent.buildingSettings.BuildScale;

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

            //  Play 3D sound at building location
            AudioManager.Instance?.PlaySFXAtPosition("Building Placed", placementWorldPosition);
            Debug.LogWarning("Building sound");
        }
    }

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
