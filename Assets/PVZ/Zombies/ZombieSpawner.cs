using UnityEngine;
using System;
using System.Collections;
using PVZ3D.Core;

namespace PVZ3D.Zombies
{
    public class ZombieSpawner : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject firstZombie;
        [SerializeField] private GameObject[] zombiePrefabs;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private int totalWaves = 3;
        [SerializeField] private int zombiesPerWave = 10;
        [SerializeField] private float wavesInterval = 15f;
        [SerializeField] private AudioSource waveWarningAudio;
        [SerializeField] private float spawnYOffset = 0.1f;

        private bool hasSpawnedFirstZombie;

        public static event Action OnSpawnerStarted;
        public static event Action<int, int> OnWaveStarted;
        public static event Action OnAllWavesFinished;

        public int TotalWaves => totalWaves;

        private void Start()
        {
            ResolveGameManager()?.SetWaveProgress(0, totalWaves);
            StartCoroutine(SpawnWaves());
        }

        private IEnumerator SpawnWaves()
        {
            OnSpawnerStarted?.Invoke();
            yield return new WaitForSeconds(8f);

            for (int wave = 1; wave <= totalWaves; wave++)
            {
                if (waveWarningAudio != null)
                {
                    waveWarningAudio.volume = 0.2f;
                    waveWarningAudio.Play();
                }
                yield return new WaitForSeconds(5f);

                ResolveGameManager()?.SetWaveProgress(wave, totalWaves);
                Debug.Log("Wave " + wave + " starting!");
                OnWaveStarted?.Invoke(wave, totalWaves);

                for (int i = 0; i < zombiesPerWave; i++)
                {
                    SpawnZombie();
                    yield return new WaitForSeconds(spawnInterval);
                }

                yield return new WaitForSeconds(wavesInterval);
            }

            Debug.Log("All waves finished!");
            OnAllWavesFinished?.Invoke();

            while (FindObjectsByType<ZombieBase>(FindObjectsSortMode.None).Length > 0)
            {
                yield return null;
            }

            ResolveGameManager()?.ClearLevel();
        }

        private void SpawnZombie()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return;

            GameObject prefabToSpawn = GetZombiePrefab();
            if (prefabToSpawn == null) return;

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Vector3 spawnPos = spawnPoint.position + new Vector3(0f, spawnYOffset, 0f);

            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        }

        public void SpawnZombies(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnZombie();
            }
        }

        private GameObject GetZombiePrefab()
        {
            if (!hasSpawnedFirstZombie && firstZombie != null)
            {
                hasSpawnedFirstZombie = true;
                return firstZombie;
            }

            if (zombiePrefabs == null || zombiePrefabs.Length == 0)
            {
                return null;
            }

            return zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        }

        private GameManager ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager;
        }
    }
}
