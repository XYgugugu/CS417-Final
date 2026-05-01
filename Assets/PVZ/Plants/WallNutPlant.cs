using UnityEngine;

namespace PVZ3D.Plants
{
    public class WallNutPlant : MonoBehaviour
    {
        [SerializeField] private bool isTallNut;

        public bool CanUpgradeToTallNut => !isTallNut;

        public bool TryUpgradeToTallNut()
        {
            if (!CanUpgradeToTallNut)
            {
                return false;
            }

            isTallNut = true;

            PlantBase plant = GetComponent<PlantBase>();
            if (plant != null)
            {
                plant.MultiplyMaxHealth(2f);
            }

            ApplyTallNutVisual();
            return true;
        }

        private void ApplyTallNutVisual()
        {
            transform.localScale = new Vector3(1.12f, 1.55f, 1.12f);

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material material = renderer.material;
                material.color = Color.Lerp(material.color, new Color(0.9f, 0.58f, 0.24f), 0.35f);
            }
        }
    }
}
