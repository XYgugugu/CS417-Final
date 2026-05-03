using UnityEngine;
using PVZ3D.Region;

namespace PVZ3D.Plants
{
    public class PlantBase : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private Vector3 hurtboxCenter = new Vector3(0f, 0.45f, 0f);
        [SerializeField] private Vector3 hurtboxSize = new Vector3(0.8f, 0.9f, 0.8f);
        [SerializeField] private bool isPlaced;
        [SerializeField] private Color feedbackStartColor = new Color(0.42f, 0.8f, 0.25f, 1f);
        [SerializeField] private Color feedbackEndColor = new Color(0.55f, 0.38f, 0.16f, 0.7f);

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }
        public bool IsPlaced => isPlaced;
        public GridCell OccupiedCell { get; private set; }

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

            body.useGravity = true;
            body.isKinematic = false;
        }

        protected void ConfigureFeedbackColors(Color startColor, Color endColor)
        {
            feedbackStartColor = startColor;
            feedbackEndColor = endColor;
        }

        protected virtual void RefreshFeedbackColors()
        {
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

        [ContextMenu("Play Planted Feedback")]
        public void PlayPlantedFeedback()
        {
            RefreshFeedbackColors();
            PlantVisualUtility.CreateParticleBurst(
                transform.position + Vector3.up * 0.08f,
                feedbackStartColor,
                feedbackEndColor,
                24,
                0.08f,
                1.35f,
                0.55f,
                0.28f,
                "Plant Planted Feedback");
        }

        [ContextMenu("Kill Plant")]
        public void KillPlantForTest()
        {
            TakeDamage(currentHealth);
        }

        public virtual bool CanPlaceOn(GridCell cell)
        {
            return !IsDead && cell != null && (OccupiedCell == null || OccupiedCell == cell);
        }

        public virtual void PlaceOnCell(GridCell cell)
        {
            if (!CanPlaceOn(cell))
            {
                return;
            }

            OccupiedCell = cell;
            isPlaced = true;
            PlayPlantedFeedback();
        }

        public virtual void RemoveFromCell(GridCell cell)
        {
            if (OccupiedCell != cell)
            {
                return;
            }

            OccupiedCell = null;
            isPlaced = false;
        }

        protected virtual void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            OccupiedCell?.ClearPlant(this);
            OccupiedCell = null;
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
            RefreshFeedbackColors();
            PlantVisualUtility.CreateParticleBurst(
                transform.position + Vector3.up * Mathf.Max(0.35f, hurtboxCenter.y),
                feedbackEndColor,
                feedbackStartColor,
                30,
                0.045f,
                1.1f,
                0.85f,
                Mathf.Max(0.12f, hurtboxSize.x * 0.18f),
                "Plant Death Feedback");
        }
    }
}
