using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class ZombieSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject[] zombiePrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private WaveManager waveUI;

    [Header("Wave Settings")]
    [SerializeField] private int[] zombiesPerWave = { 10, 15, 20, 25, 30 };
    [SerializeField] private float waveCooldown = 15f;
    [SerializeField] private float initialSpawnDelay = 3f;

    [Header("Spawn Settings")]
    [SerializeField] private float initialSpawnInterval = 2f;
    [SerializeField] private float minimumSpawnInterval = 0.5f;
    [SerializeField] private float spawnIntervalDecrement = 0.2f;

    private HashSet<ZombieAI> activeZombies = new HashSet<ZombieAI>();
    private Coroutine waveCoroutine;
    private int currentWave;
    private int zombiesKilled;
    private float currentSpawnInterval;
    private bool isSpawning;
    private bool isWaitingForNextWave;

    public event Action OnWaveCompleted;

    private void Start()
    {
        InitializeSpawner();
    }

    private void InitializeSpawner()
    {
        ValidateComponents();
        currentSpawnInterval = initialSpawnInterval;
        currentWave = 0;
        SubscribeToEvents();
        StartNextWave();
    }

    private void ValidateComponents()
    {
        if (zombiePrefabs == null || zombiePrefabs.Length == 0)
            Debug.LogError("Zombie prefabs not assigned!");

        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogError("Spawn points not assigned!");

        if (waveUI == null)
            Debug.LogError("Wave UI Controller not assigned!");
    }

    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += PauseSpawner;
            GameManager.Instance.OnGameResumed += ResumeSpawner;
            GameManager.Instance.OnGameOver += StopSpawner;
        }
    }

    private void StartNextWave()
    {
        if (isSpawning || isWaitingForNextWave || currentWave >= zombiesPerWave.Length)
            return;

        currentWave++;
        zombiesKilled = 0;

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }

        waveCoroutine = StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        isSpawning = true;
        int zombiesToSpawn = zombiesPerWave[currentWave - 1];

        // Показываем UI новой волны
        waveUI.SetUIVisibility(true);
        waveUI.ShowNewWave(currentWave);
        waveUI.UpdateWaveProgress(zombiesKilled, zombiesToSpawn);

        yield return new WaitForSeconds(initialSpawnDelay);

        // Спавним зомби
        for (int i = 0; i < zombiesToSpawn && GameManager.Instance.IsGameActive(); i++)
        {
            if (!GameManager.Instance.isGamePaused)
            {
                SpawnZombie();
                yield return new WaitForSeconds(currentSpawnInterval);
            }
            else
            {
                yield return null;
            }
        }

        isSpawning = false;

        while (activeZombies.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        OnWaveCompleted?.Invoke();

        if (currentWave < zombiesPerWave.Length)
        {
            isWaitingForNextWave = true;
            StartCoroutine(WaveCooldownRoutine());
        }
        else
        {
            waveUI.SetUIVisibility(false);
            GameManager.Instance.WinGame();
        }
    }

    private IEnumerator WaveCooldownRoutine()
    {
        float timeLeft = waveCooldown;
        
        while (timeLeft > 0)
        {
            if (!GameManager.Instance.isGamePaused)
            {
                timeLeft -= Time.deltaTime;
                waveUI.ShowWaveCooldown(timeLeft);
                yield return null;
            }
            else
            {
                yield return null;
            }
        }

        isWaitingForNextWave = false;
        AdjustDifficulty();
        StartNextWave();
    }

    private void SpawnZombie()
    {
        if (spawnPoints.Length == 0 || zombiePrefabs.Length == 0)
            return;

        Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        GameObject zombiePrefab = zombiePrefabs[UnityEngine.Random.Range(0, zombiePrefabs.Length)];

        GameObject zombieObject = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        ZombieAI zombie = zombieObject.GetComponent<ZombieAI>();

        if (zombie != null)
        {
            zombie.Initialize(GameManager.Instance.GetPlayer());
            zombie.OnZombieDeath += HandleZombieDeath;
            activeZombies.Add(zombie);
        }
    }

    private void HandleZombieDeath(ZombieAI zombie)
    {
        if (activeZombies.Remove(zombie))
        {
            zombiesKilled++;
            GameManager.Instance.AddScore(100);
            waveUI.UpdateWaveProgress(zombiesKilled, zombiesPerWave[currentWave - 1]);
        }
    }

    private void AdjustDifficulty()
    {
        currentSpawnInterval = Mathf.Max(
            minimumSpawnInterval,
            initialSpawnInterval - (spawnIntervalDecrement * (currentWave - 1))
        );
    }

    private void PauseSpawner()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }
    }

    private void ResumeSpawner()
    {
        if (isSpawning && !isWaitingForNextWave)
        {
            waveCoroutine = StartCoroutine(WaveRoutine());
        }
    }

    private void StopSpawner()
    {
        StopAllCoroutines();
        foreach (var zombie in activeZombies)
        {
            if (zombie != null)
                Destroy(zombie.gameObject);
        }
        activeZombies.Clear();
        isSpawning = false;
        isWaitingForNextWave = false;
        waveUI.SetUIVisibility(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused -= PauseSpawner;
            GameManager.Instance.OnGameResumed -= ResumeSpawner;
            GameManager.Instance.OnGameOver -= StopSpawner;
        }
    }
}