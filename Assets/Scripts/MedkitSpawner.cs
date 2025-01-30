using UnityEngine;
using System.Collections.Generic;

public class MedkitSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject medkitPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int minMedkits = 1;
    [SerializeField] private int maxMedkits = 3;

    private List<GameObject> currentMedkits = new List<GameObject>();
    private ZombieSpawner zombieSpawner;

    private void Start()
    {
        zombieSpawner = FindObjectOfType<ZombieSpawner>();
        if (zombieSpawner == null)
        {
            Debug.LogError("ZombieSpawner not found in the scene!");
            return;
        }

        // Подписываемся на события окончания волны
        zombieSpawner.OnWaveCompleted += HandleWaveCompleted;
    }

    private void HandleWaveCompleted()
    {
        // Удаляем старые аптечки
        ClearMedkits();
        
        // Спауним новые
        SpawnMedkits();
    }

    private void SpawnMedkits()
    {
        int medkitsToSpawn = Random.Range(minMedkits, maxMedkits + 1);
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < medkitsToSpawn && availablePoints.Count > 0; i++)
        {
            // Выбираем случайную точку спауна
            int pointIndex = Random.Range(0, availablePoints.Count);
            Transform spawnPoint = availablePoints[pointIndex];
            availablePoints.RemoveAt(pointIndex); // Убираем использованную точку

            // Спауним аптечку
            GameObject medkit = Instantiate(medkitPrefab, spawnPoint.position, Quaternion.identity);
            currentMedkits.Add(medkit);
        }
    }

    private void ClearMedkits()
    {
        foreach (GameObject medkit in currentMedkits)
        {
            if (medkit != null)
            {
                Destroy(medkit);
            }
        }
        currentMedkits.Clear();
    }

    private void OnDestroy()
    {
        if (zombieSpawner != null)
        {
            zombieSpawner.OnWaveCompleted -= HandleWaveCompleted;
        }
        ClearMedkits();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.green;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.5f);
            }
        }
    }
#endif
}