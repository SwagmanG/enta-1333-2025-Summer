using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager BMInstance { get; private set; }

    [Header("References")]
    public GridManager gridManager;
    public GameUiManager guiManager;

    [Header("Building Prefabs")]
    [SerializeField] private List<GameObject> buildingPrefabs;

    [Header("Preview Settings")]
    [SerializeField] private Material previewValidMaterial;
    [SerializeField] private Material previewInvalidMaterial;

    private Camera mainCamera;
    private GameObject previewBuildingInstance;
    private int currentRotation = 0;

    private List<GameObject> filteredBuildingPrefabs = new();
    private int currentFilteredIndex = 0;

    private readonly List<PlacedBuildingInfo> placedBuildings = new();
    private GameObject placedCastleInstance = null;

    private int totalBarracksPlaced = 0;
    private int totalTowersPlaced = 0;

    private void Awake()
    {
        if (BMInstance != null && BMInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        BMInstance = this;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        UpdateFilteredBuildings();
        NotifyUIUpdate();
    }

    private void Update()
    {
        if (!BuildModeController.BMCInstance.IsInBuildMode)
        {
            DestroyPreview();
            return;
        }

        UpdateFilteredBuildings();
        HandleBuildingCycleInput();
        HandleRotationInput();
        UpdatePreviewBuilding();

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuildingAtMouseClick();
        }
    }


    private void UpdateFilteredBuildings()
    {
        filteredBuildingPrefabs = buildingPrefabs.Where(prefab =>
        {
            BuildingType buildingType = prefab.GetComponent<BuildingType>();
            if (buildingType == null || buildingType.buildingSettings == null) return false;

            string name = buildingType.buildingSettings.BuildingName;

            if (name == "Castle" && placedCastleInstance != null)
                return false;

            if (name == "Tower")
            {
                if (totalBarracksPlaced == 0) return false;
                if (totalTowersPlaced >= totalBarracksPlaced * 6) return false;
            }

            return true;

        }).ToList();

        if (filteredBuildingPrefabs.Count == 0)
        {
            currentFilteredIndex = 0;
        }
        else
        {
            currentFilteredIndex %= filteredBuildingPrefabs.Count;
        }
    }

    private void HandleBuildingCycleInput()
    {
        if (filteredBuildingPrefabs.Count == 0)
            return;

        bool cycled = false;

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentFilteredIndex = (currentFilteredIndex + 1) % filteredBuildingPrefabs.Count;
            cycled = true;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            currentFilteredIndex = (currentFilteredIndex - 1 + filteredBuildingPrefabs.Count) % filteredBuildingPrefabs.Count;
            cycled = true;
        }

        if (cycled)
        {
            AudioManager.Instance?.PlaySFX("Cycle Buildings");
            DestroyPreview();
            NotifyUIUpdate();
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
        if (filteredBuildingPrefabs.Count == 0 || gridManager == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
        {
            DestroyPreview();
            return;
        }

        Vector3 worldPos = hitInfo.point;
        Vector2Int gridPos = gridManager.GetGridPosFromWorld(worldPos);

        GameObject prefab = filteredBuildingPrefabs[currentFilteredIndex];
        if (prefab == null) return;

        BuildingType buildingType = prefab.GetComponent<BuildingType>();
        if (buildingType == null || buildingType.buildingSettings == null) return;

        int width = buildingType.buildingSettings.BuildingSizeX;
        int height = buildingType.buildingSettings.BuildingSizeY;
        float nodeSize = gridManager.GridSettings.NodeSize;

        bool rotated = currentRotation % 180 != 0;
        int finalWidth = rotated ? height : width;
        int finalHeight = rotated ? width : height;

        Vector3 previewWorldPos = CalculateWorldPosition(gridPos, finalWidth, finalHeight, nodeSize);

        bool canPlace = CanPlaceBuildingAt(gridPos.x, gridPos.y, finalWidth, finalHeight);

        int requiredPopulation = buildingType.buildingSettings.PopulationCost;
        int requiredGold = buildingType.buildingSettings.GoldCost;

        bool hasEnoughPopulation = ResourceManager.RMInstance.CurrentPopulation >= requiredPopulation;
        bool hasEnoughGold = ResourceManager.RMInstance.CurrentGold >= requiredGold;

        bool hasEnoughResources = hasEnoughPopulation && hasEnoughGold;
        bool showAsValid = canPlace && hasEnoughResources;

        if (previewBuildingInstance == null)
        {
            previewBuildingInstance = Instantiate(prefab, previewWorldPos, Quaternion.Euler(0, currentRotation, 0));
            previewBuildingInstance.transform.localScale = Vector3.one * buildingType.buildingSettings.BuildScale;
        }

        previewBuildingInstance.transform.position = previewWorldPos;
        previewBuildingInstance.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        SetPreviewMaterial(previewBuildingInstance, showAsValid);
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
        if (filteredBuildingPrefabs.Count == 0 || gridManager == null)
            return;

        GameObject prefab = filteredBuildingPrefabs[currentFilteredIndex];
        if (prefab == null) return;

        BuildingType buildingType = prefab.GetComponent<BuildingType>();
        if (buildingType == null || buildingType.buildingSettings == null) return;

        string buildingName = buildingType.buildingSettings.BuildingName;
        int populationCost = buildingType.buildingSettings.PopulationCost;
        int goldCost = buildingType.buildingSettings.GoldCost;

        var resourceManager = ResourceManager.RMInstance;

        if (resourceManager.CurrentPopulation < populationCost)
        {
            Debug.LogWarning($"Not enough population to place {buildingName}. Requires {populationCost}, but only {resourceManager.CurrentPopulation} available.");
            return;
        }

        if (resourceManager.CurrentGold < goldCost)
        {
            Debug.LogWarning($"Not enough gold to place {buildingName}. Requires {goldCost}, but only {resourceManager.CurrentGold} available.");
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

        resourceManager.SpendPopulation(populationCost);
        resourceManager.SpendGold(goldCost);

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

            CastleResourceManager castleRes = newBuilding.GetComponent<CastleResourceManager>();
            if (castleRes != null)
            {
                castleRes.ActivateResourceGeneration();
            }
        }
        else
        {
            StartCoroutine(ApplyFoodTaxCoroutine(buildingType.buildingSettings.FoodTax));

            if (placedCastleInstance != null)
            {
                CastleResourceManager castleRes = placedCastleInstance.GetComponent<CastleResourceManager>();
                if (castleRes != null)
                {
                    castleRes.AddToGoldModifier(buildingType.buildingSettings.GoldMod);
                    castleRes.AddToFoodModifier(buildingType.buildingSettings.FoodMod);
                    castleRes.AddToPopulationModifier(buildingType.buildingSettings.PopulationMod);
                    Debug.Log($"Added modifiers from {buildingName} to castle.");
                }
            }
        }

        if (buildingName == "Barracks") totalBarracksPlaced++;
        else if (buildingName == "Tower") totalTowersPlaced++;

        AudioManager.Instance?.PlaySFXAtPosition("Building Placed", placeWorldPos);
        DestroyPreview();
    }

    private IEnumerator ApplyFoodTaxCoroutine(int foodTax)
    {
        while (true)
        {
            yield return new WaitForSeconds(20f);
            ResourceManager.RMInstance?.SpendFood(foodTax);
        }
    }

    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        int checkStartX = startX;
        int checkStartY = startY;
        int checkEndX = startX + width - 1;
        int checkEndY = startY + height - 1;

        for (int x = checkStartX; x <= checkEndX; x++)
        {
            for (int y = checkStartY; y <= checkEndY; y++)
            {
                if (x >= startX && x < startX + width && y >= startY && y < startY + height)
                {
                    // core building footprint must be walkable
                    GridNode node = gridManager.GetNode(x, y);
                    if (node == null || !node.Walkable)
                        return false;
                }
                else
                {
                    // border tiles must NOT be occupied by a building (adjacent nodes)
                    GridNode node = gridManager.GetNode(x, y);
                    if (node != null && node.BuildingOccupied)
                        return false;
                }
            }
        }
        return true;
    }

    private void MarkGridNodesOccupied(int startX, int startY, int width, int height)
    {
        // Mark core footprint nodes non-walkable and not adjacent occupied
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = gridManager.GetNode(startX + x, startY + y);
                if (node != null)
                {
                    node.Walkable = false;
                    node.BuildingOccupied = true; // Occupied by building
                    gridManager.MarkOccupied(node, null);
                }
            }
        }

        // Mark adjacent border nodes as BuildingOccupied = true but keep walkable
        int borderStartX = startX - 1;
        int borderStartY = startY - 1;
        int borderEndX = startX + width;
        int borderEndY = startY + height;

        for (int x = borderStartX; x <= borderEndX; x++)
        {
            for (int y = borderStartY; y <= borderEndY; y++)
            {
                bool isCore = (x >= startX && x < startX + width) && (y >= startY && y < startY + height);
                if (isCore)
                    continue;

                GridNode node = gridManager.GetNode(x, y);
                if (node != null)
                {
                    // Only mark as occupied if walkable (don't overwrite core nodes)
                    if (node.Walkable)
                    {
                        node.BuildingOccupied = true;
                    }
                }
            }
        }
    }

    private void MarkGridNodesUnoccupied(int startX, int startY, int width, int height)
    {
        // Clear core footprint nodes
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = gridManager.GetNode(startX + x, startY + y);
                if (node != null)
                {
                    node.Walkable = true;
                    node.BuildingOccupied = false;
                    gridManager.MarkUnoccupied(node, null);
                }
            }
        }

        // Clear adjacent border nodes if no other buildings occupy them
        int borderStartX = startX;
        int borderStartY = startY;
        int borderEndX = startX + width - 1;
        int borderEndY = startY + height - 1;

        for (int x = borderStartX; x <= borderEndX; x++)
        {
            for (int y = borderStartY; y <= borderEndY; y++)
            {
                bool isCore = (x >= startX && x < startX + width) && (y >= startY && y < startY + height);
                if (isCore)
                    continue;

                GridNode node = gridManager.GetNode(x, y);
                if (node != null)
                {
                    // Only clear if walkable and currently occupied
                    if (node.Walkable && node.BuildingOccupied)
                    {
                        // Check if this node is adjacent to any other building footprints (avoid clearing if adjacent to other buildings)
                        if (!IsNodeAdjacentToAnyBuilding(x, y))
                        {
                            node.BuildingOccupied = false;
                        }
                    }
                }
            }
        }
    }

    private bool IsNodeAdjacentToAnyBuilding(int x, int y)
    {
        // Check the 3x3 area around the node for any core building footprints
        for (int checkX = x - 1; checkX <= x + 1; checkX++)
        {
            for (int checkY = y - 1; checkY <= y + 1; checkY++)
            {
                GridNode node = gridManager.GetNode(checkX, checkY);
                if (node != null && !node.Walkable && node.BuildingOccupied)
                {
                    return true;
                }
            }
        }
        return false;
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
        if (buildingHealth == null) return;

        GameObject destroyedGO = buildingHealth.gameObject;
        int index = placedBuildings.FindIndex(buildInfo => buildInfo.Building == destroyedGO);

        if (index >= 0)
        {
            PlacedBuildingInfo buildInfo = placedBuildings[index];
            MarkGridNodesUnoccupied(buildInfo.BaseGridPosition.x, buildInfo.BaseGridPosition.y, buildInfo.Width, buildInfo.Height);
            placedBuildings.RemoveAt(index);

            BuildingType buildType = destroyedGO.GetComponent<BuildingType>();
            if (buildType != null && buildType.buildingSettings != null)
            {
                string name = buildType.buildingSettings.BuildingName;

                if (name == "Castle")
                {
                    placedCastleInstance = null;
                    Debug.Log("Castle destroyed. Game Over!");
                    GameUiManager.Instance.TriggerGameOver();
                }
                else if (name == "Barracks")
                {
                    totalBarracksPlaced = Mathf.Max(0, totalBarracksPlaced - 1);
                }
                else if (name == "Tower")
                {
                    totalTowersPlaced = Mathf.Max(0, totalTowersPlaced - 1);
                }
            }
        }

        UpdateFilteredBuildings();
    }

    private class PlacedBuildingInfo
    {
        public GameObject Building;
        public Vector2Int BaseGridPosition;
        public int Width;
        public int Height;
    }

    public string GetCurrentBuildingName()
    {
        return filteredBuildingPrefabs.Count == 0 ? "None" :
            filteredBuildingPrefabs[currentFilteredIndex]?.GetComponent<BuildingType>()?.buildingSettings?.BuildingName ?? "Unknown";
    }

    public int GetCurrentPopulationCost()
    {
        return filteredBuildingPrefabs.Count == 0 ? 0 :
            filteredBuildingPrefabs[currentFilteredIndex]?.GetComponent<BuildingType>()?.buildingSettings?.PopulationCost ?? 0;
    }

    public int GetCurrentGoldCost()
    {
        return filteredBuildingPrefabs.Count == 0 ? 0 :
            filteredBuildingPrefabs[currentFilteredIndex]?.GetComponent<BuildingType>()?.buildingSettings?.GoldCost ?? 0;
    }

    private void NotifyUIUpdate()
    {
        guiManager.UpdateBuildingPreviewUI();
    }
}
