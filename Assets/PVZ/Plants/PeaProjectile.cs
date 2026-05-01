using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PeaProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float damage = 20f;
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float hitRadius = 0.18f;

        private Vector3 direction = Vector3.right;
        private bool hasHit;

        public static PeaProjectile Create(Vector3 position, Vector3 direction)
        {
            GameObject pea = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pea.name = "Pea Projectile";
            pea.transform.position = position;
            pea.transform.localScale = Vector3.one * 0.18f;

            Collider collider = pea.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Rigidbody rb = pea.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            Renderer renderer = pea.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.85f, 0.25f);
            }

            PeaProjectile projectile = pea.AddComponent<PeaProjectile>();
            projectile.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
            return projectile;
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

            if (TryHitZombieBetween(start, end))
            {
                return;
            }

            transform.position = end;
        }

        private void OnTriggerEnter(Collider other)
        {
            ZombieBase zombie = other.GetComponentInParent<ZombieBase>();
            if (zombie == null)
            {
                return;
            }

            HitZombie(zombie);
        }

        private bool TryHitZombieBetween(Vector3 start, Vector3 end)
        {
            Vector3 center = (start + end) * 0.5f;
            float radius = hitRadius + Vector3.Distance(start, end) * 0.5f;
            Collider[] hits = Physics.OverlapSphere(center, radius);

            for (int i = 0; i < hits.Length; i++)
            {
                ZombieBase zombie = hits[i].GetComponentInParent<ZombieBase>();
                if (zombie == null || zombie.IsDead)
                {
                    continue;
                }

                HitZombie(zombie);
                return true;
            }

            return false;
        }

        private void HitZombie(ZombieBase zombie)
        {
            if (hasHit || zombie == null || zombie.IsDead)
            {
                return;
            }

            hasHit = true;
            zombie.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
