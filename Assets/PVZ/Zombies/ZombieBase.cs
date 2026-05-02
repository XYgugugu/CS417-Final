using UnityEngine;
using System.Collections;
using PVZ3D.Core;
using PVZ3D.Region;
using PVZ3D.Plants;

namespace PVZ3D.Zombies
{
    public class ZombieBase : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float baseHealth = 100f;
        [SerializeField] private float shieldHealth = 0f;
        [SerializeField] private bool hasShield = false;
        // [SerializeField] private GameObject replacementZombie;

        [Header("Attack")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackInterval = 1f;

        [Header("Loot Drop")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private int coinDropAmount = 1;

        [Header("Attack Juice")]
        [SerializeField] private float attackDistance = 0.08f;
        [SerializeField] private float attackSpeed = 8f;

        [Header("Audio")]
        [SerializeField] private AudioSource walkAudio;
        [SerializeField] private AudioSource attackAudio;
        [SerializeField] private AudioSource deathAudio;

        private float currentHealth;
        private float currentShieldHealth;
        private float attackTimer = 0f;
        private bool isAttacking = false;
        private PathFollow movement;
        private PlantBase targetPlant;
        private float dropOffsetY = 0.3f;

        private void Awake()
        {
            EnsureRuntimeSetup();
        }

        void Start()
        {
            currentHealth = baseHealth;
            currentShieldHealth = shieldHealth;
            movement = GetComponent<PathFollow>();
        }

        void Update()
        {
            HandleWalkAudio();
            ZombieAttack();
        }

        private void HandleWalkAudio()
        {
            if (movement == null || walkAudio == null) return;

            if (!movement.IsStopped() && !walkAudio.isPlaying)
            {
                walkAudio.volume = 0.3f;
                walkAudio.Play();
            }
            else if (movement.IsStopped() && walkAudio.isPlaying)
            {
                walkAudio.Stop();
            }
        }

        private void ZombieAttack()
        {   
             if (targetPlant == null)
            {
                if (movement != null)
                {
                    movement.ResumeMoving();
                }
                return;
            }

            movement?.StopMoving();

            attackTimer += Time.deltaTime;

            if (attackTimer >= attackInterval)
            {
                targetPlant.TakeDamage(attackDamage);

                if (attackAudio != null)
                {
                    attackAudio.volume = 0.7f;
                    attackAudio.Play();
                }
                StartCoroutine(AttackJuice());

                attackTimer = 0f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlantBase plant = other.GetComponentInParent<PlantBase>();

            if (plant != null)
            {
                targetPlant = plant;
                attackTimer = 0f;
                movement?.StopMoving();
            }
        }

        private void EnsureRuntimeSetup()
        {
            if (!CompareTag("Zombie"))
            {
                gameObject.tag = "Zombie";
            }

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.isKinematic = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            Collider hitbox = GetComponent<Collider>();
            if (hitbox == null)
            {
                CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                hitbox = capsule;
            }

            CapsuleCollider capsuleHitbox = hitbox as CapsuleCollider;
            if (capsuleHitbox != null)
            {
                capsuleHitbox.radius = Mathf.Max(capsuleHitbox.radius, 0.65f);
                capsuleHitbox.height = Mathf.Max(capsuleHitbox.height, 1.8f);
                capsuleHitbox.direction = 1;
                capsuleHitbox.center = new Vector3(0f, 0.65f, 0f);
            }

            hitbox.isTrigger = true;
        }

        private IEnumerator AttackJuice()
        {
            if (isAttacking) yield break;

            isAttacking = true;

            Vector3 start = transform.localPosition;
            Vector3 forward = start + Vector3.forward * attackDistance;

            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * attackSpeed;
                transform.localPosition = Vector3.Lerp(start, forward, t);
                yield return null;
            }

            t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * attackSpeed;
                transform.localPosition = Vector3.Lerp(forward, start, t);
                yield return null;
            }

            transform.localPosition = start;
            isAttacking = false;
        }

        public void TakeDamage(float damage)
        {
            if (hasShield && currentShieldHealth > 0)
            {
                if (currentShieldHealth - damage <= 0)
                {
                    float carryOverDamage = damage - currentShieldHealth;
                    currentShieldHealth = 0f;
                    hasShield = false;
                    currentHealth -= carryOverDamage;
                    // ReplaceWithBaseZombie(carryOverDamage);
                    return;
                }

                currentShieldHealth -= damage;
                return;
            }

            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        // private void ReplaceWithBaseZombie(float carryOverDamage)
        // {
        //     // Destory original zombie and replace with office worker zombie
        //     Vector3 currentPosition = transform.position;

        //     GameObject newZombie = Instantiate(
        //         replacementZombie,
        //         currentPosition,
        //         Quaternion.identity
        //     );

        //     ZombieBase newZombieBase = newZombie.GetComponent<ZombieBase>();

        //     if (newZombieBase != null && carryOverDamage > 0)
        //     {
        //         newZombieBase.TakeDamage(carryOverDamage);
        //     }

        //     Destroy(gameObject);
        // }

        private void Die()
        {
            Debug.Log("Zombie died");
            
            DropLoot();

            if (deathAudio != null)
            {
                deathAudio.transform.parent = null;
                walkAudio.volume = 0.4f;
                deathAudio.Play();
                Destroy(deathAudio.gameObject, deathAudio.clip.length);
            }

            Destroy(gameObject);
        }

        private void DropLoot()
        {
            if (coinPrefab == null) return;

            for (int i = 0; i < coinDropAmount; i++)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    dropOffsetY,
                    Random.Range(-0.3f, 0.3f)
                );

                Vector3 dropPosition = transform.position + randomOffset;

                Instantiate(
                    coinPrefab,
                    dropPosition,
                    Quaternion.identity
                );
            }
        }
    }
}
