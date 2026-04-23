using PVZ3D.Core;
using PVZ3D.Grid;
using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.Waves
{
    public class ZombieSpawner : MonoBehaviour
    {
        public static ZombieSpawner Instance { get; private set; }

        [Header("Prefabs")]
        [Tooltip("Optional authored prefab. Runtime fallback visuals are used if null.")]
        [SerializeField] private GameObject basicZombiePrefab;
        [Tooltip("Optional authored prefab. Runtime fallback visuals are used if null.")]
        [SerializeField] private GameObject toughZombiePrefab;
        [Tooltip("Optional override. If null, Runtime/Zombies is used.")]
        [SerializeField] private Transform zombieRuntimeParent;

        [Header("Stats")]
        [SerializeField] private float basicHealth = 78f;
        [SerializeField] private float basicSpeed = 0.4f;
        [SerializeField] private float basicDamage = 12f;
        [SerializeField] private int basicCoinsReward = 6;

        [SerializeField] private float toughHealth = 155f;
        [SerializeField] private float toughSpeed = 0.3f;
        [SerializeField] private float toughDamage = 18f;
        [SerializeField] private int toughCoinsReward = 12;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public ZombieBase SpawnZombie(int lane, bool tough)
        {
            if (LawnGridManager.Instance == null)
            {
                return null;
            }

            if (zombieRuntimeParent == null)
            {
                GameObject root = GameObject.Find("Runtime/Zombies");
                if (root == null)
                {
                    GameObject runtime = GameObject.Find("Runtime") ?? new GameObject("Runtime");
                    root = new GameObject("Zombies");
                    root.transform.SetParent(runtime.transform, false);
                }

                zombieRuntimeParent = root.transform;
            }

            Vector3 spawnPos = LawnGridManager.Instance.GetZombieSpawnPosition(lane) + Vector3.up * 0.55f;
            GameObject zombieObj = CreateZombieObject(spawnPos, tough);
            zombieObj.transform.SetParent(zombieRuntimeParent, true);
            zombieObj.name = tough ? "Zombie_Tough" : "Zombie_Basic";

            ZombieBase zombie = zombieObj.GetComponent<ZombieBase>();
            if (zombie == null)
            {
                zombie = zombieObj.AddComponent<ZombieBase>();
            }

            if (tough)
            {
                zombie.Configure(lane, toughHealth, toughSpeed, toughDamage, 1.1f, toughCoinsReward, 1);
            }
            else
            {
                zombie.Configure(lane, basicHealth, basicSpeed, basicDamage, 1f, basicCoinsReward, 1);
            }

            GameManager.Instance?.RegisterZombieSpawned(lane);
            return zombie;
        }

        private GameObject CreateZombieObject(Vector3 spawnPos, bool tough)
        {
            GameObject prefab = tough ? toughZombiePrefab : basicZombiePrefab;
            if (prefab != null)
            {
                return Instantiate(prefab, spawnPos, Quaternion.identity);
            }

            GameObject fallback = new GameObject("ZombieFallback");
            fallback.transform.position = spawnPos;
            fallback.transform.localScale = Vector3.one;

            CapsuleCollider col = fallback.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.height = 1.4f;
            col.radius = tough ? 0.38f : 0.34f;

            BuildZombieVisual(fallback.transform, tough);

            return fallback;
        }

        private static void BuildZombieVisual(Transform root, bool tough)
        {
            Color body = tough ? new Color(0.43f, 0.16f, 0.15f) : new Color(0.32f, 0.44f, 0.36f);
            Color head = tough ? new Color(0.58f, 0.37f, 0.35f) : new Color(0.64f, 0.52f, 0.42f);
            Color accents = tough ? new Color(0.14f, 0.14f, 0.16f) : new Color(0.17f, 0.19f, 0.2f);

            CreateVisualPart(root, PrimitiveType.Capsule, new Vector3(0f, 0.65f, 0f), tough ? new Vector3(0.88f, 1.18f, 0.88f) : new Vector3(0.78f, 1.1f, 0.78f), body);
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 1.35f, 0f), new Vector3(0.44f, 0.44f, 0.44f), head);
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(0.22f, 0.95f, 0f), new Vector3(0.12f, 0.38f, 0.12f), accents);
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(-0.22f, 0.95f, 0f), new Vector3(0.12f, 0.38f, 0.12f), accents);

            if (tough)
            {
                CreateVisualPart(root, PrimitiveType.Cube, new Vector3(0f, 1.02f, 0.08f), new Vector3(0.48f, 0.2f, 0.16f), new Color(0.2f, 0.2f, 0.24f));
            }
        }

        private static GameObject CreateVisualPart(Transform parent, PrimitiveType primitive, Vector3 localPos, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;
            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, color);
            }

            Collider col = part.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            return part;
        }
    }
}
