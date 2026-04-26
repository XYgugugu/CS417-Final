using PVZ3D.Core;
using PVZ3D.Resources;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class SunflowerPlant : PlantBase
    {
        private const int TwinSunflowerUpgradeCost = 50;
        private const int UpgradeSunBonus = 25;

        [SerializeField] private bool isUpgradedSunflower;
        [SerializeField] private Transform upgradeHaloVisual;

        private float timer;

        public override bool CanUpgradeWith(PlantDefinition upgradeDefinition)
        {
            return !isUpgradedSunflower
                && upgradeDefinition != null
                && upgradeDefinition.Archetype == PlantArchetype.Sunflower;
        }

        public override int GetUpgradeCost(PlantDefinition upgradeDefinition)
        {
            return CanUpgradeWith(upgradeDefinition) ? TwinSunflowerUpgradeCost : 0;
        }

        public override string GetUpgradeName(PlantDefinition upgradeDefinition)
        {
            return CanUpgradeWith(upgradeDefinition) ? "Empowered Sunflower" : string.Empty;
        }

        public override bool ApplyUpgrade(PlantDefinition upgradeDefinition)
        {
            if (!CanUpgradeWith(upgradeDefinition))
            {
                return false;
            }

            isUpgradedSunflower = true;
            gameObject.name = "Empowered Sunflower";
            transform.localScale = new Vector3(1.08f, 1.08f, 1.08f);
            RefreshBaseScale();
            EnsureUpgradeVisuals();
            PlayUpgradeFeedback();
            return true;
        }

        public override void Initialize(PlantDefinition plantDefinition, Grid.GridCell cell)
        {
            base.Initialize(plantDefinition, cell);
            if (isUpgradedSunflower)
            {
                EnsureUpgradeVisuals();
            }
        }

        private void Update()
        {
            if (IsDead || definition == null)
            {
                return;
            }

            GamePhase phase = GameManager.Instance != null ? GameManager.Instance.State.Phase : GamePhase.Menu;
            if (phase != GamePhase.Battle && phase != GamePhase.Prep)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer >= definition.SunDropInterval)
            {
                timer = 0f;
                int sunAmount = definition.SunPerDrop + (isUpgradedSunflower ? UpgradeSunBonus : 0);
                SunSpawner.SpawnSunAt(transform.position + Vector3.up * 1.2f, sunAmount);
                PlayScalePulse(new Vector3(1.08f, 1.18f, 1.08f), 0.22f);
                SpawnFeedbackFlash(new Color(1f, 0.92f, 0.38f), new Vector3(0.32f, 0.32f, 0.32f), 0.16f, Vector3.up * 1.1f);
                SpawnFeedbackFlash(new Color(1f, 0.82f, 0.24f), new Vector3(0.52f, 0.04f, 0.52f), 0.18f, Vector3.up * 0.18f, PrimitiveType.Cylinder);
                if (isUpgradedSunflower)
                {
                    SpawnFeedbackFlash(new Color(1f, 0.98f, 0.7f), new Vector3(0.42f, 0.42f, 0.42f), 0.18f, Vector3.up * 1.35f);
                }
            }
        }

        private void EnsureUpgradeVisuals()
        {
            TintFlower(new Color(1f, 0.9f, 0.28f), new Color(0.56f, 0.34f, 0.12f));

            if (upgradeHaloVisual == null)
            {
                GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                halo.name = "SunflowerUpgradeHalo";
                halo.transform.SetParent(transform, false);
                halo.transform.localPosition = new Vector3(0f, 1.06f, 0f);
                halo.transform.localScale = new Vector3(0.34f, 0.03f, 0.34f);
                halo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                Renderer renderer = halo.GetComponent<Renderer>();
                if (renderer != null)
                {
                    RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(1f, 0.96f, 0.54f));
                }

                Collider collider = halo.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }

                upgradeHaloVisual = halo.transform;
            }
        }

        private void TintFlower(Color petalColor, Color centerColor)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Vector3 localPos = renderer.transform.localPosition;
                bool centerLike = localPos.y > 0.88f && renderer.transform.localScale.z <= 0.18f;
                if (centerLike)
                {
                    RuntimeVisualMaterialUtility.ApplyColor(renderer, centerColor);
                }
                else if (localPos.y > 0.72f)
                {
                    RuntimeVisualMaterialUtility.ApplyColor(renderer, petalColor);
                }
            }
        }
    }
}
