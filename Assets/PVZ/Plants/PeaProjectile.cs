using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PeaProjectile : MonoBehaviour
    {
        [SerializeField] private float damage = 20f;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float hitRadius = 0.18f * PlantVisualUtility.PrefabScale;

        private Vector3 direction = Vector3.forward;
        private bool hasHit;

        public static PeaProjectile Create(Vector3 position, Vector3 direction)
        {
            GameObject pea = new GameObject("Pea Projectile");
            pea.transform.position = position;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Pea Visual";
            visual.transform.SetParent(pea.transform, false);
            visual.transform.localScale = Vector3.one * 0.18f * PlantVisualUtility.PrefabScale;

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.85f, 0.25f);
            }

            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            SphereCollider collider = pea.AddComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = 0.18f * PlantVisualUtility.PrefabScale;
                collider.isTrigger = true;
            }

            Rigidbody body = pea.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;

            PeaProjectile projectile = pea.AddComponent<PeaProjectile>();
            projectile.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            return projectile;
        }

        public void Initialize(float projectileDamage, float projectileSpeed)
        {
            damage = Mathf.Max(0f, projectileDamage);
            speed = Mathf.Max(0.1f, projectileSpeed);
        }

        private void Awake()
        {
            EnsurePrefabSetup();
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            Vector3 start = transform.position;
            Vector3 movement = direction * speed * Time.deltaTime;
            Vector3 end = start + movement;

            if (TryHitBetween(start, end))
            {
                return;
            }

            transform.position = end;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHit(other);
        }

        private bool TryHitBetween(Vector3 start, Vector3 end)
        {
            Vector3 movement = end - start;
            float distance = movement.magnitude;
            if (distance <= 0.001f)
            {
                return TryHitOverlaps(start);
            }

            RaycastHit[] hits = Physics.SphereCastAll(
                start,
                hitRadius,
                movement / distance,
                distance,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (!IsSelfCollider(hitCollider) && TryHit(hitCollider))
                {
                    return true;
                }
            }

            return TryHitOverlaps(end);
        }

        private bool TryHit(Collider other)
        {
            if (hasHit || other == null)
            {
                return false;
            }

            ZombieBase zombie = ResolveZombie(other);
            if (zombie == null && !IsZombieTagged(other))
            {
                return false;
            }

            ConsumeProjectile();

            if (zombie != null)
            {
                zombie.TakeDamage(damage);
            }
            else
            {
                other.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }

            return true;
        }

        private void ConsumeProjectile()
        {
            hasHit = true;
            enabled = false;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private bool TryHitOverlaps(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position, hitRadius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsSelfCollider(hits[i]) && TryHit(hits[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSelfCollider(Collider other)
        {
            return other == null || other.transform == transform || other.transform.IsChildOf(transform);
        }

        private static bool IsZombieTagged(Collider other)
        {
            Transform current = other.transform;
            while (current != null)
            {
                if (current.CompareTag("Zombie"))
                {
                    return true;
                }

                current = current.parent;
            }

            Rigidbody attachedBody = other.attachedRigidbody;
            if (attachedBody == null)
            {
                return false;
            }

            current = attachedBody.transform;
            while (current != null)
            {
                if (current.CompareTag("Zombie"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static ZombieBase ResolveZombie(Collider other)
        {
            ZombieBase zombie = other.GetComponentInParent<ZombieBase>();
            if (zombie != null)
            {
                return zombie;
            }

            Rigidbody attachedBody = other.attachedRigidbody;
            return attachedBody != null ? attachedBody.GetComponentInParent<ZombieBase>() : null;
        }

        private void EnsurePrefabSetup()
        {
            if (GetComponentInChildren<Renderer>(true) == null)
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "Pea Visual";
                visual.transform.SetParent(transform, false);
                visual.transform.localScale = Vector3.one * 0.18f * PlantVisualUtility.PrefabScale;

                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.2f, 0.85f, 0.25f);
                }

                Collider visualCollider = visual.GetComponent<Collider>();
                if (visualCollider != null)
                {
                    Destroy(visualCollider);
                }
            }

            SphereCollider collider = GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }

            collider.radius = hitRadius;
            collider.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.isKinematic = true;
        }
    }
}
