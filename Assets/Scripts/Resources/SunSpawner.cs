using PVZ3D.Core;
using PVZ3D.Grid;
using UnityEngine;

namespace PVZ3D.Resources
{
    public class SunSpawner : MonoBehaviour
    {
        public static SunSpawner Instance { get; private set; }

        [Header("Sun Spawn")]
        [Tooltip("Optional authored prefab. Runtime fallback sun visual is used if null.")]
        [SerializeField] private GameObject sunPickupPrefab;
        [SerializeField] private float passiveInterval = 5.4f;
        [SerializeField] private float spawnHeight = 1.3f;
        [SerializeField] private int passiveSunAmount = 20;
        [Tooltip("Optional override. If null, Runtime/Pickups is used.")]
        [SerializeField] private Transform pickupRuntimeParent;

        private float timer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            GamePhase phase = GameManager.Instance.State.Phase;
            if (phase != GamePhase.Battle && phase != GamePhase.Prep)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer >= passiveInterval)
            {
                timer = 0f;
                SpawnPassiveSun();
            }
        }

        public void SpawnPassiveSun()
        {
            LawnGridManager grid = LawnGridManager.Instance;
            if (grid == null)
            {
                return;
            }

            int lane = Random.Range(0, grid.Lanes);
            int col = Random.Range(0, grid.Columns);
            Vector3 position = grid.GetCellPosition(lane, col) + Vector3.up * spawnHeight;
            SpawnSun(position, passiveSunAmount);
        }

        public SunPickup SpawnSun(Vector3 position, int amount)
        {
            GameObject pickup = CreateSunObject(position);
            SunPickup sun = pickup.GetComponent<SunPickup>();
            if (sun == null)
            {
                sun = pickup.AddComponent<SunPickup>();
            }

            sun.Configure(amount);
            return sun;
        }

        public static SunPickup SpawnSunAt(Vector3 position, int amount)
        {
            return Instance != null ? Instance.SpawnSun(position, amount) : null;
        }

        public void ResetTimer()
        {
            timer = 0f;
        }

        private GameObject CreateSunObject(Vector3 position)
        {
            if (sunPickupPrefab != null)
            {
                Transform parent = ResolvePickupRuntimeParent();
                return Instantiate(sunPickupPrefab, position, Quaternion.identity, parent);
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.name = "SunPickup";
            fallback.transform.position = position;
            fallback.transform.localScale = Vector3.one * 0.28f;
            Renderer renderer = fallback.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(1f, 0.85f, 0.15f));
            }

            CreateSunRay(fallback.transform, new Vector3(0.4f, 0f, 0f), new Vector3(0.22f, 0.05f, 0.05f));
            CreateSunRay(fallback.transform, new Vector3(-0.4f, 0f, 0f), new Vector3(0.22f, 0.05f, 0.05f));
            CreateSunRay(fallback.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.05f, 0.22f, 0.05f));
            CreateSunRay(fallback.transform, new Vector3(0f, -0.4f, 0f), new Vector3(0.05f, 0.22f, 0.05f));

            Transform runtimeParent = ResolvePickupRuntimeParent();
            fallback.transform.SetParent(runtimeParent, true);

            return fallback;
        }

        private Transform ResolvePickupRuntimeParent()
        {
            if (pickupRuntimeParent != null)
            {
                return pickupRuntimeParent;
            }

            GameObject root = GameObject.Find("Runtime/Pickups");
            if (root == null)
            {
                GameObject runtime = GameObject.Find("Runtime") ?? new GameObject("Runtime");
                root = new GameObject("Pickups");
                root.transform.SetParent(runtime.transform, false);
            }

            pickupRuntimeParent = root.transform;
            return pickupRuntimeParent;
        }

        private static void CreateSunRay(Transform parent, Vector3 localPos, Vector3 localScale)
        {
            GameObject ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ray.transform.SetParent(parent, false);
            ray.transform.localPosition = localPos;
            ray.transform.localScale = localScale;
            Renderer renderer = ray.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(1f, 0.93f, 0.52f));
            }

            Collider col = ray.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }
}
