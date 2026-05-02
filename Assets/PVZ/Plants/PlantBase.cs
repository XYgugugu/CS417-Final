using UnityEngine;

namespace PVZ3D.Plants
{
    public class PlantBase : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private Vector3 hurtboxCenter = new Vector3(0f, 0.45f, 0f);
        [SerializeField] private Vector3 hurtboxSize = new Vector3(0.8f, 0.9f, 0.8f);

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }

        protected virtual void Awake()
        {
            currentHealth = Mathf.Max(1f, maxHealth);
        }

        public void SetMaxHealth(float value, bool refillHealth = true)
        {
            maxHealth = Mathf.Max(1f, value);
            if (refillHealth)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }
        }

        public void MultiplyMaxHealth(float multiplier, bool refillHealth = true)
        {
            SetMaxHealth(maxHealth * Mathf.Max(0.1f, multiplier), refillHealth);
        }

        protected void ConfigureHurtbox(Vector3 center, Vector3 size)
        {
            hurtboxCenter = center;
            hurtboxSize = new Vector3(
                Mathf.Max(0.1f, size.x),
                Mathf.Max(0.1f, size.y),
                Mathf.Max(0.1f, size.z));

            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                ApplyHurtbox(box);
            }
        }

        protected void EnsureHurtbox()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            ApplyHurtbox(box);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.isKinematic = true;
        }

        public virtual void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            SpawnDeathFeedback();
            Destroy(gameObject);
        }

        private void ApplyHurtbox(BoxCollider box)
        {
            box.center = hurtboxCenter;
            box.size = hurtboxSize;
            box.isTrigger = false;
        }

        private void SpawnDeathFeedback()
        {
            GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            burst.name = "Plant Death Feedback";
            burst.transform.position = transform.position + Vector3.up * Mathf.Max(0.35f, hurtboxCenter.y);
            burst.transform.localScale = hurtboxSize * 0.35f;

            Renderer renderer = burst.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.45f, 0.75f, 0.25f, 0.75f);
            }

            Collider collider = burst.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Destroy(burst, 0.25f);
        }
    }
}
