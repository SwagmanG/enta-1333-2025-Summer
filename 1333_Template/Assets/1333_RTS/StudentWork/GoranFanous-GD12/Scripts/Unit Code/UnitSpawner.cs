using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Prefab for enemy unit")]
    public GameObject enemyUnitPrefab;

    // Reference to the GridManager which handles grid-related operations
    [SerializeField] private GridManager gridManager;

    // Reference to the main camera for raycasting mouse clicks to world positions
    private Camera mainCamera;

    private void Start()
    {
        // Cache main camera reference on start for performance
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Do not allow spawning while in build mode
        if (BuildModeController.Instance.IsInBuildMode)
            return;

        // On left mouse button click, attempt to spawn an enemy unit
        if (Input.GetMouseButtonDown(0))
        {
            TrySpawnEnemyAtClickPosition();
        }
    }

    /// <summary>
    /// Attempts to spawn an enemy unit at the grid node corresponding to the mouse click position.
    /// </summary>
    private void TrySpawnEnemyAtClickPosition()
    {
        // Create a ray from the camera through the mouse cursor position
        Ray mouseClickRay = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Perform a raycast to detect collision point in the world
        if (Physics.Raycast(mouseClickRay, out RaycastHit rayHitInfo, 100f))
        {
            // The exact world position where the mouse clicked on a collider
            Vector3 clickedWorldPosition = rayHitInfo.point;

            // Convert the world position to grid coordinates (x, y)
            Vector2Int clickedGridCoordinates = gridManager.GetGridPosFromWorld(clickedWorldPosition);

            // Retrieve the GridNode at the clicked coordinates
            GridNode clickedNode = gridManager.GetNode(clickedGridCoordinates.x, clickedGridCoordinates.y);

            // Ensure the node exists and is walkable (spawnable)
            if (clickedNode == null || !clickedNode.Walkable)
                return;

            // Calculate spawn position slightly above the ground to avoid clipping
            Vector3 spawnPosition = clickedNode.WorldPosition + Vector3.up * 0.5f;

            // Instantiate the enemy unit prefab at the spawn position with no rotation
            Instantiate(enemyUnitPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
