using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Region;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PlantBase : MonoBehaviour
    {
        [SerializeField] protected float currentHealth;
        public bool IsDead { get; private set; }

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

        public virtual void Die()
        {
            Destroy(gameObject);
        }
    }
}