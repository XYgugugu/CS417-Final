using UnityEngine;

namespace PVZ3D.Plants
{
    public class SunflowerPlant : PlantBase
    {
        [SerializeField] private float sunInterval = 8f;
        [SerializeField] private Vector3 sunSpawnOffset = new Vector3(0f, 1.25f, 0f);
        [SerializeField] private int normalSunValue = 25;

        private float nextSunTime;

        protected override void Awake()
        {
            base.Awake();
            InitializePlant(
                100f,
                new Vector3(0f, 0.45f, 0f) * PlantVisualUtility.PrefabScale,
                new Vector3(0.8f, 0.9f, 0.8f) * PlantVisualUtility.PrefabScale,
                new Color(1f, 0.86f, 0.08f, 1f),
                new Color(0.42f, 0.82f, 0.24f, 0.65f),
                PlantVisualKind.Sunflower);
            nextSunTime = Time.time + sunInterval;
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
            PlantVisualUtility.CreateSunVisual(transform.position + sunSpawnOffset, 1f, normalSunValue);
        }
    }
}
