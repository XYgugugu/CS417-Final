using UnityEngine;
using System.Collections;
using PVZ3D.Core;
using PVZ3D.Plants;

namespace PVZ3D.Zombies
{
    public class ZombieBase : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float baseHealth = 100f;
        [SerializeField] private float shieldHealth = 0f;
        [SerializeField] private bool hasShield = false;

        [Header("Attack")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackInterval = 1f;

        [Header("Loot Drop")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private int coinDropAmount = 1;
        [SerializeField] private int scoreValue = 10;

        [Header("Type")]
        [SerializeField] private GameObject SlicePrefab;

        [Header("Game Stats")]
        [SerializeField] private GameManager gameManager;

        [Header("Attack Juice")]
        [SerializeField] private float attackDistance = 0.08f;
        [SerializeField] private float attackSpeed = 8f;

        [Header("Audio")]
        [SerializeField] private AudioSource walkAudio;
        [SerializeField] private AudioSource attackAudio;
        [SerializeField] private AudioSource deathAudio;

        private const float DropOffsetY = 0.3f;

        private float currentHealth;
        private float currentShieldHealth;
        private float attackTimer;
        private bool isAttacking;
        private PathFollow movement;
        private PlantBase targetPlant;
        private bool isDead;

        public float AttackDamage => attackDamage;

        private void Awake()
        {
            EnsureRuntimeSetup();
            currentHealth = baseHealth;
            currentShieldHealth = shieldHealth;
            movement = GetComponent<PathFollow>();
        }

        private void Update()
        {
            if (isDead) return;

            HandleWalkAudio();
            AttackPlant();
        }

        private void HandleWalkAudio()
        {
            if (movement == null || walkAudio == null) return;

            if (!movement.IsStopped && !walkAudio.isPlaying)
            {
                walkAudio.volume = 0.3f;
                walkAudio.Play();
            }
            else if (movement.IsStopped && walkAudio.isPlaying)
            {
                walkAudio.Stop();
            }
        }

        private void AttackPlant()
        {
            if (targetPlant == null)
            {
                movement?.ResumeMoving();
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
            if (isDead || damage <= 0f) return;

            if (hasShield && currentShieldHealth > 0)
            {
                float absorbedDamage = Mathf.Min(currentShieldHealth, damage);
                currentShieldHealth -= absorbedDamage;
                damage -= absorbedDamage;
                hasShield = currentShieldHealth > 0f;
            }

            if (damage <= 0f) return;

            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Kill()
        {
            Die();
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log("Zombie died");

            ResolveGameManager()?.RegisterZombieKilled(scoreValue);
            
            DropLoot();

            if (deathAudio != null)
            {
                deathAudio.transform.parent = null;
                deathAudio.volume = 0.4f;
                deathAudio.Play();
                float destroyDelay = deathAudio.clip != null ? deathAudio.clip.length : 1f;
                Destroy(deathAudio.gameObject, destroyDelay);
            }

            if (SlicePrefab != null)
            {
                Instantiate(SlicePrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }

        private GameManager ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager;
        }

        private void DropLoot()
        {
            if (coinPrefab == null) return;

            for (int i = 0; i < coinDropAmount; i++)
            {
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    DropOffsetY,
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
