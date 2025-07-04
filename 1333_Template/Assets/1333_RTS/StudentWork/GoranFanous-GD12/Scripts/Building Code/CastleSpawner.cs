using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerUnitPrefab;
    private GridManager gridManager;

    private void Start()
    {
        // Ensure the player unit prefab is assigned
        if (playerUnitPrefab == null)
        {
            Debug.LogError("CastleSpawner: Please assign PlayerUnitPrefab in inspector.");
            enabled = false;
            return;
        }

        // Find and cache the GridManager instance in the scene
        gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("CastleSpawner: Could not find GridManager in scene.");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Check if spacebar is pressed to trigger spawning a new unit
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TrySpawnPlayerUnitNearCastle();
        }
    }

    /// <summary>
    /// Attempts to spawn a player unit near the castle by finding a free walkable adjacent grid node.
    /// </summary>
    private void TrySpawnPlayerUnitNearCastle()
    {
        // Get the grid position of the castle based on world position
        Vector2Int castleGridPosition = gridManager.GetGridPosFromWorld(transform.position);
        GridNode castleGridNode = gridManager.GetNode(castleGridPosition.x, castleGridPosition.y);

        if (castleGridNode == null)
        {
            Debug.LogWarning("CastleSpawner: Castle node not found on grid.");
            return;
        }

        // Collect candidate spawn nodes that are adjacent to non-walkable castle area nodes
        HashSet<GridNode> candidateSpawnNodes = new();

        // All eight possible directions around a node (including diagonals)
        int[,] surroundingOffsets = new int[,]
        {
            { 0, 1 },   // North
            { 1, 0 },   // East
            { 0, -1 },  // South
            { -1, 0 },  // West
            { 1, 1 },   // Northeast
            { -1, -1 }, // Southwest
            { 1, -1 },  // Southeast
            { -1, 1 }   // Northwest
        };

        // Iterate through a 3x3 grid centered on the castle's grid position
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                int currentX = castleGridPosition.x + offsetX;
                int currentY = castleGridPosition.y + offsetY;

                GridNode currentNode = gridManager.GetNode(currentX, currentY);

                // If the current node is non-walkable (castle area), check its neighbors
                if (currentNode != null && !currentNode.Walkable)
                {
                    for (int directionIndex = 0; directionIndex < surroundingOffsets.GetLength(0); directionIndex++)
                    {
                        int neighborX = currentX + surroundingOffsets[directionIndex, 0];
                        int neighborY = currentY + surroundingOffsets[directionIndex, 1];

                        GridNode neighborNode = gridManager.GetNode(neighborX, neighborY);

                        // Add neighbor nodes that are walkable and not currently occupied
                        if (neighborNode != null && neighborNode.Walkable && !gridManager.IsOccupied(neighborNode))
                        {
                            candidateSpawnNodes.Add(neighborNode);
                        }
                    }
                }
            }
        }

        // Try to find a free node from candidate nodes to spawn the unit
        foreach (GridNode freeNode in candidateSpawnNodes)
        {
            if (IsNodeAvailableForSpawn(freeNode))
            {
                SpawnPlayerUnitAtNode(freeNode);
                return;
            }
        }

        Debug.Log("CastleSpawner: No free node found around castle to spawn unit.");
    }

    /// <summary>
    /// Checks if the specified node is free and walkable for spawning a unit.
    /// </summary>
    /// <param name="node">The grid node to check.</param>
    /// <returns>True if free and walkable, false otherwise.</returns>
    private bool IsNodeAvailableForSpawn(GridNode node)
    {
        return node != null && node.Walkable && !gridManager.IsOccupied(node);
    }

    /// <summary>
    /// Instantiates a player unit at the specified grid node and commands all player units to pathfind to the current target.
    /// </summary>
    /// <param name="spawnNode">The grid node to spawn the unit at.</param>
    private void SpawnPlayerUnitAtNode(GridNode spawnNode)
    {
        Vector3 spawnPosition = spawnNode.WorldPosition + Vector3.up * 0.5f; // Slightly above ground for visibility
        GameObject newUnitObject = Instantiate(playerUnitPrefab, spawnPosition, Quaternion.identity);

        UnitController unitController = newUnitObject.GetComponent<UnitController>();
        if (unitController != null)
        {
            unitController.armyType = ArmyType.Player;
        }

        Debug.Log($"CastleSpawner: Spawned player unit at {spawnPosition}");

        // Find the current target (e.g. enemy or objective) by tag
        GameObject currentTargetObject = GameObject.FindGameObjectWithTag("Target");
        if (currentTargetObject != null)
        {
            // Find all player units and command them to pathfind to the target position
            UnitController[] allUnits = GameObject.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (UnitController playerUnit in allUnits)
            {
                if (playerUnit.armyType == ArmyType.Player)
                {
                    playerUnit.RequestPath(currentTargetObject.transform.position);
                }
            }
        }
    }
}
