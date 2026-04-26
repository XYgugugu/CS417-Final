using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Grid;
using PVZ3D.Plants;
using UnityEngine;

namespace PVZ3D.Zombies
{
    // public class ZombieBase : MonoBehaviour
    // {
    //     private static readonly HashSet<ZombieBase> ActiveZombies = new HashSet<ZombieBase>();

    //     [Header("Stats")]
    //     [SerializeField] private float maxHealth = 100f;
    //     [SerializeField] private float moveSpeed = 0.7f;
    //     [SerializeField] private float attackDamage = 12f;
    //     [SerializeField] private float attackInterval = 1.0f;
    //     [SerializeField] private int coinReward = 5;
    //     [SerializeField] private int baseDamage = 1;

    //     [Header("Runtime")]
    //     [SerializeField] private int lane;
    //     [SerializeField] private float currentHealth;
    //     [SerializeField] private float baseTargetX;

    //     private ZombieMovement movement;
    //     private ZombieAttack attack;

    //     public int Lane => lane;
    //     public float MoveSpeed => moveSpeed;
    //     public float AttackDamage => attackDamage;
    //     public float AttackInterval => attackInterval;
    //     public float BaseTargetX => baseTargetX;
    //     public bool IsDead { get; private set; }

    //     private void OnEnable()
    //     {
    //         ActiveZombies.Add(this);
    //     }

    //     private void OnDisable()
    //     {
    //         ActiveZombies.Remove(this);
    //     }

    //     private void Awake()
    //     {
    //         movement = GetComponent<ZombieMovement>();
    //         if (movement == null)
    //         {
    //             movement = gameObject.AddComponent<ZombieMovement>();
    //         }

    //         movement.Initialize(this);

    //         attack = GetComponent<ZombieAttack>();
    //         if (attack == null)
    //         {
    //             attack = gameObject.AddComponent<ZombieAttack>();
    //         }

    //         attack.Initialize(this);
    //         currentHealth = maxHealth;
    //     }

    //     private void Update()
    //     {
    //         if (IsDead)
    //         {
    //             return;
    //         }

    //         GamePhase phase = GameManager.Instance != null ? GameManager.Instance.State.Phase : GamePhase.Menu;
    //         if (phase != GamePhase.Prep && phase != GamePhase.Battle)
    //         {
    //             return;
    //         }

    //         movement.Tick(Time.deltaTime);
    //         attack.Tick(Time.deltaTime);
    //     }

    //     public void Configure(int laneIndex, float health, float speed, float damage, float hitInterval, int reward, int baseHit)
    //     {
    //         lane = laneIndex;
    //         maxHealth = Mathf.Max(1f, health);
    //         currentHealth = maxHealth;
    //         moveSpeed = Mathf.Max(0.1f, speed);
    //         attackDamage = Mathf.Max(1f, damage);
    //         attackInterval = Mathf.Max(0.2f, hitInterval);
    //         coinReward = Mathf.Max(0, reward);
    //         baseDamage = Mathf.Max(1, baseHit);
    //         baseTargetX = LawnGridManager.Instance != null
    //             ? LawnGridManager.Instance.GetBasePositionForLane(lane).x
    //             : -1.5f;
    //     }

    //     public void TakeDamage(float amount)
    //     {
    //         if (IsDead || amount <= 0f)
    //         {
    //             return;
    //         }

    //         currentHealth -= amount;
    //         if (currentHealth <= 0f)
    //         {
    //             Die(true, true);
    //         }
    //     }

    //     public void SetAttackTarget(PlantBase plant)
    //     {
    //         attack.SetTarget(plant);
    //     }

    //     public void ClearAttackTarget()
    //     {
    //         attack.ClearTarget();
    //     }

    //     public void ReachBase()
    //     {
    //         if (IsDead)
    //         {
    //             return;
    //         }

    //         SpawnBaseHitFlash();
    //         GameManager.Instance?.DamageBase(baseDamage);
    //         Die(false, false);
    //     }

    //     public void Die(bool grantReward, bool countAsKill)
    //     {
    //         if (IsDead)
    //         {
    //             return;
    //         }

    //         IsDead = true;
    //         if (grantReward)
    //         {
    //             ResourceManager.Instance?.AddCoins(coinReward, true);
    //         }

    //         SpawnDeathBurst();
    //         GameManager.Instance?.RegisterZombieRemoved(lane, countAsKill);
    //         Destroy(gameObject);
    //     }

    //     public static ZombieBase GetFirstAliveInLaneAhead(int laneIndex, float xPosition, float maxDistance)
    //     {
    //         ZombieBase best = null;
    //         float bestDistance = float.PositiveInfinity;

    //         foreach (ZombieBase zombie in ActiveZombies)
    //         {
    //             if (zombie == null || zombie.IsDead || zombie.lane != laneIndex)
    //             {
    //                 continue;
    //             }

    //             float dx = zombie.transform.position.x - xPosition;
    //             if (dx <= 0f || dx > maxDistance)
    //             {
    //                 continue;
    //             }

    //             if (dx < bestDistance)
    //             {
    //                 bestDistance = dx;
    //                 best = zombie;
    //             }
    //         }

    //         return best;
    //     }

    //     public static void DestroyAllZombies()
    //     {
    //         ZombieBase[] zombies = new ZombieBase[ActiveZombies.Count];
    //         ActiveZombies.CopyTo(zombies);

    //         foreach (ZombieBase zombie in zombies)
    //         {
    //             if (zombie != null)
    //             {
    //                 zombie.Die(false, false);
    //             }
    //         }
    //     }

    //     private void SpawnDeathBurst()
    //     {
    //         GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    //         burst.transform.position = transform.position + Vector3.up * 0.8f;
    //         burst.transform.localScale = Vector3.one * 0.24f;
    //         Renderer renderer = burst.GetComponent<Renderer>();
    //         if (renderer != null)
    //         {
    //             RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(0.82f, 0.3f, 0.3f));
    //         }

    //         Collider col = burst.GetComponent<Collider>();
    //         if (col != null)
    //         {
    //             col.enabled = false;
    //         }

    //         Destroy(burst, 0.2f);
    //     }

    //     private void SpawnBaseHitFlash()
    //     {
    //         Vector3 pos = transform.position;
    //         if (LawnGridManager.Instance != null)
    //         {
    //             pos = LawnGridManager.Instance.GetBasePositionForLane(lane) + new Vector3(0.35f, 0.45f, 0f);
    //         }

    //         GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //         flash.transform.position = pos;
    //         flash.transform.localScale = new Vector3(0.16f, 0.6f, 1.25f);
    //         Renderer renderer = flash.GetComponent<Renderer>();
    //         if (renderer != null)
    //         {
    //             RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(0.95f, 0.35f, 0.3f));
    //         }

    //         Collider col = flash.GetComponent<Collider>();
    //         if (col != null)
    //         {
    //             col.enabled = false;
    //         }

    //         Destroy(flash, 0.16f);
    //     }
    // }
}