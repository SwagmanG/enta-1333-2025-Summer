using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSpawner : MonoBehaviour
{
    // Enemy unit prefabs - these are the units that will spawn during waves
    [Header("Enemy Unit Prefabs (must have UnitController with UnitType)")]
    [SerializeField] private List<GameObject> EnemyUnitPrefabs;

    // Reference to the grid system for spawn positioning
    [Header("Grid Manager Reference")]
    [SerializeField] private GridManager GridManager;

    // Configuration for how waves work and scale over time
    [Header("Wave Settings")]
    [SerializeField] private int NumberOfWaves = 5;           // Total waves in the game
    [SerializeField] private float TimeBetweenWaves = 10f;    // How long to wait between waves
    [SerializeField] private float WaveCountMultiplier = 1.2f; // How much enemy count increases each wave
    [SerializeField] private List<int> BaseEnemyCounts;       // Starting count for each enemy type

    // UI element that lets players manually start waves
    [Header("UI References")]
    [SerializeField] private Button WaveStartButton;

    // Internal state tracking for the wave system
    private int currentWave = 0;           // Which wave we're currently on
    private Camera mainCamera;             // Reference to main camera
    private bool canSpawnWave = false;     // Whether we're allowed to spawn the next wave
    private Coroutine waveCoroutine;       // Coroutine handle for wave management
    private int aliveEnemies = 0;          // Count of enemies currently alive

    private void Start()
    {
        // === INITIALIZATION ===
        // Set up the spawner and validate configuration

        mainCamera = Camera.main;

        // Make sure we have enemy prefabs to spawn
        if (EnemyUnitPrefabs == null || EnemyUnitPrefabs.Count == 0)
        {
            Debug.LogError("UnitSpawner: No enemy unit prefabs assigned!");
            enabled = false;
            return;
        }

        // Auto-generate base enemy counts if not properly configured
        // This ensures we have a count for each enemy type
        if (BaseEnemyCounts == null || BaseEnemyCounts.Count != EnemyUnitPrefabs.Count)
        {
            BaseEnemyCounts = new List<int>(new int[EnemyUnitPrefabs.Count]);
            for (int i = 0; i < BaseEnemyCounts.Count; i++)
            {
                BaseEnemyCounts[i] = 1;
            }
        }

        // Start the wave spawning system
        waveCoroutine = StartCoroutine(SpawnWaves());
    }

    // === WAVE CONTROL METHODS ===
    // Handle manual wave starting and management

    // Allow players or other systems to force start the current wave
    public void ForceStartCurrentWave()
    {
        Debug.Log("Wave Start");

        if (!canSpawnWave)
        {
            canSpawnWave = true;
            Debug.Log("Force started first wave!");
            StartCoroutine(CountdownToNextWave(currentWave)); // start countdown with optional manual trigger

        }
        else
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = StartCoroutine(SpawnWaves(currentWave));
            Debug.Log($"Force started wave {currentWave + 1}!");
        }
    }

    // === WAVE SPAWNING SYSTEM ===
    // Core coroutine that manages wave timing and progression

    // Main wave management coroutine - handles timing and progression
    private IEnumerator SpawnWaves(int startFromWaveIndex = 0)
    {
        currentWave = startFromWaveIndex;

        // Wait until we're allowed to start spawning waves
        while (!canSpawnWave)
            yield return null;

        // Spawn each wave in sequence
        while (currentWave < NumberOfWaves)
        {
            // Hide the wave start button on the final wave
            if (currentWave == NumberOfWaves - 1 && WaveStartButton != null)
            {
                WaveStartButton.gameObject.SetActive(false);
            }

            // Spawn the current wave
            SpawnWave(currentWave);
            currentWave++;

            // Break if we've completed all waves
            if (currentWave >= NumberOfWaves)
                break;


            // Start next countdown through UI Manager
            GameUiManager.Instance.StartWaveCountdown(currentWave);

            // Wait until ForceStartCurrentWave is called again
            yield break;

           
           
        }
    }

    // Spawn all enemies for a specific wave
    private void SpawnWave(int waveIndex)
    {
        Debug.Log($"Spawning wave {waveIndex + 1} of {NumberOfWaves}");

        // Spawn each type of enemy unit according to the wave scaling
        for (int i = 0; i < EnemyUnitPrefabs.Count; i++)
        {
            int baseCount = BaseEnemyCounts[i];
            // Calculate how many of this enemy type to spawn based on wave progression
            int countToSpawn = Mathf.CeilToInt(baseCount * Mathf.Pow(WaveCountMultiplier, waveIndex));

            // Spawn each individual enemy of this type
            for (int spawnIndex = 0; spawnIndex < countToSpawn; spawnIndex++)
            {
                // Find a valid spawn position on the grid edges
                Vector2Int spawnGridPos = GetRandomEdgeGridPosition();
                GridNode node = GridManager.GetNode(spawnGridPos.x, spawnGridPos.y);

                // Make sure we found a valid, walkable spawn position
                if (node == null || !node.Walkable)
                {
                    // Try multiple times to find a valid spawn spot
                    bool foundValid = false;
                    for (int tries = 0; tries < 10; tries++)
                    {
                        spawnGridPos = GetRandomEdgeGridPosition();
                        node = GridManager.GetNode(spawnGridPos.x, spawnGridPos.y);
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

                // Spawn the enemy slightly above the ground to prevent embedding
                Vector3 spawnPos = node.WorldPosition + Vector3.up * 0.5f;
                GameObject enemy = Instantiate(EnemyUnitPrefabs[i], spawnPos, Quaternion.identity);

                // Set up the enemy controller and track it
                UnitController controller = enemy.GetComponent<UnitController>();
                if (controller != null)
                {
                    // controller.OnDeath += HandleEnemyDeath; // Commented out - would handle enemy death tracking
                    aliveEnemies++;
                }
            }
        }

        Debug.Log($"Wave {waveIndex + 1} spawned with total enemies: {aliveEnemies}");
    }

    // Starts a countdown and allows player to manually trigger wave early
    private IEnumerator CountdownToNextWave(int waveIndex)
    {
        float countdownTime = 60f + (waveIndex * 30f); // 1:00, 1:30, etc.
        bool waveStarted = false;

        void ManualStart()
        {
            if (!waveStarted)
            {
                waveStarted = true;
                if (WaveStartButton != null)
                    WaveStartButton.onClick.RemoveListener(ManualStart);

                StopCoroutine(nameof(CountdownToNextWave));
                waveCoroutine = StartCoroutine(SpawnWaves(waveIndex));
            }
        }

        if (WaveStartButton != null)
        {
            WaveStartButton.gameObject.SetActive(true);
            WaveStartButton.onClick.AddListener(ManualStart);
        }

        GameUiManager.Instance.StartWaveCountdown(waveIndex);

        while (countdownTime > 0f && !waveStarted)
        {
            countdownTime -= Time.deltaTime;
            yield return null;
        }

        if (!waveStarted)
        {
            if (WaveStartButton != null)
                WaveStartButton.onClick.RemoveListener(ManualStart);

            waveCoroutine = StartCoroutine(SpawnWaves(waveIndex));
        }
    }


    // === SPAWN POSITION UTILITIES ===
    // Get a random position along the edges of the grid for enemy spawning
    // This ensures enemies spawn from the borders and move inward
    private Vector2Int GetRandomEdgeGridPosition()
    {
        int gridX = GridManager.GridSettings.GridSizeX;
        int gridY = GridManager.GridSettings.GridSizeY;

        // Pick a random edge: 0=top, 1=bottom, 2=left, 3=right
        int edge = Random.Range(0, 4);
        int x, y;

        switch (edge)
        {
            case 0: x = Random.Range(0, gridX); y = gridY - 1; break; // Top edge
            case 1: x = Random.Range(0, gridX); y = 0; break;         // Bottom edge
            case 2: x = 0; y = Random.Range(0, gridY); break;         // Left edge
            case 3: x = gridX - 1; y = Random.Range(0, gridY); break; // Right edge
            default: x = 0; y = 0; break;                             // Fallback
        }

        return new Vector2Int(x, y);
    }
}