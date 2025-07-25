using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("References")]
    public GridManager gridManager;

    [Header("Building Prefabs")]
    [SerializeField] private List<GameObject> buildingPrefabs;

    [Header("Preview Settings")]
    [SerializeField] private Material previewValidMaterial;
    [SerializeField] private Material previewInvalidMaterial;

    private int currentBuildingIndex = 0;
    private Camera mainCamera;
    private GameObject previewBuildingInstance;
    private int currentRotation = 0;

    private readonly List<PlacedBuildingInfo> placedBuildings = new();
    private GameObject placedCastleInstance = null;

    private int totalBarracksPlaced = 0;
    private int totalTowersPlaced = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!BuildModeController.Instance.IsInBuildMode)
        {
            DestroyPreview();
            return;
        }

        HandleBuildingCycleInput();
        HandleRotationInput();
        UpdatePreviewBuilding();

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
            currentBuildingIndex = (currentBuildingIndex + 1) % buildingPrefabs.Count;
            changed = true;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            currentBuildingIndex = (currentBuildingIndex - 1 + buildingPrefabs.Count) % buildingPrefabs.Count;
            changed = true;
        }

        if (changed)
        {
            AudioManager.Instance?.PlaySFX("Cycle Buildings");
            DestroyPreview();
        }
    }

    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation = (currentRotation + 90) % 360;
            if (previewBuildingInstance != null)
                previewBuildingInstance.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        }
    }

    private void UpdatePreviewBuilding()
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0 || gridManager == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
        {
            DestroyPreview();
            return;
        }

        Vector3 worldPos = hitInfo.point;
        Vector2Int gridPos = gridManager.GetGridPosFromWorld(worldPos);

        GameObject prefab = buildingPrefabs[currentBuildingIndex];
        if (prefab == null) return;

        BuildingType buildingType = prefab.GetComponent<BuildingType>();
        if (buildingType == null || buildingType.buildingSettings == null) return;

        string buildingName = buildingType.buildingSettings.BuildingName;

        if (buildingName == "Castle" && placedCastleInstance != null)
        {
            DestroyPreview();
            return;
        }

        if (buildingName == "Tower" && totalTowersPlaced >= totalBarracksPlaced * 6)
        {
            DestroyPreview();
            return;
        }

        int width = buildingType.buildingSettings.BuildingSizeX;
        int height = buildingType.buildingSettings.BuildingSizeY;
        float nodeSize = gridManager.GridSettings.NodeSize;

        bool rotated = currentRotation % 180 != 0;
        int finalWidth = rotated ? height : width;
        int finalHeight = rotated ? width : height;

        Vector3 previewWorldPos = CalculateWorldPosition(gridPos, finalWidth, finalHeight, nodeSize);
        bool canPlace = CanPlaceBuildingAt(gridPos.x, gridPos.y, finalWidth, finalHeight);

        if (previewBuildingInstance == null)
        {
            previewBuildingInstance = Instantiate(prefab, previewWorldPos, Quaternion.Euler(0, currentRotation, 0));
            previewBuildingInstance.transform.localScale = Vector3.one * buildingType.buildingSettings.BuildScale;
        }

        previewBuildingInstance.transform.position = previewWorldPos;
        previewBuildingInstance.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        SetPreviewMaterial(previewBuildingInstance, canPlace);
    }

    private void SetPreviewMaterial(GameObject building, bool canPlace)
    {
        Material targetMaterial = canPlace ? previewValidMaterial : previewInvalidMaterial;

        foreach (var renderer in building.GetComponentsInChildren<Renderer>())
        {
            renderer.material = targetMaterial;
        }
    }

    private void DestroyPreview()
    {
        if (previewBuildingInstance != null)
        {
            Destroy(previewBuildingInstance);
            previewBuildingInstance = null;
        }
    }

    private void TryPlaceBuildingAtMouseClick()
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0 || gridManager == null)
            return;

        GameObject prefab = buildingPrefabs[currentBuildingIndex];
        if (prefab == null) return;

        BuildingType buildingType = prefab.GetComponent<BuildingType>();
        if (buildingType == null || buildingType.buildingSettings == null) return;

        string buildingName = buildingType.buildingSettings.BuildingName;
        int populationCost = buildingType.buildingSettings.PopulationCost;

        if (ResourceManager.Instance.CurrentPopulation < populationCost)
        {
            Debug.LogWarning("Not enough population to place this building.");
            return;
        }

        if (buildingName == "Castle" && placedCastleInstance != null)
        {
            Debug.LogWarning("Only one Castle can exist at a time.");
            return;
        }

        if (buildingName == "Tower" && totalTowersPlaced >= totalBarracksPlaced * 6)
        {
            Debug.LogWarning("You need to place another Barracks to place more towers.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
            return;

        Vector3 worldPos = hitInfo.point;
        Vector2Int gridPos = gridManager.GetGridPosFromWorld(worldPos);

        int width = buildingType.buildingSettings.BuildingSizeX;
        int height = buildingType.buildingSettings.BuildingSizeY;
        float nodeSize = gridManager.GridSettings.NodeSize;

        bool rotated = currentRotation % 180 != 0;
        int finalWidth = rotated ? height : width;
        int finalHeight = rotated ? width : height;

        if (!CanPlaceBuildingAt(gridPos.x, gridPos.y, finalWidth, finalHeight))
        {
            Debug.Log("Cannot place building here.");
            return;
        }

        Vector3 placeWorldPos = CalculateWorldPosition(gridPos, finalWidth, finalHeight, nodeSize);
        GameObject newBuilding = Instantiate(prefab, placeWorldPos, Quaternion.Euler(0, currentRotation, 0));
        newBuilding.transform.localScale = Vector3.one * buildingType.buildingSettings.BuildScale;

        MarkGridNodesOccupied(gridPos.x, gridPos.y, finalWidth, finalHeight);

        placedBuildings.Add(new PlacedBuildingInfo
        {
            Building = newBuilding,
            BaseGridPosition = gridPos,
            Width = finalWidth,
            Height = finalHeight
        });

        if (buildingName == "Castle")
        {
            placedCastleInstance = newBuilding;
        }
        else if (buildingName == "Barracks")
        {
            totalBarracksPlaced++;
        }
        else if (buildingName == "Tower")
        {
            totalTowersPlaced++;
        }

        ResourceManager.Instance.SpendPopulation(populationCost);

        AudioManager.Instance?.PlaySFXAtPosition("Building Placed", placeWorldPos);
        DestroyPreview();
    }

    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = gridManager.GetNode(startX + x, startY + y);
                if (node == null || !node.Walkable)
                    return false;
            }
        }
        return true;
    }

    private void MarkGridNodesOccupied(int startX, int startY, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = gridManager.GetNode(startX + x, startY + y);
                if (node != null)
                {
                    node.Walkable = false;
                    gridManager.MarkOccupied(node, null);
                }
            }
        }
    }

    private void MarkGridNodesUnoccupied(int startX, int startY, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = gridManager.GetNode(startX + x, startY + y);
                if (node != null)
                {
                    node.Walkable = true;
                    gridManager.MarkUnoccupied(node, null);
                }
            }
        }
    }

    private Vector3 CalculateWorldPosition(Vector2Int gridOrigin, int width, int height, float nodeSize)
    {
        float offsetX = (width - 1) * 0.5f * nodeSize;
        float offsetY = (height - 1) * 0.5f * nodeSize;

        Vector3 basePos = gridManager.GridSettings.UseXZPlane
            ? new Vector3(gridOrigin.x, 0f, gridOrigin.y) * nodeSize
            : new Vector3(gridOrigin.x, gridOrigin.y, 0f) * nodeSize;

        return gridManager.GridSettings.UseXZPlane
            ? basePos + new Vector3(offsetX, 0.5f, offsetY)
            : basePos + new Vector3(offsetX, offsetY, 0.5f);
    }

    public void OnBuildingDestroyed(BuildingHealth buildingHealth)
    {
        if (buildingHealth == null)
        {
            Debug.LogWarning("BuildingManager: Tried to handle destroyed building but received null reference.");
            return;
        }

        GameObject destroyedGO = buildingHealth.gameObject;
        int index = placedBuildings.FindIndex(info => info.Building == destroyedGO);

        if (index >= 0)
        {
            PlacedBuildingInfo info = placedBuildings[index];
            MarkGridNodesUnoccupied(info.BaseGridPosition.x, info.BaseGridPosition.y, info.Width, info.Height);
            placedBuildings.RemoveAt(index);

            BuildingType bt = destroyedGO.GetComponent<BuildingType>();
            if (bt != null && bt.buildingSettings != null)
            {
                string name = bt.buildingSettings.BuildingName;
                if (name == "Castle")
                    placedCastleInstance = null;
                else if (name == "Barracks")
                    totalBarracksPlaced = Mathf.Max(0, totalBarracksPlaced - 1);
                else if (name == "Tower")
                    totalTowersPlaced = Mathf.Max(0, totalTowersPlaced - 1);
            }
        }
        else
        {
            Debug.LogWarning($"BuildingManager could not find matching building info for destroyed building {destroyedGO.name}");
        }
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

        BuildingType buildingType = currentPrefab.GetComponent<BuildingType>();
        string name = buildingType?.buildingSettings?.BuildingName ?? "Unknown";
        int populationCost = buildingType?.buildingSettings?.PopulationCost ?? 0;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            normal = { textColor = Color.white },
            alignment = TextAnchor.UpperCenter
        };

        float width = 400f;
        float height = 30f;

        Rect rect1 = new Rect((Screen.width - width) / 2, 10, width, height);
        Rect rect2 = new Rect((Screen.width - width) / 2, 45, width, height);

        GUI.Label(rect1, $"Placing: {name} (R to rotate)", style);
        GUI.Label(rect2, $"Population Cost: {populationCost}", style);
    }

    private class PlacedBuildingInfo
    {
        public GameObject Building;
        public Vector2Int BaseGridPosition;
        public int Width;
        public int Height;
    }
}
