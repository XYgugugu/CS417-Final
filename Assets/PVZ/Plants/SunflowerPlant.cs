using UnityEngine;

namespace PVZ3D.Plants
{
    public class SunflowerPlant : PlantBase
    {
        [SerializeField] private float sunInterval = 8f;
        [SerializeField] private Vector3 sunSpawnOffset = new Vector3(0f, 1.25f, 0f);
        [SerializeField] private float sunLifetime = 12f;
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
            ConfigureHurtbox(new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.9f, 0.8f));
            EnsureHurtbox();
            PlantVisualUtility.EnsurePlantVisual(transform, PlantVisualKind.Sunflower);
            nextSunTime = Time.time + sunInterval;
        }

        private void Update()
        {
            if (IsDead || Time.time < nextSunTime)
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
            GameObject sun = PlantVisualUtility.CreateSunVisual(transform.position + sunSpawnOffset, visualScale, sunValue);
            Destroy(sun, sunLifetime);
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
            upgradeHalo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            upgradeHalo.transform.localScale = new Vector3(0.55f, 0.015f, 0.55f);

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
