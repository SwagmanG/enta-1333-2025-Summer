using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Enemy Unit Prefabs (must have UnitController with UnitType)")]
    [SerializeField] private List<GameObject> enemyUnitPrefabs;

    [Header("Grid Manager Reference")]
    [SerializeField] private GridManager gridManager;

    [Header("Wave Settings")]
    [Tooltip("Number of waves to spawn")]
    [SerializeField] private int numberOfWaves = 5;

    [Tooltip("Seconds between each wave spawn")]
    [SerializeField] private float timeBetweenWaves = 10f;

    [Tooltip("Multiplier applied to enemy counts after each wave")]
    [SerializeField] private float waveCountMultiplier = 1.2f;

    [Tooltip("Base amount of each enemy prefab to spawn in the first wave")]
    [SerializeField] private List<int> baseEnemyCounts;

    private int currentWave = 0;
    private Camera mainCamera;

    // Flag to control when waves can start spawning
    private bool canSpawnWave = false;

    private Coroutine waveCoroutine;

    private void Start()
    {
        mainCamera = Camera.main;

        if (enemyUnitPrefabs == null || enemyUnitPrefabs.Count == 0)
        {
            Debug.LogError("UnitSpawner: No enemy unit prefabs assigned!");
            enabled = false;
            return;
        }

        if (baseEnemyCounts == null || baseEnemyCounts.Count != enemyUnitPrefabs.Count)
        {
            // Initialize baseEnemyCounts to 1 for each prefab if not set or mismatched
            baseEnemyCounts = new List<int>(new int[enemyUnitPrefabs.Count]);
            for (int i = 0; i < baseEnemyCounts.Count; i++)
            {
                baseEnemyCounts[i] = 1;
            }
        }

        // Start the wave routine but it will wait until forced start
        waveCoroutine = StartCoroutine(SpawnWaves());
    }

    /// <summary>
    /// Public method to force-start the current wave immediately.
    /// If the first wave hasn't started yet, this triggers it.
    /// If waiting between waves, this skips the wait and starts next wave immediately.
    /// </summary>
    public void ForceStartCurrentWave()
    {

        Debug.Log("Wave Start");
        if (!canSpawnWave)
        {
            canSpawnWave = true;
            Debug.Log("Force started first wave!");
        }
        else
        {
            // If currently waiting between waves, skip wait to start next wave immediately
            StopCoroutine(waveCoroutine);
            waveCoroutine = StartCoroutine(SpawnWaves(currentWave));
            Debug.Log($"Force started wave {currentWave + 1}!");
        }
    }

    /// <summary>
    /// Coroutine to spawn waves.
    /// If startFromWaveIndex is specified, resumes spawning from that wave.
    /// </summary>
    private IEnumerator SpawnWaves(int startFromWaveIndex = 0)
    {
        currentWave = startFromWaveIndex;

        // Wait until allowed to spawn first wave
        while (!canSpawnWave)
        {
            yield return null;
        }

        while (currentWave < numberOfWaves)
        {
            SpawnWave(currentWave);
            currentWave++;

            if (currentWave >= numberOfWaves)
                break;

            // Wait timeBetweenWaves seconds or until forced start interrupts by resetting coroutine
            float timer = 0f;
            while (timer < timeBetweenWaves)
            {
                timer += Time.deltaTime;
                yield return null;

                // If forced start interrupts, this coroutine will be stopped externally and restarted
            }
        }
    }

    private void SpawnWave(int waveIndex)
    {
        Debug.Log($"Spawning wave {waveIndex + 1} of {numberOfWaves}");

        for (int i = 0; i < enemyUnitPrefabs.Count; i++)
        {
            int baseCount = baseEnemyCounts[i];
            int countToSpawn = Mathf.CeilToInt(baseCount * Mathf.Pow(waveCountMultiplier, waveIndex));

            for (int spawnIndex = 0; spawnIndex < countToSpawn; spawnIndex++)
            {
                Vector2Int spawnGridPos = GetRandomEdgeGridPosition();

                GridNode node = gridManager.GetNode(spawnGridPos.x, spawnGridPos.y);

                if (node == null || !node.Walkable)
                {
                    // If chosen node is invalid, try a few times to find a valid edge node
                    bool foundValid = false;
                    for (int tries = 0; tries < 10; tries++)
                    {
                        spawnGridPos = GetRandomEdgeGridPosition();
                        node = gridManager.GetNode(spawnGridPos.x, spawnGridPos.y);
                        if (node != null && node.Walkable)
                        {
                            foundValid = true;
                            break;
                        }
                    }
                    if (!foundValid)
                    {
                        Debug.LogWarning("Could not find valid spawn position on outskirts for enemy.");
                        continue;
                    }
                }

                Vector3 spawnPos = node.WorldPosition + Vector3.up * 0.5f;
                Instantiate(enemyUnitPrefabs[i], spawnPos, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// Returns a random grid position along the outskirts (edges) of the grid.
    /// Outskirts means nodes on any edge: top, bottom, left, or right.
    /// </summary>
    private Vector2Int GetRandomEdgeGridPosition()
    {
        int gridX = gridManager.GridSettings.GridSizeX;
        int gridY = gridManager.GridSettings.GridSizeY;

        // Choose which edge to spawn on: 0=top,1=bottom,2=left,3=right
        int edge = Random.Range(0, 4);
        int x, y;

        switch (edge)
        {
            case 0: // Top row (y = max)
                x = Random.Range(0, gridX);
                y = gridY - 1;
                break;
            case 1: // Bottom row (y=0)
                x = Random.Range(0, gridX);
                y = 0;
                break;
            case 2: // Left column (x=0)
                x = 0;
                y = Random.Range(0, gridY);
                break;
            case 3: // Right column (x=max)
                x = gridX - 1;
                y = Random.Range(0, gridY);
                break;
            default:
                x = 0;
                y = 0;
                break;
        }

        return new Vector2Int(x, y);
    }
}
