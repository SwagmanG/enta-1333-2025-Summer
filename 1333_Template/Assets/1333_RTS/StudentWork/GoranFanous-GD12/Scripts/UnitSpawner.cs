using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Rendering;

public class UnitSpawner : MonoBehaviour
{
    // Prefab for the player-controlled unit
    public GameObject PlayerUnitPrefab;

    // Prefab for the enemy or target unit
    public GameObject TargetPrefab;

    // Reference to the grid system to query positions and nodes
    [SerializeField] private GridManager gridManager;

    // Reference to the A* pathfinding script for route calculation
    [SerializeField] private AstarPathfinding pathfinder;

    // Instance of the currently spawned player unit
    private GameObject currentPlayerUnit;

    // Instance of the currently spawned target/enemy
    private GameObject currentTarget;

    // Flag to track if the player unit is selected (currently unused)
    private bool playerSelected = false;

    // Reference to the main camera
    private Camera mainCamera;

    // Initialize references and spawn the player unit once at the start
    private void Start()
    {
        mainCamera = Camera.main;
        SpawnPlayerUnit();
    }

    // Handle input for spawning and targeting
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnPlayerUnit();
        }

        // Don't allow enemy spawning or pathing while in build mode
        if (BuildModeController.Instance.IsInBuildMode)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            TrySpawnEnemyAtClick();
        }
    }

    // Spawns a player unit at a valid random walkable grid location
    private void SpawnPlayerUnit()
    {
        if (currentPlayerUnit != null)
            Destroy(currentPlayerUnit);

        Vector3 playerPos = GetRandomWalkablePosition() + Vector3.up * 0.5f;
        currentPlayerUnit = Instantiate(PlayerUnitPrefab, playerPos, Quaternion.identity);

        UnitController playerController = currentPlayerUnit.GetComponent<UnitController>();
        playerController.armyType = ArmyType.Player;
        playerController.StopMovement();
        playerController.SetPathfinder(pathfinder); //  Assign pathfinder
    }

    // Handles left mouse click to place an enemy unit and pathfind to it
    private void TrySpawnEnemyAtClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 spawnPosition = hit.point;
            Vector2Int gridCoords = gridManager.GetGridPosFromWorld(spawnPosition);
            GridNode node = gridManager.GetNode(gridCoords.x, gridCoords.y);

            if (currentTarget != null)
                Destroy(currentTarget);

            Vector3 targetPos = node.WorldPosition + Vector3.up * 0.5f;
            currentTarget = Instantiate(TargetPrefab, targetPos, Quaternion.identity);

            UnitController enemyController = currentTarget.GetComponent<UnitController>();
            enemyController.armyType = ArmyType.Enemy;
            enemyController.SetPathfinder(pathfinder); //  Assign pathfinder

            if (currentPlayerUnit != null)
            {
                UnitController playerController = currentPlayerUnit.GetComponent<UnitController>();
                playerController.RequestPath(targetPos); //  Ask player to find path
            }

            /*if (node != null && node.Walkable)
            {
                if (currentTarget != null)
                    Destroy(currentTarget);

                Vector3 targetPos = node.WorldPosition + Vector3.up * 0.5f;
                currentTarget = Instantiate(TargetPrefab, targetPos, Quaternion.identity);

                UnitController enemyController = currentTarget.GetComponent<UnitController>();
                enemyController.armyType = ArmyType.Enemy;

                if (currentPlayerUnit != null)
                {
                    UnitAIController playerAI = currentPlayerUnit.GetComponent<UnitAIController>();
                    if (playerAI != null)
                    {
                        playerAI.SetTarget(currentTarget.transform); // tell the unit its new goal
                    }
                }
            }*/
        }
    }

    // Spawns both player and enemy at different random locations and triggers pathfinding
    private void SpawnUnits()
    {
        if (currentPlayerUnit != null)
            Destroy(currentPlayerUnit);
        if (currentTarget != null)
            Destroy(currentTarget);

        Vector3 playerPos = GetRandomWalkablePosition() + Vector3.up * 0.5f;
        Vector3 targetPos = GetRandomWalkablePosition() + Vector3.up * 0.5f;

        while (targetPos == playerPos)
        {
            targetPos = GetRandomWalkablePosition() + Vector3.up * 0.5f;
        }

        currentPlayerUnit = Instantiate(PlayerUnitPrefab, playerPos, Quaternion.identity);
        UnitController playerController = currentPlayerUnit.GetComponent<UnitController>();
        playerController.armyType = ArmyType.Player;
        playerController.StopMovement();

        currentTarget = Instantiate(TargetPrefab, targetPos, Quaternion.identity);
        UnitController enemyController = currentTarget.GetComponent<UnitController>();
        enemyController.armyType = ArmyType.Enemy;

        pathfinder.VisualizePath(playerPos, targetPos, playerController);
    }

    // Selects a random walkable node from the grid and returns its world position
    private Vector3 GetRandomWalkablePosition()
    {
        GridSettings settings = gridManager.GridSettings;
        Vector3 pos;

        while (true)
        {
            int x = Random.Range(0, settings.GridSizeX);
            int y = Random.Range(0, settings.GridSizeY);
            GridNode node = gridManager.GetNode(x, y);

            if (node != null && node.Walkable)
            {
                pos = node.WorldPosition;
                break;
            }
        }

        return pos;
    }



    // Draws a line in the scene view between the player and enemy units
    private void OnDrawGizmos()
    {
        if (currentTarget != null && currentPlayerUnit != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(currentPlayerUnit.transform.position, currentTarget.transform.position);
        }
    }
}