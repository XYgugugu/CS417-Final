using UnityEngine;
using PVZ3D.Region;

namespace PVZ3D.Plants
{
    public class PlantBase : MonoBehaviour
    {
        private float maxHealth = 100f;
        private float currentHealth;
        private Vector3 hurtboxCenter = new Vector3(0f, 0.45f, 0f);
        private Vector3 hurtboxSize = new Vector3(0.8f, 0.9f, 0.8f);
        private bool isPlaced;
        private Color feedbackStartColor = new Color(0.42f, 0.8f, 0.25f, 1f);
        private Color feedbackEndColor = new Color(0.55f, 0.38f, 0.16f, 0.7f);

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }
        public bool IsPlaced => isPlaced;
        public GridCell OccupiedCell { get; private set; }

        protected virtual void Awake()
        {
            currentHealth = Mathf.Max(1f, maxHealth);
        }

        protected void InitializePlant(
            float health,
            Vector3 hurtboxCenter,
            Vector3 hurtboxSize,
            Color plantedFeedbackColor,
            Color deathFeedbackColor,
            PlantVisualKind visualKind)
        {
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            ConfigureHurtbox(hurtboxCenter, hurtboxSize);
            feedbackStartColor = plantedFeedbackColor;
            feedbackEndColor = deathFeedbackColor;
            EnsureHurtbox();
            PlantVisualUtility.EnsurePlantVisual(transform, visualKind);
            PlantVisualUtility.EnsurePlantInteraction(transform);
        }

        private void ConfigureHurtbox(Vector3 center, Vector3 size)
        {
            this.hurtboxCenter = center;
            this.hurtboxSize = new Vector3(
                Mathf.Max(0.1f, size.x),
                Mathf.Max(0.1f, size.y),
                Mathf.Max(0.1f, size.z));

            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                ApplyHurtbox(box);
            }
        }

        private void EnsureHurtbox()
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

        public void TakeDamage(float amount)
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

        private void PlayPlantedFeedback()
        {
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

        public bool CanPlaceOn(GridCell cell)
        {
            return !IsDead && cell != null && (OccupiedCell == null || OccupiedCell == cell);
        }

        public void PlaceOnCell(GridCell cell)
        {
            if (!CanPlaceOn(cell))
            {
                return;
            }

            OccupiedCell = cell;
            isPlaced = true;
            PlayPlantedFeedback();
        }

        private void Die()
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
