using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;

/// <summary>
/// Main controller for the building system - handles placing, previewing, and managing buildings
/// </summary>
public class BuildingManager : MonoBehaviour
{
    // Singleton pattern for easy access from other scripts
    public static BuildingManager BMInstance { get; private set; }

    [Header("References")]
    // Core managers we need to communicate with
    public GridManager GridManager;
    public GameUiManager GuiManager;

    [Header("Building Prefabs")]
    // List of all buildings that can be placed in the game
    [SerializeField] private List<GameObject> buildingPrefabs;

    [Header("Preview Settings")]
    // Materials to show whether a building can be placed or not
    [SerializeField] private Material previewValidMaterial;
    [SerializeField] private Material previewInvalidMaterial;

    // Camera reference for raycasting from mouse position
    private Camera mainCamera;
    // The ghost building that follows your mouse cursor
    private GameObject previewBuildingInstance;
    // Current rotation angle (0, 90, 180, or 270 degrees)
    private int currentRotation = 0;

    // Buildings that can currently be placed (filtered based on game state)
    private List<GameObject> filteredBuildingPrefabs = new();
    // Which building in the filtered list we're currently selecting
    private int currentFilteredIndex = 0;

    // Keep track of all buildings we've placed for management
    private readonly List<PlacedBuildingInfo> placedBuildings = new();
    // Special reference to the castle since it's important for game logic
    private GameObject placedCastleInstance = null;

    // Track building counts for game rules (towers can only be built if you have barracks)
    private int totalBarracksPlaced = 0;
    private int totalTowersPlaced = 0;

    /// <summary>
    /// Set up singleton pattern when object is created
    /// </summary>
    private void Awake()
    {
        // Make sure only one BuildingManager exists
        if (BMInstance != null && BMInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        BMInstance = this;
    }

    /// <summary>
    /// Initialize everything once the game starts
    /// </summary>
    private void Start()
    {
        // Get the main camera for mouse raycasting
        mainCamera = Camera.main;
        // Set up which buildings can be placed initially
        UpdateFilteredBuildings();
        // Tell the UI to update with current building info
        NotifyUIUpdate();
    }

    /// <summary>
    /// Handle all the building logic every frame
    /// </summary>
    private void Update()
    {
        // Only do building stuff if we're actually in build mode
        if (!BuildModeController.BMCInstance.IsInBuildMode)
        {
            DestroyPreview();
            return;
        }

        // Keep our available buildings list up to date
        UpdateFilteredBuildings();
        // Check if player wants to cycle through buildings (Q/E keys)
        HandleBuildingCycleInput();
        // Check if player wants to rotate the building (R key)
        HandleRotationInput();
        // Show the ghost preview of where the building will be placed
        UpdatePreviewBuilding();

        // Try to place a building when left mouse is clicked
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuildingAtMouseClick();
        }
    }

    /// <summary>
    /// Figure out which buildings the player is allowed to place right now
    /// </summary>
    private void UpdateFilteredBuildings()
    {
        // Filter buildings based on game rules
        filteredBuildingPrefabs = buildingPrefabs.Where(prefab =>
        {
            BuildingType buildingType = prefab.GetComponent<BuildingType>();
            if (buildingType == null || buildingType.buildingSettings == null) return false;

            string name = buildingType.buildingSettings.BuildingName;

            // Can't place more than one castle
            if (name == "Castle" && placedCastleInstance != null)
                return false;

            // Towers require barracks and have a limit based on barracks count
            if (name == "Tower")
            {
                if (totalBarracksPlaced == 0) return false;
                if (totalTowersPlaced >= totalBarracksPlaced * 6) return false;
            }

            return true;

        }).ToList();

        // Make sure our selected index stays within bounds
        if (filteredBuildingPrefabs.Count == 0)
        {
            currentFilteredIndex = 0;
        }
        else
        {
            currentFilteredIndex %= filteredBuildingPrefabs.Count;
        }
    }

    /// <summary>
    /// Handle player input for cycling through available buildings
    /// </summary>
    private void HandleBuildingCycleInput()
    {
        // Can't cycle if there are no buildings available
        if (filteredBuildingPrefabs.Count == 0)
            return;

        bool cycled = false;

        // E key cycles forward through buildings
        if (Input.GetKeyDown(KeyCode.E))
        {
            currentFilteredIndex = (currentFilteredIndex + 1) % filteredBuildingPrefabs.Count;
            cycled = true;
        }
        // Q key cycles backward through buildings
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            currentFilteredIndex = (currentFilteredIndex - 1 + filteredBuildingPrefabs.Count) % filteredBuildingPrefabs.Count;
            cycled = true;
        }

