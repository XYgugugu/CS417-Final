using UnityEngine;

namespace PVZ3D.Plants
{
    public class SunflowerPlant : MonoBehaviour
    {
        [SerializeField] private float sunInterval = 8f;
        [SerializeField] private Vector3 sunOffset = new Vector3(0f, 1.25f, 0f);
        [SerializeField] private bool isTwinSunflower;

        private float nextSunTime;

        public bool CanUpgradeToTwinSunflower => !isTwinSunflower;

        private void OnEnable()
        {
            nextSunTime = Time.time + sunInterval;
        }

        private void Update()
        {
            if (Time.time < nextSunTime)
            {
                return;
            }

            nextSunTime = Time.time + sunInterval;
            SunPickup.Create(transform.position + sunOffset, isTwinSunflower ? 50 : 25, isTwinSunflower);
        }

        public bool TryUpgradeToTwinSunflower()
        {
            if (!CanUpgradeToTwinSunflower)
            {
                return false;
            }

            isTwinSunflower = true;
            ApplyTwinSunflowerVisual();
            return true;
        }

        private void ApplyTwinSunflowerVisual()
        {
            transform.localScale = Vector3.one * 1.16f;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material material = renderer.material;
                material.color = Color.Lerp(material.color, new Color(1f, 0.95f, 0.15f), 0.45f);
            }
        }
    }
}
