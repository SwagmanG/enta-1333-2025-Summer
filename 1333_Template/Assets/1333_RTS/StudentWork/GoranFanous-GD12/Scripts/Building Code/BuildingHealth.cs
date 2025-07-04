using UnityEngine;
using RTS_1333;

/// <summary>
/// Manages health, damage, and grid occupation state for a building.
/// Buildings occupy multiple grid nodes based on size.
/// </summary>
public class BuildingHealth : MonoBehaviour
{
    [SerializeField] private BuildingSettings buildingSettings;   // Settings containing building size and health info
    [SerializeField] private ArmyType armyType;                   // The army this building belongs to

    private int currentHealth;                                    // Current health of the building
    private GridManager gridManager;                              // Reference to the GridManager for grid operations

    // Public getters for external access
    public ArmyType ArmyType => armyType;
    public BuildingSettings BuildingSettings => buildingSettings;
    public int CurrentHealth => currentHealth;

    private void Start()
    {
        if (buildingSettings == null)
        {
            Debug.LogError($"BuildingSettings not assigned on {gameObject.name}");
            return;
        }

        // Initialize health from settings
        currentHealth = buildingSettings.MaxHealth;

        // Find the grid manager in the scene
        gridManager = FindFirstObjectByType<GridManager>();

        // Mark grid nodes as occupied based on building footprint
        MarkOccupiedNodes(true);
    }

    /// <summary>
    /// Apply damage to the building and check for destruction.
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply.</param>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"{buildingSettings.BuildingName} took {damageAmount} damage. Current health: {currentHealth}");

        if (currentHealth <= 0)
            DestroyBuilding();
    }

    /// <summary>
    /// Handle destruction of the building, freeing grid nodes.
    /// </summary>
    private void DestroyBuilding()
    {
        Debug.Log($"{buildingSettings.BuildingName} destroyed!");

        // Unmark grid nodes as walkable before destroying building
        MarkOccupiedNodes(false);

        Destroy(gameObject);
    }

    /// <summary>
    /// Marks or unmarks the grid nodes occupied by this building as walkable/unwalkable.
    /// </summary>
    /// <param name="isOccupied">True to mark nodes as occupied (unwalkable), false to unmark (walkable).</param>
    private void MarkOccupiedNodes(bool isOccupied)
    {
        if (gridManager == null || buildingSettings == null)
            return;

        // Get the grid coordinate of the building's base position
        Vector2Int buildingBaseGridPos = gridManager.GetGridPosFromWorld(transform.position);

        // Width and height of the building in grid nodes, from settings
        int buildingWidthInNodes = buildingSettings.BuildingSizeX;
        int buildingHeightInNodes = buildingSettings.BuildingSizeY;

        // Iterate over all grid nodes covered by this building footprint
        for (int offsetX = 0; offsetX < buildingWidthInNodes; offsetX++)
        {
            for (int offsetY = 0; offsetY < buildingHeightInNodes; offsetY++)
            {
                // Get the specific node position relative to the building's base node
                GridNode node = gridManager.GetNode(buildingBaseGridPos.x + offsetX, buildingBaseGridPos.y + offsetY);

                if (node != null)
                {
                    // Set the node walkability based on occupation status
                    node.Walkable = !isOccupied;

                    // Inform GridManager of the node's occupation state
                    if (!isOccupied)
                    {
                        gridManager.MarkUnoccupied(node, null);
                    }
                    else
                    {
                        gridManager.MarkOccupied(node, null);
                    }
                }
            }
        }
    }
}
