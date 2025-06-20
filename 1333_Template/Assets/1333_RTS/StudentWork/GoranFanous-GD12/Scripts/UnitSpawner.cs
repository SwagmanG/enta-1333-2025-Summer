using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the spawning of an enemy target unit in the world based on mouse clicks.
/// Also commands all player units to pathfind toward the new enemy target.
/// </summary>
public class UnitSpawner : MonoBehaviour
{
    [Header("Prefab for enemy target")]
    public GameObject enemyTargetPrefab;

    [SerializeField] private GridManager gridManager;

    private GameObject currentEnemyTarget;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Disable unit spawning while in build mode
        if (BuildModeController.Instance.IsInBuildMode)
            return;

        // Listen for left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            TrySpawnEnemyAtClickPosition();
        }
    }

    /// <summary>
    /// Attempts to spawn an enemy target at the clicked grid location if the node is valid.
    /// </summary>
    private void TrySpawnEnemyAtClickPosition()
    {
        // Cast a ray from the mouse position into the world
        Ray mouseClickRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseClickRay, out RaycastHit rayHitInfo, 100f))
        {
            Vector3 clickedWorldPosition = rayHitInfo.point;

            // Convert world position to grid coordinates
            Vector2Int clickedGridPosition = gridManager.GetGridPosFromWorld(clickedWorldPosition);
            GridNode clickedNode = gridManager.GetNode(clickedGridPosition.x, clickedGridPosition.y);

            // Skip if node is invalid, unwalkable, or already occupied
            if (clickedNode == null || !clickedNode.Walkable || gridManager.IsOccupied(clickedNode))
                return;

            // Remove the previously spawned target and clear its occupied status
            if (currentEnemyTarget != null)
            {
                Vector2Int previousTargetGridPosition = gridManager.GetGridPosFromWorld(currentEnemyTarget.transform.position);
                GridNode previousTargetNode = gridManager.GetNode(previousTargetGridPosition.x, previousTargetGridPosition.y);
                gridManager.MarkUnoccupied(previousTargetNode, null);
                Destroy(currentEnemyTarget);
            }

            // Spawn the new enemy target slightly above the grid node position
            Vector3 spawnPosition = clickedNode.WorldPosition + Vector3.up * 0.5f;
            currentEnemyTarget = Instantiate(enemyTargetPrefab, spawnPosition, Quaternion.identity);

            // Mark the node as occupied (even though it's just a dummy target)
            gridManager.MarkOccupied(clickedNode, null);

            // Instruct all player units to pathfind to the new enemy target
            UnitController[] allUnits = GameObject.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (UnitController unit in allUnits)
            {
                if (unit.armyType == ArmyType.Player)
                {
                    unit.RequestPath(currentEnemyTarget.transform.position);
                }
            }
        }
    }
}
