using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PeaProjectile : MonoBehaviour
    {
        [SerializeField] private float damage = 20f;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float hitRadius = 0.18f;

        private Vector3 direction = Vector3.forward;
        private bool hasHit;

        public static PeaProjectile Create(Vector3 position, Vector3 direction)
        {
            GameObject pea = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pea.name = "Pea Projectile";
            pea.transform.position = position;
            pea.transform.localScale = Vector3.one * 0.18f;

            Renderer renderer = pea.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.85f, 0.25f);
            }

            Collider collider = pea.GetComponent<Collider>();
            if (collider != null)
            {
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
            Vector3 center = (start + end) * 0.5f;
            float radius = hitRadius + Vector3.Distance(start, end) * 0.5f;
            Collider[] hits = Physics.OverlapSphere(center, radius);

            for (int i = 0; i < hits.Length; i++)
            {
                if (TryHit(hits[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryHit(Collider other)
        {
            if (hasHit || other == null)
            {
                return false;
            }

            ZombieBase zombie = other.GetComponentInParent<ZombieBase>();
            if (zombie == null)
            {
                return false;
            }

            zombie.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            hasHit = true;
            Destroy(gameObject);
            return true;
        }
    }
}
