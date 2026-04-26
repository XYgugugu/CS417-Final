using PVZ3D.Core;
using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private int lane;
        [SerializeField] private float damage;
        [SerializeField] private float speed = 9f;
        [SerializeField] private float lifeTime = 5f;
        [SerializeField] private float spinSpeed = 300f;

        private float spawnTime;
        private Transform visualTransform;

        public void Initialize(int laneIndex, float projectileDamage, float projectileSpeed)
        {
            lane = laneIndex;
            damage = projectileDamage;
            speed = projectileSpeed;
            spawnTime = Time.time;
            EnsureVisual();

            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.16f;
                sphere.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void Update()
        {
            transform.position += Vector3.right * (speed * Time.deltaTime);
            if (visualTransform != null)
            {
                visualTransform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);
            }

            if (Time.time - spawnTime >= lifeTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ZombieBase zombie = other.GetComponentInParent<ZombieBase>();
            if (zombie == null || zombie.Lane != lane)
            {
                return;
            }

            zombie.TakeDamage(damage);
            SpawnImpactFlash();
            Destroy(gameObject);
        }

        private void EnsureVisual()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            if (transform.childCount > 0)
            {
                visualTransform = transform.GetChild(0);
                return;
            }

            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.SetParent(transform, false);
            orb.transform.localScale = new Vector3(0.22f, 0.16f, 0.22f);
            Renderer orbRenderer = orb.GetComponent<Renderer>();
            if (orbRenderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(orbRenderer, new Color(0.35f, 0.96f, 0.35f));
            }

            Collider orbCol = orb.GetComponent<Collider>();
            if (orbCol != null)
            {
                orbCol.enabled = false;
            }

            visualTransform = orb.transform;
        }

        private void SpawnImpactFlash()
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.transform.position = transform.position;
            flash.transform.localScale = Vector3.one * 0.18f;
            Renderer renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(0.78f, 1f, 0.78f));
            }

            Collider col = flash.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.position = transform.position;
            ring.transform.localScale = new Vector3(0.24f, 0.02f, 0.24f);
            Renderer ringRenderer = ring.GetComponent<Renderer>();
            if (ringRenderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(ringRenderer, new Color(0.66f, 1f, 0.66f));
            }

            Collider ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null)
            {
                ringCollider.enabled = false;
            }

            Destroy(flash, 0.12f);
            Destroy(ring, 0.12f);
        }
    }
}
