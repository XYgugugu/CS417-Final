using System.Collections;
using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject coinPrefab;
    public Transform spawnPoint;

    [Header("Auto Spawn")]
    public bool autoSpawn = true;
    public float averageSpawnInterval = 1.0f;
    public float intervalJitter = 0.2f;

    [Header("Spawn Layout")]
    public int coinsPerSpawn = 1;
    public float randomRadius = 0.5f;
    public float spawnHeight = 0.05f;

    private Coroutine spawnRoutine;

    private void OnEnable()
    {
        if (autoSpawn)
        {
            spawnRoutine = StartCoroutine(AutoSpawnRoutine());
        }
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator AutoSpawnRoutine()
    {
        while (true)
        {
            float minTime = Mathf.Max(0.01f, averageSpawnInterval - intervalJitter);
            float maxTime = Mathf.Max(minTime, averageSpawnInterval + intervalJitter);
            float waitTime = Random.Range(minTime, maxTime);

            yield return new WaitForSeconds(waitTime);
            SpawnCoins();
        }
    }

    public void SpawnCoins()
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning("CoinSpawner: coinPrefab is not assigned.");
            return;
        }

        Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;

        for (int i = 0; i < coinsPerSpawn; i++)
        {
            Vector2 offset2D = Random.insideUnitCircle * randomRadius;
            Vector3 spawnPos = center + new Vector3(offset2D.x, spawnHeight, offset2D.y);

            Instantiate(coinPrefab, spawnPos, Quaternion.identity);
        }
    }
}