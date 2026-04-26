using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Grid;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PlantBase : MonoBehaviour
    {
        private static readonly HashSet<PlantBase> ActivePlants = new HashSet<PlantBase>();

        [SerializeField] protected PlantDefinition definition;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected GridCell occupiedCell;
        [SerializeField] protected int lane;

        public PlantDefinition Definition => definition;
        public int Lane => lane;
        public bool IsDead { get; private set; }

        protected virtual void OnEnable()
        {
            ActivePlants.Add(this);
        }

        protected virtual void OnDisable()
        {
            ActivePlants.Remove(this);
        }

        public virtual void Initialize(PlantDefinition plantDefinition, GridCell cell)
        {
            definition = plantDefinition;
            occupiedCell = cell;
            lane = cell != null ? cell.LaneIndex : 0;
            currentHealth = definition != null ? Mathf.Max(1f, definition.MaxHealth) : 100f;
            IsDead = false;

            if (occupiedCell != null)
            {
                occupiedCell.AssignPlant(this);
                transform.position = occupiedCell.transform.position + Vector3.up * 0.5f;
            }
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

        public virtual void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;

            if (occupiedCell != null)
            {
                GameManager.Instance?.RegisterPlantRemoved(occupiedCell.LaneIndex, occupiedCell.ColumnIndex);
                occupiedCell.ClearPlant(this);
                occupiedCell = null;
            }

            Destroy(gameObject);
        }

        public static void DestroyAllPlants()
        {
            PlantBase[] plants = new PlantBase[ActivePlants.Count];
            ActivePlants.CopyTo(plants);

            foreach (PlantBase plant in plants)
            {
                if (plant != null)
                {
                    plant.Die();
                }
            }
        }
    }
}