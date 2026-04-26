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
        [SerializeField] private Vector3 baseScale = Vector3.one;

        private Coroutine scalePulseRoutine;

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

            baseScale = transform.localScale;
        }

        public virtual void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            currentHealth -= amount;
            PlayDamageFeedback();
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

        public virtual bool CanUpgradeWith(PlantDefinition upgradeDefinition)
        {
            return false;
        }

        public virtual int GetUpgradeCost(PlantDefinition upgradeDefinition)
        {
            return 0;
        }

        public virtual string GetUpgradeName(PlantDefinition upgradeDefinition)
        {
            return string.Empty;
        }

        public virtual bool ApplyUpgrade(PlantDefinition upgradeDefinition)
        {
            return false;
        }

        protected void RefreshBaseScale()
        {
            baseScale = transform.localScale;
        }

        protected void PlayScalePulse(Vector3 scaleMultiplier, float duration)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (scalePulseRoutine != null)
            {
                StopCoroutine(scalePulseRoutine);
            }

            scalePulseRoutine = StartCoroutine(AnimateScalePulse(scaleMultiplier, duration));
        }

        protected void SpawnFeedbackFlash(Color color, Vector3 scale, float lifeTime, Vector3? worldOffset = null, PrimitiveType primitiveType = PrimitiveType.Sphere)
        {
            GameObject flash = GameObject.CreatePrimitive(primitiveType);
            flash.transform.position = transform.position + (worldOffset ?? Vector3.up * 0.75f);
            flash.transform.localScale = scale;

            Renderer renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, color);
            }

            Collider collider = flash.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Destroy(flash, lifeTime);
        }

        protected virtual void PlayDamageFeedback()
        {
            PlayScalePulse(new Vector3(1.08f, 0.88f, 1.08f), 0.12f);
            SpawnFeedbackFlash(new Color(1f, 0.42f, 0.42f), new Vector3(0.42f, 0.08f, 0.42f), 0.12f, Vector3.up * 0.1f, PrimitiveType.Cylinder);
        }

        protected virtual void PlayUpgradeFeedback()
        {
            PlayScalePulse(new Vector3(1.15f, 1.15f, 1.15f), 0.22f);
            SpawnFeedbackFlash(new Color(0.98f, 0.92f, 0.46f), new Vector3(0.42f, 0.42f, 0.42f), 0.18f, Vector3.up * 1f);
            SpawnFeedbackFlash(new Color(0.7f, 1f, 0.6f), new Vector3(0.62f, 0.04f, 0.62f), 0.16f, Vector3.up * 0.12f, PrimitiveType.Cylinder);
        }

        private System.Collections.IEnumerator AnimateScalePulse(Vector3 scaleMultiplier, float duration)
        {
            Vector3 originalScale = baseScale;
            Vector3 targetScale = Vector3.Scale(originalScale, scaleMultiplier);

            float halfDuration = Mathf.Max(0.01f, duration * 0.5f);
            float timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float t = timer / halfDuration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float t = timer / halfDuration;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
            scalePulseRoutine = null;
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
