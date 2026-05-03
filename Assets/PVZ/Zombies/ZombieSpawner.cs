using UnityEngine;
using System.Collections;
using PVZ3D.Core;

namespace PVZ3D.Zombies
{
 public class ZombieSpawner : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject firstZombie;
        [SerializeField] private GameObject[] zombiePrefabs; 
        [SerializeField] public Transform[] spawnPoints;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private int totalWaves = 3;
        [SerializeField] private int zombiesPerWave = 10;
        [SerializeField] private float WavesInterval = 15f;
        [SerializeField] private AudioSource waveWarningAudio;
        [SerializeField] private float spawnYOffset = 0.1f;
        private bool isFirstZombie = true;

        void Start()
        {
            ResolveGameManager()?.SetWaveProgress(0, totalWaves);
            StartCoroutine(SpawnWaves());
        }

        private IEnumerator SpawnWaves()
        {
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

                for (int i = 0; i < zombiesPerWave; i++)
                {
                    SpawnZombie();
                    yield return new WaitForSeconds(spawnInterval);
                }

                yield return new WaitForSeconds(WavesInterval);
            }

            Debug.Log("All waves finished!");
        }

        private void SpawnZombie()
        {
            if (spawnPoints.Length == 0) return;

            GameObject prefabToSpawn;

            if (isFirstZombie && firstZombie != null)
            {
                prefabToSpawn = firstZombie;
                isFirstZombie = false;
            }
            else
            {
                if (zombiePrefabs.Length == 0) return;
                prefabToSpawn = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
            }

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Vector3 spawnPos = spawnPoint.position + new Vector3(0f, spawnYOffset, 0f);

            Instantiate(
                prefabToSpawn,
                spawnPos,
                Quaternion.identity
            );
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
