using UnityEngine;

namespace PVZ3D.Plants
{
    public class PeashooterPlant : PlantBase
    {
        [SerializeField] private float fireInterval = 1f;
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0.45f, 0.65f, 0f) * PlantVisualUtility.PrefabScale;
        [SerializeField] private float projectileDamage = 20f;
        [SerializeField] private float projectileSpeed = 5f;

        private float nextFireTime;

        protected override void Awake()
        {
            base.Awake();
            InitializePlant(
                100f,
                new Vector3(0f, 0.45f, 0f) * PlantVisualUtility.PrefabScale,
                new Vector3(0.8f, 0.9f, 0.8f) * PlantVisualUtility.PrefabScale,
                new Color(0.18f, 0.85f, 0.24f, 1f),
                new Color(0.04f, 0.5f, 0.16f, 0.65f),
                PlantVisualKind.Peashooter);
            nextFireTime = Time.time + FireInterval;
        }

        private void Update()
        {
            if (!IsPlaced || IsDead || Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + FireInterval;
            FirePea();
        }

        private void FirePea()
        {
            Vector3 spawnPosition = transform.position + transform.TransformDirection(muzzleOffset);
            PeaProjectile projectile = PeaProjectile.Create(spawnPosition, transform.right);
            projectile.Initialize(projectileDamage, projectileSpeed);
        }

        private float FireInterval => Mathf.Max(0.05f, fireInterval);
    }
}
