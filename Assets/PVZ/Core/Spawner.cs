using UnityEngine;
using System.Collections;

namespace PVZ3D.Core
{
    public class Spawner : MonoBehaviour
    {
        [Header("References")]
        public GameObject prefab;
        public Transform spawnPoint;

        [Header("Auto Spawn")]
        public bool autoSpawn = false;
        public float averageSpawnInterval = 5.0f;
        public float intervalJitter = 10.0f;
        
        [Header("Spawn Layout")]
        public int numPerSpawn = 1;
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
                Spawn();
            }
        }

        public virtual void Spawn()
        {
            if (prefab == null)
            {
                Debug.LogWarning("Spawner: prefab is not assigned.");
                return;
            }

            Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;

            for (int i = 0; i < numPerSpawn; i++)
            {
                Vector2 offset2D = Random.insideUnitCircle * randomRadius;
                Vector3 spawnPos = center + new Vector3(offset2D.x, spawnHeight, offset2D.y);

                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
        }
    }
}