using UnityEngine;
using PVZ3D.Core;

namespace PVZ3D.Plants
{
    public class SunCollectible : MonoBehaviour
    {
        [SerializeField] private int value = 25;
        [SerializeField] private float triggerRadius = 0.45f;
        [SerializeField] private float lifetime = 25f;
        [SerializeField] private float fadeStartTime = 20f;
        [SerializeField] private float impulseForce = 3.4f;
        [SerializeField] private float upwardImpulse = 1.35f;
        [SerializeField] private float bounceForce = 1.2f;
        [SerializeField] private float groundedNormalThreshold = 0.65f;

        private float spawnTime;
        private Renderer[] renderers;
        private Color[] originalColors;
        private bool impulseApplied;
        private bool collected;
        private GameManager gameManager;
        private SphereCollider physicsCollider;
        private PhysicsMaterial bouncyMaterial;
        private PhysicsMaterial settledMaterial;

        private void Awake()
        {
            EnsurePrefabSetup();
            CacheRenderers();
        }

        private void Start()
        {
            spawnTime = Time.time;
            ApplySpawnImpulse();
        }

        public void SetValue(int sunValue)
        {
            value = Mathf.Max(0, sunValue);
        }

        private void Update()
        {
            float age = Time.time - spawnTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (age >= fadeStartTime)
            {
                float fadeDuration = Mathf.Max(0.01f, lifetime - fadeStartTime);
                float alpha = Mathf.Clamp01(1f - ((age - fadeStartTime) / fadeDuration));
                ApplyAlpha(alpha);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !IsPlayer(other))
            {
                return;
            }

            Collect();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsGroundCollision(collision))
            {
                StopBouncing();
                return;
            }

            BounceAwayFrom(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (IsGroundCollision(collision))
            {
                StopBouncing();
            }
        }

        private void EnsurePrefabSetup()
        {
            SphereCollider triggerCollider = null;
            physicsCollider = null;
            SphereCollider[] sphereColliders = GetComponents<SphereCollider>();
            for (int i = 0; i < sphereColliders.Length; i++)
            {
                if (sphereColliders[i].isTrigger && triggerCollider == null)
                {
                    triggerCollider = sphereColliders[i];
                }
                else if (!sphereColliders[i].isTrigger && physicsCollider == null)
                {
                    physicsCollider = sphereColliders[i];
                }
            }

            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }

            triggerCollider.radius = triggerRadius;
            triggerCollider.isTrigger = true;

            if (physicsCollider == null)
            {
                physicsCollider = gameObject.AddComponent<SphereCollider>();
            }

            physicsCollider.radius = triggerRadius * 0.55f;
            physicsCollider.isTrigger = false;
            physicsCollider.material = GetBouncyMaterial();

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = true;
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void BounceAwayFrom(Collision collision)
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null || collision == null || collision.contactCount == 0)
            {
                return;
            }

            Vector3 normal = collision.GetContact(0).normal;
            Vector3 bounceDirection = Vector3.Reflect(body.linearVelocity.normalized, normal);
            if (bounceDirection.sqrMagnitude <= 0.001f)
            {
                bounceDirection = (normal + Vector3.up * 0.35f).normalized;
            }

            body.AddForce(bounceDirection.normalized * bounceForce, ForceMode.Impulse);
        }

        private void StopBouncing()
        {
            if (physicsCollider != null)
            {
                physicsCollider.material = GetSettledMaterial();
            }
        }

        private bool IsGroundCollision(Collision collision)
        {
            if (collision == null)
            {
                return false;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.rigidbody == null && collision.GetContact(i).normal.y >= groundedNormalThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private PhysicsMaterial GetBouncyMaterial()
        {
            if (bouncyMaterial == null)
            {
                bouncyMaterial = new PhysicsMaterial("Sun Bouncy Material")
                {
                    bounciness = 0.75f,
                    dynamicFriction = 0.15f,
                    staticFriction = 0.15f,
                    bounceCombine = PhysicsMaterialCombine.Maximum,
                    frictionCombine = PhysicsMaterialCombine.Minimum
                };
            }

            return bouncyMaterial;
        }

        private PhysicsMaterial GetSettledMaterial()
        {
            if (settledMaterial == null)
            {
                settledMaterial = new PhysicsMaterial("Sun Settled Material")
                {
                    bounciness = 0f,
                    dynamicFriction = 0.8f,
                    staticFriction = 0.8f,
                    bounceCombine = PhysicsMaterialCombine.Minimum,
                    frictionCombine = PhysicsMaterialCombine.Maximum
                };
            }

            return settledMaterial;
        }

        [ContextMenu("Collect Sun")]
        public void Collect()
        {
            if (collected)
            {
                return;
            }

            collected = true;

            GameManager manager = ResolveGameManager();
            if (manager != null)
            {
                manager.PlantsEconomy.CollectSun(value);
            }

            PlayCollectedFeedback();
            SetVisible(false);
            Destroy(gameObject, 0.35f);
        }

        private void PlayCollectedFeedback()
        {
            PlantVisualUtility.CreateParticleBurst(
                transform.position,
                new Color(1f, 0.94f, 0.18f, 1f),
                new Color(1f, 0.56f, 0.05f, 0.8f),
                36,
                0.07f,
                1.8f,
                0.45f,
                0.18f,
                "Sun Collected Feedback");
        }

        private void SetVisible(bool visible)
        {
            Renderer[] sunRenderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < sunRenderers.Length; i++)
            {
                if (sunRenderers[i] != null)
                {
                    sunRenderers[i].enabled = visible;
                }
            }

            Collider[] sunColliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < sunColliders.Length; i++)
            {
                if (sunColliders[i] != null)
                {
                    sunColliders[i].enabled = visible;
                }
            }
        }

        private GameManager ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager;
        }

        private static bool IsPlayer(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                return true;
            }

            Rigidbody attachedBody = other.attachedRigidbody;
            if (attachedBody != null && attachedBody.CompareTag("Player"))
            {
                return true;
            }

            Transform root = other.transform.root;
            return root != null && root.CompareTag("Player");
        }

        private void ApplySpawnImpulse()
        {
            if (impulseApplied)
            {
                return;
            }

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                return;
            }

            Vector2 horizontal = Random.insideUnitCircle.normalized;
            if (horizontal.sqrMagnitude <= 0.001f)
            {
                horizontal = Vector2.right;
            }

            Vector3 direction = new Vector3(horizontal.x, upwardImpulse, horizontal.y).normalized;
            body.AddForce(direction * impulseForce, ForceMode.Impulse);
            impulseApplied = true;
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                Material material = renderers[i].material;
                originalColors[i] = material.color;
                ConfigureFadeMaterial(material);
            }
        }

        private void ApplyAlpha(float alpha)
        {
            if (renderers == null || originalColors == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                Color color = originalColors[i];
                color.a *= alpha;
                renderers[i].material.color = color;
            }
        }

        private static void ConfigureFadeMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.SetFloat("_Mode", 2f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
    }
}
