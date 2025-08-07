using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSpawner : MonoBehaviour
{
    [Header("Enemy Unit Prefabs (must have UnitController with UnitType)")]
    [SerializeField] private List<GameObject> enemyUnitPrefabs;

    [Header("Grid Manager Reference")]
    [SerializeField] private GridManager gridManager;

    [Header("Wave Settings")]
    [SerializeField] private int numberOfWaves = 5;
    [SerializeField] private float timeBetweenWaves = 10f;
    [SerializeField] private float waveCountMultiplier = 1.2f;
    [SerializeField] private List<int> baseEnemyCounts;

    [Header("UI References")]
    [SerializeField] private Button waveStartButton;

    private int currentWave = 0;
    private Camera mainCamera;
    private bool canSpawnWave = false;
    private Coroutine waveCoroutine;

    private int aliveEnemies = 0;

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
            baseEnemyCounts = new List<int>(new int[enemyUnitPrefabs.Count]);
            for (int i = 0; i < baseEnemyCounts.Count; i++)
            {
                baseEnemyCounts[i] = 1;
            }
        }

        waveCoroutine = StartCoroutine(SpawnWaves());
    }

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
            StopCoroutine(waveCoroutine);
            waveCoroutine = StartCoroutine(SpawnWaves(currentWave));
            Debug.Log($"Force started wave {currentWave + 1}!");
        }
    }

    private IEnumerator SpawnWaves(int startFromWaveIndex = 0)
    {
        currentWave = startFromWaveIndex;

        while (!canSpawnWave)
            yield return null;

        while (currentWave < numberOfWaves)
        {
            if (currentWave == numberOfWaves - 1 && waveStartButton != null)
            {
                waveStartButton.gameObject.SetActive(false);
            }

            SpawnWave(currentWave);
            currentWave++;

            if (currentWave >= numberOfWaves)
                break;

            float timer = 0f;
            while (timer < timeBetweenWaves)
            {
                timer += Time.deltaTime;
                yield return null;
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
                GameObject enemy = Instantiate(enemyUnitPrefabs[i], spawnPos, Quaternion.identity);

                UnitController controller = enemy.GetComponent<UnitController>();
                if (controller != null)
                {
                   // controller.OnDeath += HandleEnemyDeath;
                    aliveEnemies++;
                }
            }
        }

        Debug.Log($"Wave {waveIndex + 1} spawned with total enemies: {aliveEnemies}");
    }

   

    private Vector2Int GetRandomEdgeGridPosition()
    {
        int gridX = gridManager.GridSettings.GridSizeX;
        int gridY = gridManager.GridSettings.GridSizeY;

        int edge = Random.Range(0, 4);
        int x, y;

        switch (edge)
        {
            case 0: x = Random.Range(0, gridX); y = gridY - 1; break;
            case 1: x = Random.Range(0, gridX); y = 0; break;
            case 2: x = 0; y = Random.Range(0, gridY); break;
            case 3: x = gridX - 1; y = Random.Range(0, gridY); break;
            default: x = 0; y = 0; break;
        }

        return new Vector2Int(x, y);
    }
}
