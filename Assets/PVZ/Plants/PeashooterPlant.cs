using UnityEngine;

namespace PVZ3D.Plants
{
    public class PeashooterPlant : PlantBase
    {
        [SerializeField] private float fireInterval = 1f;
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0.45f, 0.65f, 0f) * PlantVisualUtility.PrefabScale;
        [SerializeField] private float projectileDamage = 20f;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float repeaterFireInterval = 0.5f;
        [SerializeField] private float upgradedVisualScale = 1.18f;
        [SerializeField] private bool isRepeater;

        private float nextFireTime;
        private GameObject upgradeHalo;

        public bool CanUpgradeToRepeater => !isRepeater;

        protected override void Awake()
        {
            base.Awake();
            SetMaxHealth(100f);
            ConfigureHurtbox(
                new Vector3(0f, 0.45f, 0f) * PlantVisualUtility.PrefabScale,
                new Vector3(0.8f, 0.9f, 0.8f) * PlantVisualUtility.PrefabScale);
            RefreshFeedbackColors();
            EnsureHurtbox();
            PlantVisualUtility.EnsurePlantVisual(transform, PlantVisualKind.Peashooter);
            PlantVisualUtility.EnsurePlantInteraction(transform);
            nextFireTime = Time.time + GetCurrentFireInterval();
        }

        protected override void RefreshFeedbackColors()
        {
            ConfigureFeedbackColors(
                new Color(0.18f, 0.85f, 0.24f, 1f),
                new Color(0.04f, 0.5f, 0.16f, 0.65f));
        }

        private void Update()
        {
            if (!IsPlaced || IsDead || Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + GetCurrentFireInterval();
            FirePea();
        }

        [ContextMenu("Upgrade To Repeater")]
        public bool TryUpgradeToRepeater()
        {
            if (IsDead || !CanUpgradeToRepeater)
            {
                return false;
            }

            isRepeater = true;
            nextFireTime = Mathf.Min(nextFireTime, Time.time + GetCurrentFireInterval());
            ApplyRepeaterVisual();
            return true;
        }

        private void FirePea()
        {
            Vector3 spawnPosition = transform.position + transform.TransformDirection(muzzleOffset);
            PeaProjectile projectile = PeaProjectile.Create(spawnPosition, transform.right);
            projectile.Initialize(projectileDamage, projectileSpeed);
        }

        private float GetCurrentFireInterval()
        {
            float interval = isRepeater ? repeaterFireInterval : fireInterval;
            return Mathf.Max(0.05f, interval);
        }

        private void ApplyRepeaterVisual()
        {
            PlantVisualUtility.ScaleVisualRoot(transform, Vector3.one * upgradedVisualScale);

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = Color.Lerp(renderers[i].material.color, new Color(0.05f, 1f, 0.25f), 0.5f);
                }
            }

            upgradeHalo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upgradeHalo.name = "Repeater Upgrade Ring";
            upgradeHalo.transform.SetParent(transform, false);
            upgradeHalo.transform.localPosition = new Vector3(0f, 0.04f, 0f) * PlantVisualUtility.PrefabScale;
            upgradeHalo.transform.localScale = new Vector3(0.55f, 0.015f, 0.55f) * PlantVisualUtility.PrefabScale;

            Renderer haloRenderer = upgradeHalo.GetComponent<Renderer>();
            if (haloRenderer != null)
            {
                haloRenderer.material.color = new Color(1f, 0.88f, 0.12f);
            }

            Collider haloCollider = upgradeHalo.GetComponent<Collider>();
            if (haloCollider != null)
            {
                Destroy(haloCollider);
            }
        }
    }
}
