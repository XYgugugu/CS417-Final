using UnityEngine;

namespace PVZ3D.Plants
{
    public class WallNutPlant : PlantBase
    {
        [SerializeField] private bool isTallNut;

        public bool CanUpgradeToTallNut => !isTallNut;

        protected override void Awake()
        {
            base.Awake();
            SetMaxHealth(800f);
            ConfigureHurtbox(new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f));
            EnsureHurtbox();
            PlantVisualUtility.EnsurePlantVisual(transform, PlantVisualKind.WallNut);
        }

        [ContextMenu("Upgrade To Tall-Nut")]
        public bool TryUpgradeToTallNut()
        {
            if (IsDead || !CanUpgradeToTallNut)
            {
                return false;
            }

            isTallNut = true;
            MultiplyMaxHealth(2f);
            ConfigureHurtbox(new Vector3(0f, 0.78f, 0f), new Vector3(1.1f, 1.55f, 1.1f));
            EnsureHurtbox();
            ApplyTallNutVisual();
            return true;
        }

        private void ApplyTallNutVisual()
        {
            PlantVisualUtility.ScaleVisualRoot(transform, new Vector3(1.12f, 1.55f, 1.12f));

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = Color.Lerp(renderers[i].material.color, new Color(0.9f, 0.58f, 0.24f), 0.35f);
                }
            }
        }
    }
}