        // Play sound and update UI when we cycle
        if (cycled)
        {
            AudioManager.AMInstance?.PlaySFX("Cycle Buildings");
            DestroyPreview();
            NotifyUIUpdate();
        }
    }

    /// <summary>
    /// Handle building rotation input
    /// </summary>
    private void HandleRotationInput()
    {
        // R key rotates the building 90 degrees
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation = (currentRotation + 90) % 360;
            // Update the preview building rotation immediately
            if (previewBuildingInstance != null)
                previewBuildingInstance.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        }
    }

    /// <summary>
    /// Show a ghost preview of the building at the mouse cursor
    /// </summary>
    private void UpdatePreviewBuilding()
    {
        // Need buildings and grid to work with
        if (filteredBuildingPrefabs.Count == 0 || GridManager == null)
            return;

        // Cast a ray from camera through mouse position to find where we're pointing
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
        {
            DestroyPreview();
            return;
        }

        // Convert world position to grid coordinates
        Vector3 worldPos = hitInfo.point;
        Vector2Int gridPos = GridManager.GetGridPosFromWorld(worldPos);

        // Get the building we want to preview
        GameObject prefab = filteredBuildingPrefabs[currentFilteredIndex];
        if (prefab == null) return;

        BuildingType buildingType = prefab.GetComponent<BuildingType>();
        if (buildingType == null || buildingType.buildingSettings == null) return;

        // Figure out the building's size (might be rotated)
        int width = buildingType.buildingSettings.BuildingSizeX;
        int height = buildingType.buildingSettings.BuildingSizeY;
        float nodeSize = GridManager.GridSettings.NodeSize;

        // Swap width/height if rotated 90 or 270 degrees
        bool rotated = currentRotation % 180 != 0;
        int finalWidth = rotated ? height : width;
        int finalHeight = rotated ? width : height;

        // Calculate where the building should be positioned in world space
        Vector3 previewWorldPos = CalculateWorldPosition(gridPos, finalWidth, finalHeight, nodeSize);

        // Check if we can actually place the building here
        bool canPlace = CanPlaceBuildingAt(gridPos.x, gridPos.y, finalWidth, finalHeight);

        // Check if player has enough resources
        int requiredPopulation = buildingType.buildingSettings.PopulationCost;
        int requiredGold = buildingType.buildingSettings.GoldCost;

        bool hasEnoughPopulation = ResourceManager.RMInstance.CurrentPopulation >= requiredPopulation;
        bool hasEnoughGold = ResourceManager.RMInstance.CurrentGold >= requiredGold;

        bool hasEnoughResources = hasEnoughPopulation && hasEnoughGold;
        bool showAsValid = canPlace && hasEnoughResources;

        // Create or update the preview building
        if (previewBuildingInstance == null)
        {
            previewBuildingInstance = Instantiate(prefab, previewWorldPos, Quaternion.Euler(0, currentRotation, 0));
            previewBuildingInstance.transform.localScale = Vector3.one * buildingType.buildingSettings.BuildScale;
        }

        // Update position and rotation
        previewBuildingInstance.transform.position = previewWorldPos;
        previewBuildingInstance.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
        // Color it green if valid, red if invalid
        SetPreviewMaterial(previewBuildingInstance, showAsValid);
    }

    /// <summary>
    /// Change the preview building's material to show if it can be placed
    /// </summary>
    private void SetPreviewMaterial(GameObject building, bool canPlace)
    {
        Material targetMaterial = canPlace ? previewValidMaterial : previewInvalidMaterial;
        // Apply the material to all renderers on the building
        foreach (var renderer in building.GetComponentsInChildren<Renderer>())
        {
            renderer.material = targetMaterial;
        }
    }

    /// <summary>
    /// Remove the preview building from the scene
    /// </summary>
    private void DestroyPreview()
    {
        if (previewBuildingInstance != null)
        {
            Destroy(previewBuildingInstance);
            previewBuildingInstance = null;
        }
    }

    /// <summary>
    /// Try to actually place a building when the player clicks
    /// </summary>
    private void TryPlaceBuildingAtMouseClick()
    {
        // Need buildings and grid to work with
        if (filteredBuildingPrefabs.Count == 0 || GridManager == null)
            return;

        GameObject prefab = filteredBuildingPrefabs[currentFilteredIndex];
        if (prefab == null) return;

        BuildingType buildingType = prefab.GetComponent<BuildingType>();
        if (buildingType == null || buildingType.buildingSettings == null) return;

        // Get building info and costs
        string buildingName = buildingType.buildingSettings.BuildingName;
        int populationCost = buildingType.buildingSettings.PopulationCost;
        int goldCost = buildingType.buildingSettings.GoldCost;

        var resourceManager = ResourceManager.RMInstance;

        // Check if player has enough resources
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

        // Figure out where the mouse is pointing
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
            return;

        Vector3 worldPos = hitInfo.point;
        Vector2Int gridPos = GridManager.GetGridPosFromWorld(worldPos);

        // Calculate building size with rotation
        int width = buildingType.buildingSettings.BuildingSizeX;
        int height = buildingType.buildingSettings.BuildingSizeY;
        float nodeSize = GridManager.GridSettings.NodeSize;

        bool rotated = currentRotation % 180 != 0;
        int finalWidth = rotated ? height : width;
        int finalHeight = rotated ? width : height;

        // Make sure we can place it here
        if (!CanPlaceBuildingAt(gridPos.x, gridPos.y, finalWidth, finalHeight))
        {
            Debug.Log("Cannot place building here.");
            return;
        }

        // Charge the player for the building
        resourceManager.SpendPopulation(populationCost);
        resourceManager.SpendGold(goldCost);

        // Create the actual building
        Vector3 placeWorldPos = CalculateWorldPosition(gridPos, finalWidth, finalHeight, nodeSize);
        GameObject newBuilding = Instantiate(prefab, placeWorldPos, Quaternion.Euler(0, currentRotation, 0));
        newBuilding.transform.localScale = Vector3.one * buildingType.buildingSettings.BuildScale;

        // Mark the grid spots as occupied
        MarkGridNodesOccupied(gridPos.x, gridPos.y, finalWidth, finalHeight);

        // Keep track of this building
        placedBuildings.Add(new PlacedBuildingInfo
        {
            Building = newBuilding,
            BaseGridPosition = gridPos,
            Width = finalWidth,
            Height = finalHeight
        });

        // Special handling for different building types
        if (buildingName == "Castle")
        {
            placedCastleInstance = newBuilding;

            // Start the castle's resource generation
            CastleResourceManager castleRes = newBuilding.GetComponent<CastleResourceManager>();
            if (castleRes != null)
            {
                castleRes.ActivateResourceGeneration();
            }
        }
        else
        {
            // Non-castle buildings have ongoing food costs
            StartCoroutine(ApplyFoodTaxCoroutine(buildingType.buildingSettings.FoodTax));

            // Add this building's bonuses to the castle if it exists
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

        // Update our building counts
        if (buildingName == "Barracks") totalBarracksPlaced++;
        else if (buildingName == "Tower") totalTowersPlaced++;

        // Play placement sound and clean up
        AudioManager.AMInstance?.PlaySFXAtPosition("Building Placed", placeWorldPos);
        DestroyPreview();
    }

    /// <summary>
    /// Apply ongoing food costs for buildings (runs every 20 seconds)
    /// </summary>
    private IEnumerator ApplyFoodTaxCoroutine(int foodTax)
    {
        while (true)
        {
            yield return new WaitForSeconds(20f);
            ResourceManager.RMInstance?.SpendFood(foodTax);
        }
    }

    /// <summary>
    /// Check if a building can be placed at the specified grid location
    /// </summary>
    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        // Check the building footprint plus a border around it
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
                    // Core building footprint must be walkable (empty)
                    GridNode node = GridManager.GetNode(x, y);
                    if (node == null || !node.Walkable)
                        return false;
                }
                else
                {
                    // Border tiles must not be occupied by other buildings
                    GridNode node = GridManager.GetNode(x, y);
                    if (node != null && node.BuildingOccupied)
                        return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Mark grid nodes as occupied when a building is placed
    /// </summary>
    private void MarkGridNodesOccupied(int startX, int startY, int width, int height)
    {
        // Mark the building's footprint as non-walkable and occupied
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = GridManager.GetNode(startX + x, startY + y);
                if (node != null)
                {
                    node.Walkable = false;
                    node.BuildingOccupied = true; // Occupied by building
                    GridManager.MarkOccupied(node, null);
                }
            }
        }

        // Mark adjacent border nodes as occupied but keep them walkable
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

                GridNode node = GridManager.GetNode(x, y);
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

    /// <summary>
    /// Free up grid nodes when a building is destroyed
    /// </summary>
    private void MarkGridNodesUnoccupied(int startX, int startY, int width, int height)
    {
        // Clear the building's footprint
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridNode node = GridManager.GetNode(startX + x, startY + y);
                if (node != null)
                {
                    node.Walkable = true;
                    node.BuildingOccupied = false;
                    GridManager.MarkUnoccupied(node, null);
                }
            }
        }

        // Clear adjacent border nodes if no other buildings need them
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

                GridNode node = GridManager.GetNode(x, y);
                if (node != null)
                {
                    // Only clear if walkable and currently occupied
                    if (node.Walkable && node.BuildingOccupied)
                    {
                        // Check if this node is needed by other buildings
                        if (!IsNodeAdjacentToAnyBuilding(x, y))
                        {
                            node.BuildingOccupied = false;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check if a grid node is next to any building (used for cleanup)
    /// </summary>
    private bool IsNodeAdjacentToAnyBuilding(int x, int y)
    {
        // Check the 3x3 area around the node for any building footprints
        for (int checkX = x - 1; checkX <= x + 1; checkX++)
        {
            for (int checkY = y - 1; checkY <= y + 1; checkY++)
            {
                GridNode node = GridManager.GetNode(checkX, checkY);
                if (node != null && !node.Walkable && node.BuildingOccupied)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Calculate the world position for a building based on its grid position and size
    /// </summary>
    private Vector3 CalculateWorldPosition(Vector2Int gridOrigin, int width, int height, float nodeSize)
    {
        // Center the building on its grid footprint
        float offsetX = (width - 1) * 0.5f * nodeSize;
        float offsetY = (height - 1) * 0.5f * nodeSize;

        // Convert grid position to world position
        Vector3 basePos = GridManager.GridSettings.UseXZPlane
            ? new Vector3(gridOrigin.x, 0f, gridOrigin.y) * nodeSize
            : new Vector3(gridOrigin.x, gridOrigin.y, 0f) * nodeSize;

        // Apply the centering offset
        return GridManager.GridSettings.UseXZPlane
            ? basePos + new Vector3(offsetX, 0.5f, offsetY)
            : basePos + new Vector3(offsetX, offsetY, 0.5f);
    }

    /// <summary>
    /// Called when a building is destroyed - handle cleanup and game logic
    /// </summary>
    public void OnBuildingDestroyed(BuildingHealth buildingHealth)
    {
        if (buildingHealth == null) return;

        GameObject destroyedGO = buildingHealth.gameObject;
        int index = placedBuildings.FindIndex(buildInfo => buildInfo.Building == destroyedGO);

        if (index >= 0)
        {
            PlacedBuildingInfo buildInfo = placedBuildings[index];
            // Free up the grid spots this building was using
            MarkGridNodesUnoccupied(buildInfo.BaseGridPosition.x, buildInfo.BaseGridPosition.y, buildInfo.Width, buildInfo.Height);
            placedBuildings.RemoveAt(index);

            // Handle special cases for different building types
            BuildingType buildType = destroyedGO.GetComponent<BuildingType>();
            if (buildType != null && buildType.buildingSettings != null)
            {
                string name = buildType.buildingSettings.BuildingName;

                if (name == "Castle")
                {
                    // Game over if castle is destroyed
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

        // Update what buildings can be placed now
        UpdateFilteredBuildings();
    }

    /// <summary>
    /// Simple data class to keep track of placed buildings
    /// </summary>
    private class PlacedBuildingInfo
    {
        public GameObject Building;
        public Vector2Int BaseGridPosition;
        public int Width;
        public int Height;
    }

    // Public methods for UI to get information about the currently selected building
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

    /// <summary>
    /// Tell the UI to update with current building information
    /// </summary>
    private void NotifyUIUpdate()
    {
        GuiManager.UpdateBuildingPreviewUI();
    }
}