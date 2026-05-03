using UnityEngine;

namespace PVZ3D.Plants
{
    public class SunflowerPlant : PlantBase
    {
        [SerializeField] private float sunInterval = 8f;
        [SerializeField] private Vector3 sunSpawnOffset = new Vector3(0f, 1.25f, 0f);
        [SerializeField] private int normalSunValue = 25;
        [SerializeField] private int upgradedSunValue = 50;
        [SerializeField] private float upgradedVisualScale = 1.16f;
        [SerializeField] private bool producesLargeSun;

        private float nextSunTime;
        private GameObject upgradeHalo;

        public bool CanUpgradeSunProduction => !producesLargeSun;

        protected override void Awake()
        {
            base.Awake();
            SetMaxHealth(100f);
            ConfigureHurtbox(
                new Vector3(0f, 0.45f, 0f) * PlantVisualUtility.PrefabScale,
                new Vector3(0.8f, 0.9f, 0.8f) * PlantVisualUtility.PrefabScale);
            RefreshFeedbackColors();
            EnsureHurtbox();
            PlantVisualUtility.EnsurePlantVisual(transform, PlantVisualKind.Sunflower);
            PlantVisualUtility.EnsurePlantInteraction(transform);
            nextSunTime = Time.time + sunInterval;
        }

        protected override void RefreshFeedbackColors()
        {
            ConfigureFeedbackColors(
                new Color(1f, 0.86f, 0.08f, 1f),
                new Color(0.42f, 0.82f, 0.24f, 0.65f));
        }

        private void Update()
        {
            if (!IsPlaced || IsDead || Time.time < nextSunTime)
            {
                return;
            }

            nextSunTime = Time.time + sunInterval;
            SpawnSun();
        }

        private void SpawnSun()
        {
            float visualScale = producesLargeSun ? 1.45f : 1f;
            int sunValue = producesLargeSun ? upgradedSunValue : normalSunValue;
            PlantVisualUtility.CreateSunVisual(transform.position + sunSpawnOffset, visualScale, sunValue);
        }

        [ContextMenu("Upgrade Sun Production")]
        public bool TryUpgradeSunProduction()
        {
            if (IsDead || !CanUpgradeSunProduction)
            {
                return false;
            }

            producesLargeSun = true;
            ApplyUpgradedVisual();
            return true;
        }

        private void ApplyUpgradedVisual()
        {
            PlantVisualUtility.ScaleVisualRoot(transform, Vector3.one * upgradedVisualScale);

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = Color.Lerp(renderers[i].material.color, new Color(1f, 0.95f, 0.15f), 0.35f);
                }
            }

            upgradeHalo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upgradeHalo.name = "Sunflower Upgrade Ring";
            upgradeHalo.transform.SetParent(transform, false);
            upgradeHalo.transform.localPosition = new Vector3(0f, 0.04f, 0f) * PlantVisualUtility.PrefabScale;
            upgradeHalo.transform.localScale = new Vector3(0.55f, 0.015f, 0.55f) * PlantVisualUtility.PrefabScale;

            Renderer haloRenderer = upgradeHalo.GetComponent<Renderer>();
            if (haloRenderer != null)
            {
                haloRenderer.material.color = new Color(1f, 0.78f, 0.08f);
            }

            Collider haloCollider = upgradeHalo.GetComponent<Collider>();
            if (haloCollider != null)
            {
                Destroy(haloCollider);
            }
        }
    }
}
