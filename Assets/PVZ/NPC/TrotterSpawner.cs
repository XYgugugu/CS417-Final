using UnityEngine;
using PVZ3D.Core;

namespace PVZ3D.NPC
{
    public class TrotterSpawner : Spawner
    {
        [Header("Target")]
        public Transform player;
        public int maxSpawn = 10;
        private int numSpawned = 0;

        private void Awake()
        {
            numSpawned = 0;

            if (player == null)
            {
                GameObject xrPlayer = GameObject.Find("XR-Player");
                if (xrPlayer != null)
                {
                    player = xrPlayer.transform;
                }
            }

            if (player == null)
            {
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null)
                {
                    player = taggedPlayer.transform;
                }
            }

            if (player == null)
            {
                Destroy(gameObject);
                return;
            }

            prefab = Resources.Load<GameObject>("Trotter");

            if (prefab == null)
            {
                Debug.LogError("TrotterSpawner: Could not load Trotter.prefab from Resources.");
            }
        }

        public override void Spawn()
        {
            if (prefab == null)
            {
                Debug.LogWarning("TrotterSpawner: prefab is not assigned.");
                return;
            }

            Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;

            for (int i = 0; i < numPerSpawn; i++)
            {
                Vector2 offset2D = Random.insideUnitCircle * randomRadius;
                Vector3 spawnPos = center + new Vector3(offset2D.x, spawnHeight, offset2D.y);

                GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);

                Trotter trotter = spawned.GetComponent<Trotter>();
                if (trotter != null)
                {
                    trotter.player = player;
                }
                else
                {
                    Debug.LogWarning("TrotterSpawner: Spawned prefab does not have a Trotter component.");
                }

                numSpawned += 1;
                if (numSpawned == maxSpawn)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
