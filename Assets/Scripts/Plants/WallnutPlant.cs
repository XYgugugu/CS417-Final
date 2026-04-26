using PVZ3D.Core;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class WallnutPlant : PlantBase
    {
        private const int TallnutUpgradeCost = 50;

        [SerializeField] private bool isTallnut;
        [SerializeField] private Transform crownVisual;
        [SerializeField] private Transform browVisual;

        public override bool CanUpgradeWith(PlantDefinition upgradeDefinition)
        {
            return !isTallnut
                && upgradeDefinition != null
                && upgradeDefinition.Archetype == PlantArchetype.Wallnut;
        }

        public override int GetUpgradeCost(PlantDefinition upgradeDefinition)
        {
            return CanUpgradeWith(upgradeDefinition) ? TallnutUpgradeCost : 0;
        }

        public override string GetUpgradeName(PlantDefinition upgradeDefinition)
        {
            return CanUpgradeWith(upgradeDefinition) ? "Tallnut" : string.Empty;
        }

        public override bool ApplyUpgrade(PlantDefinition upgradeDefinition)
        {
            if (!CanUpgradeWith(upgradeDefinition))
            {
                return false;
            }

            isTallnut = true;
            currentHealth *= 1.5f;
            gameObject.name = "Tallnut";
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(1f, 1.5f, 1f));
            RefreshBaseScale();
            EnsureTallnutVisuals();
            PlayUpgradeFeedback();
            return true;
        }

        public override void Initialize(PlantDefinition plantDefinition, Grid.GridCell cell)
        {
            base.Initialize(plantDefinition, cell);
            if (isTallnut)
            {
                EnsureTallnutVisuals();
            }
        }

        private void EnsureTallnutVisuals()
        {
            TintShell(new Color(0.43f, 0.24f, 0.1f), new Color(0.33f, 0.18f, 0.08f));

            if (crownVisual == null)
            {
                crownVisual = CreateUpgradePart(
                    "TallnutTop",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 1.18f, 0f),
                    new Vector3(0.36f, 0.06f, 0.36f),
                    new Color(0.62f, 0.42f, 0.2f),
                    Quaternion.identity);
            }

            if (browVisual == null)
            {
                browVisual = CreateUpgradePart(
                    "TallnutBrow",
                    PrimitiveType.Cube,
                    new Vector3(-0.04f, 0.92f, 0.28f),
                    new Vector3(0.3f, 0.05f, 0.06f),
                    new Color(0.2f, 0.1f, 0.04f),
                    Quaternion.Euler(0f, 0f, -8f));
            }
        }

        private void TintShell(Color shellColor, Color accentColor)
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
                bool accent = localPos.y <= 0.08f || localPos.z > 0.18f;
                RuntimeVisualMaterialUtility.ApplyColor(renderer, accent ? accentColor : shellColor);
            }
        }

        private Transform CreateUpgradePart(string partName, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Color color, Quaternion localRotation)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, color);
            }

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return part.transform;
        }
    }
}
