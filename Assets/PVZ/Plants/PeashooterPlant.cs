using PVZ3D.Core;
using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PeashooterPlant : PlantBase
    {
        private const int DoublePeashooterUpgradeCost = 100;

        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private bool isDoublePeashooter;
        [SerializeField] private Transform secondaryMuzzleVisual;
        [SerializeField] private Transform upgradeLeafVisual;

        private float fireTimer;

        public override void Initialize(PlantDefinition plantDefinition, Grid.GridCell cell)
        {
            base.Initialize(plantDefinition, cell);
            if (firePoint == null)
            {
                firePoint = transform;
            }

            if (isDoublePeashooter)
            {
                EnsureDoublePeashooterVisuals();
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
            FireProjectileBurst();
        }

        public override bool CanUpgradeWith(PlantDefinition upgradeDefinition)
        {
            return !isDoublePeashooter && upgradeDefinition != null && upgradeDefinition.Archetype == PlantArchetype.Peashooter;
        }

        public override int GetUpgradeCost(PlantDefinition upgradeDefinition)
        {
            return CanUpgradeWith(upgradeDefinition) ? DoublePeashooterUpgradeCost : 0;
        }

        public override string GetUpgradeName(PlantDefinition upgradeDefinition)
        {
            return CanUpgradeWith(upgradeDefinition) ? "Double Peashooter" : string.Empty;
        }

        public override bool ApplyUpgrade(PlantDefinition upgradeDefinition)
        {
            if (!CanUpgradeWith(upgradeDefinition))
            {
                return false;
            }

            isDoublePeashooter = true;
            gameObject.name = "Double Peashooter";
            transform.localScale = new Vector3(1.08f, 1.08f, 1.08f);
            RefreshBaseScale();
            EnsureDoublePeashooterVisuals();
            PlayUpgradeFeedback();
            return true;
        }

        private void FireProjectileBurst()
        {
            PlayScalePulse(isDoublePeashooter ? new Vector3(1.12f, 0.92f, 1.12f) : new Vector3(1.07f, 0.95f, 1.07f), 0.12f);
            FireProjectileAtOffset(isDoublePeashooter ? 0.1f : 0f);
            if (isDoublePeashooter)
            {
                FireProjectileAtOffset(-0.1f);
            }

            GameEvents.RaisePlantFired(Lane);
        }

        private void FireProjectileAtOffset(float zOffset)
        {
            GameObject projectileObj;
            Vector3 spawnPosition = firePoint.position + Vector3.up * 0.3f + new Vector3(0f, 0f, zOffset);
            if (projectilePrefab != null)
            {
                projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectileObj.transform.position = spawnPosition + Vector3.up * 0.15f;
                projectileObj.transform.localScale = Vector3.one * 0.2f;
                Renderer renderer = projectileObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    RuntimeVisualMaterialUtility.ApplyColor(renderer, isDoublePeashooter ? new Color(0.52f, 0.98f, 0.42f) : new Color(0.35f, 0.92f, 0.35f));
                }
            }

            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile == null)
            {
                projectile = projectileObj.AddComponent<Projectile>();
            }

            projectile.Initialize(Lane, definition.AttackDamage, 9f);
            SpawnMuzzleFlash(spawnPosition + Vector3.up * 0.12f, zOffset);
        }

        private void EnsureDoublePeashooterVisuals()
        {
            TintPlant(new Color(0.34f, 0.84f, 0.28f), new Color(0.48f, 0.96f, 0.42f));

            if (secondaryMuzzleVisual == null)
            {
                secondaryMuzzleVisual = CreateUpgradePart("SecondMuzzle", PrimitiveType.Cylinder, new Vector3(0.28f, 0.95f, -0.18f), new Vector3(0.09f, 0.26f, 0.09f), new Color(0.2f, 0.68f, 0.22f), Quaternion.Euler(90f, 0f, 0f));
            }

            if (upgradeLeafVisual == null)
            {
                upgradeLeafVisual = CreateUpgradePart("UpgradeLeaf", PrimitiveType.Sphere, new Vector3(-0.02f, 1.12f, -0.16f), new Vector3(0.18f, 0.1f, 0.26f), new Color(0.58f, 0.94f, 0.36f), Quaternion.Euler(0f, 0f, 24f));
            }
        }

        private void TintPlant(Color stemColor, Color accentColor)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Vector3 scale = renderer.transform.localScale;
                bool isStemLike = scale.y > scale.x * 1.4f;
                RuntimeVisualMaterialUtility.ApplyColor(renderer, isStemLike ? stemColor : accentColor);
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

        private void SpawnMuzzleFlash(Vector3 worldPosition, float zOffset)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.transform.position = worldPosition + new Vector3(0.12f, 0f, zOffset * 0.3f);
            flash.transform.localScale = isDoublePeashooter ? new Vector3(0.16f, 0.1f, 0.16f) : new Vector3(0.12f, 0.08f, 0.12f);

            Renderer renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, isDoublePeashooter ? new Color(0.76f, 1f, 0.62f) : new Color(0.62f, 0.98f, 0.56f));
            }

            Collider collider = flash.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Destroy(flash, 0.08f);
        }
    }
}
