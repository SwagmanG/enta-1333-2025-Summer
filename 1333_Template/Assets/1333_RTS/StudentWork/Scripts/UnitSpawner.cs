using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Rendering;

public class UnitSpawner : MonoBehaviour
{
    // Prefab to spawn as the player-controlled unit
    public GameObject PlayerUnitPrefab;

    // Prefab to spawn as the target/destination
    public GameObject TargetPrefab;

    // Reference to the GridManager to access grid data
    [SerializeField] private GridManager gridManager;

    // Reference to the A* pathfinding component
    [SerializeField] private AstarPathfinding pathfinder;

    // References to the currently spawned player and target objects
    private GameObject currentPlayerUnit;
    private GameObject currentTarget;

    // Called when the game starts
    private void Start()
    {
        SpawnUnits(); // Spawn the player and target at start
    }

    // Called every frame
    private void Update()
    {
        // If the spacebar is pressed, respawn player and target and update path
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnUnits();
        }
    }

    // Spawns player and target at two distinct walkable positions on the grid
    private void SpawnUnits()
    {
        // Destroy previously spawned player and target units
        if (currentPlayerUnit != null)
            Destroy(currentPlayerUnit);
        if (currentTarget != null)
            Destroy(currentTarget);

        // Get a random walkable position for the player and target
        Vector3 playerPos = GetRandomWalkablePosition() + Vector3.up * 0.5f;
        Vector3 targetPos = GetRandomWalkablePosition() + Vector3.up * 0.5f;

        // Ensure player and target don't spawn at the same position
        while (targetPos == playerPos)
        {
            targetPos = GetRandomWalkablePosition() + Vector3.up * 0.5f;
        }

        // Instantiate the player and target prefabs at their respective positions
        currentPlayerUnit = Instantiate(PlayerUnitPrefab, playerPos, Quaternion.identity);
        currentTarget = Instantiate(TargetPrefab, targetPos, Quaternion.identity);

        // Trigger pathfinding visualization between the player and target
        pathfinder.VisualizePath(playerPos, targetPos);
    }

    // Returns a random walkable position from the grid
    private Vector3 GetRandomWalkablePosition()
    {
        GridSettings settings = gridManager.GridSettings;
        Vector3 pos;

        while (true)
        {
            // Choose random coordinates within grid bounds
            int x = Random.Range(0, settings.GridSizeX);
            int y = Random.Range(0, settings.GridSizeY);

            // Get the node at those coordinates
            GridNode node = gridManager.GetNode(x, y);

            // If the node exists and is walkable, return its world position
            if (node != null && node.Walkable)
            {
                pos = node.WorldPosition;
                break;
            }
        }

        return pos;
    }

    // Draws a debug line between the player and target in the editor
    private void OnDrawGizmos()
    {
        if (currentTarget != null && currentPlayerUnit != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(currentPlayerUnit.transform.position, currentTarget.transform.position);
        }
    }
}