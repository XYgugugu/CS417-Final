using UnityEngine;

namespace PVZ3D.Plants
{
    public class PlantBase : MonoBehaviour
    {
        [SerializeField] private PlantType plantType;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private PlantingCell occupiedCell;

        public PlantType PlantType => plantType;
        public bool IsDead { get; private set; }

        public void Initialize(PlantType type, PlantingCell cell)
        {
            plantType = type;
            occupiedCell = cell;
            maxHealth = PlantStats.Get(type).MaxHealth;
            currentHealth = maxHealth;
            IsDead = false;
        }

        public void MultiplyMaxHealth(float multiplier)
        {
            if (multiplier <= 0f)
            {
                return;
            }

            float previousMaxHealth = maxHealth;
            maxHealth *= multiplier;
            currentHealth += maxHealth - previousMaxHealth;
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

        public void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            occupiedCell?.Clear(this);
            Destroy(gameObject);
        }
    }
}
