using PVZ3D.Zombies;
using UnityEngine;
using PVZ3D.Core;

namespace PVZ3D.Plants
{
    public class PeashooterPlant : PlantBase
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        private float fireTimer;

        public override void Initialize(PlantDefinition plantDefinition, Grid.GridCell cell)
        {
            base.Initialize(plantDefinition, cell);
            if (firePoint == null)
            {
                firePoint = transform;
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

            fireTimer += Time.deltaTime;
            if (fireTimer < definition.AttackRate)
            {
                return;
            }

            ZombieBase target = ZombieBase.GetFirstAliveInLaneAhead(Lane, transform.position.x, definition.AttackRange);
            if (target == null)
            {
                return;
            }

            fireTimer = 0f;
            FireProjectile();
        }

        private void FireProjectile()
        {
            GameObject projectileObj;
            if (projectilePrefab != null)
            {
                projectileObj = Instantiate(projectilePrefab, firePoint.position + Vector3.up * 0.3f, Quaternion.identity);
            }
            else
            {
                projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileObj.transform.position = firePoint.position + Vector3.up * 0.45f;
                projectileObj.transform.localScale = Vector3.one * 0.2f;
                Renderer renderer = projectileObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(0.35f, 0.92f, 0.35f));
                }
            }

            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile == null)
            {
                projectile = projectileObj.AddComponent<Projectile>();
            }

            projectile.Initialize(Lane, definition.AttackDamage, 9f);
            Core.GameEvents.RaisePlantFired(Lane);
        }
    }
}
