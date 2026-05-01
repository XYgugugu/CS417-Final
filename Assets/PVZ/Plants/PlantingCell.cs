using System.Collections.Generic;
using UnityEngine;

namespace PVZ3D.Plants
{
    [DisallowMultipleComponent]
    public class PlantingCell : MonoBehaviour
    {
        private static readonly List<PlantingCell> ActiveCells = new List<PlantingCell>();

        [SerializeField] private PlantBase occupant;
        [SerializeField] private float snapHeight = 0.08f;
        [SerializeField] private float pickupSnapRadius = 1.15f;
        [SerializeField] private float horizontalBoundsPadding = 0.35f;
        [SerializeField] private float occupiedPositionRadius = 0.65f;

        private Bounds plantingBounds;
        private bool hasPlantingBounds;

        public PlantBase Occupant => occupant;
        public bool IsOccupied => occupant != null;
        public float PickupSnapRadius => pickupSnapRadius;

        private void OnEnable()
        {
            if (!ActiveCells.Contains(this))
            {
                ActiveCells.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveCells.Remove(this);
        }

        private void Awake()
        {
            RefreshPlantingBounds();
        }

        public bool TryPlant(PlantType plantType)
        {
            if (CanUpgradeWith(plantType))
            {
                return TryUpgradePlant(plantType);
            }

            Vector3 plantPosition = GetPlantPosition();
            if (IsOccupied || HasPlantNearPosition(plantPosition))
            {
                return false;
            }

            if (!PlantEconomy.Instance.TrySpendForPlant(plantType, out string reason))
            {
                Debug.Log($"PlantingCell: Cannot plant {plantType}. {reason}");
                return false;
            }

            PlantBase plant = PlantVisualFactory.CreatePlantedPlant(plantType, plantPosition);
            if (plant == null)
            {
                PlantEconomy.Instance.AddSun(PlantStats.Get(plantType).Cost);
                return false;
            }

            occupant = plant;
            plant.Initialize(plantType, this);
            return true;
        }

        private bool TryUpgradePlant(PlantType plantType)
        {
            if (!PlantEconomy.Instance.TrySpendForPlant(plantType, out string reason))
            {
                Debug.Log($"PlantingCell: Cannot upgrade {plantType}. {reason}");
                return false;
            }

            if (!TryApplyUpgrade(plantType))
            {
                PlantEconomy.Instance.AddSun(PlantStats.Get(plantType).Cost);
                return false;
            }

            Debug.Log($"{plantType} upgraded.");
            return true;
        }

        private bool TryApplyUpgrade(PlantType plantType)
        {
            switch (plantType)
            {
                case PlantType.Sunflower:
                    SunflowerPlant sunflower = occupant.GetComponent<SunflowerPlant>();
                    return sunflower != null && sunflower.TryUpgradeToTwinSunflower();
                case PlantType.Peashooter:
                    PeashooterPlant peashooter = occupant.GetComponent<PeashooterPlant>();
                    return peashooter != null && peashooter.TryUpgradeToRepeater();
                case PlantType.WallNut:
                    WallNutPlant wallNut = occupant.GetComponent<WallNutPlant>();
                    return wallNut != null && wallNut.TryUpgradeToTallNut();
                default:
                    return false;
            }
        }

        private bool CanUpgradeWith(PlantType plantType)
        {
            if (occupant == null || occupant.IsDead)
            {
                return false;
            }

            if (plantType != occupant.PlantType)
            {
                return false;
            }

            switch (plantType)
            {
                case PlantType.Sunflower:
                    SunflowerPlant sunflower = occupant.GetComponent<SunflowerPlant>();
                    return sunflower != null && sunflower.CanUpgradeToTwinSunflower;
                case PlantType.Peashooter:
                    PeashooterPlant peashooter = occupant.GetComponent<PeashooterPlant>();
                    return peashooter != null && peashooter.CanUpgradeToRepeater;
                case PlantType.WallNut:
                    WallNutPlant wallNut = occupant.GetComponent<WallNutPlant>();
                    return wallNut != null && wallNut.CanUpgradeToTallNut;
                default:
                    return false;
            }
        }

        private bool HasPlantNearPosition(Vector3 position)
        {
            PlantBase[] plants = FindObjectsByType<PlantBase>(FindObjectsSortMode.None);
            float maxSqrDistance = occupiedPositionRadius * occupiedPositionRadius;

            for (int i = 0; i < plants.Length; i++)
            {
                PlantBase plant = plants[i];
                if (plant == null || plant.IsDead)
                {
                    continue;
                }

                Vector2 plantPosition = new Vector2(plant.transform.position.x, plant.transform.position.z);
                Vector2 targetPosition = new Vector2(position.x, position.z);
                if ((plantPosition - targetPosition).sqrMagnitude <= maxSqrDistance)
                {
                    return true;
                }
            }

            return false;
        }

        public void Clear(PlantBase plant)
        {
            if (occupant == plant)
            {
                occupant = null;
            }
        }

        public Vector3 GetPlantPosition()
        {
            Vector3 center = hasPlantingBounds ? plantingBounds.center : transform.position;
            return new Vector3(center.x, transform.position.y + snapHeight, center.z);
        }

        public static PlantingCell FindBestAvailableCell(Vector3 position, float maxDistance)
        {
            PlantingCell best = null;
            float bestSqrDistance = maxDistance * maxDistance;

            for (int i = 0; i < ActiveCells.Count; i++)
            {
                PlantingCell cell = ActiveCells[i];
                if (cell == null || cell.IsOccupied)
                {
                    continue;
                }

                float sqrDistance = (cell.transform.position - position).sqrMagnitude;
                if (sqrDistance <= bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = cell;
                }
            }

            return best;
        }

        public static PlantingCell FindBestAvailableCellBelow(Vector3 position, float fallbackMaxDistance)
        {
            return FindBestCellForPlant(position, fallbackMaxDistance, null);
        }

        public static PlantingCell FindBestCellForPlant(Vector3 position, float fallbackMaxDistance, PlantType? plantType)
        {
            PlantingCell best = null;
            float bestDistance = float.PositiveInfinity;
            bool isAboveOccupiedCell = false;

            for (int i = 0; i < ActiveCells.Count; i++)
            {
                PlantingCell cell = ActiveCells[i];
                if (cell == null)
                {
                    continue;
                }

                if (!cell.IsPositionAboveVisibleSoil(position, 0f))
                {
                    continue;
                }

                if (cell.IsOccupied && !cell.CanAcceptPlant(plantType))
                {
                    isAboveOccupiedCell = true;
                    continue;
                }

                float distance = cell.GetHorizontalDistanceToCenter(position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = cell;
                }
            }

            if (isAboveOccupiedCell)
            {
                return null;
            }

            if (best != null)
            {
                return best;
            }

            for (int i = 0; i < ActiveCells.Count; i++)
            {
                PlantingCell cell = ActiveCells[i];
                if (cell == null)
                {
                    continue;
                }

                if (cell.IsPositionAboveVisibleSoil(position, cell.horizontalBoundsPadding))
                {
                    if (cell.IsOccupied && !cell.CanAcceptPlant(plantType))
                    {
                        isAboveOccupiedCell = true;
                        continue;
                    }

                    float distance = cell.GetHorizontalDistanceToCenter(position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = cell;
                    }
                }
            }

            if (isAboveOccupiedCell)
            {
                return null;
            }

            if (best != null)
            {
                return best;
            }

            return FindBestAvailableCell(position, fallbackMaxDistance);
        }

        private bool CanAcceptPlant(PlantType? plantType)
        {
            return !IsOccupied || (plantType.HasValue && CanUpgradeWith(plantType.Value));
        }

        private bool IsPositionAboveVisibleSoil(Vector3 position, float horizontalPadding)
        {
            if (!hasPlantingBounds)
            {
                RefreshPlantingBounds();
            }

            Bounds bounds = hasPlantingBounds
                ? plantingBounds
                : new Bounds(transform.position, new Vector3(pickupSnapRadius * 2f, 0.5f, pickupSnapRadius * 2f));

            bounds.Expand(new Vector3(horizontalPadding * 2f, 0f, horizontalPadding * 2f));

            bool withinHorizontalBounds =
                position.x >= bounds.min.x &&
                position.x <= bounds.max.x &&
                position.z >= bounds.min.z &&
                position.z <= bounds.max.z;

            return withinHorizontalBounds && position.y >= bounds.min.y;
        }

        private float GetHorizontalDistanceToCenter(Vector3 position)
        {
            Vector3 center = hasPlantingBounds ? plantingBounds.center : transform.position;
            Vector2 center2D = new Vector2(center.x, center.z);
            Vector2 position2D = new Vector2(position.x, position.z);
            return (center2D - position2D).sqrMagnitude;
        }

        private float GetTopY()
        {
            if (!hasPlantingBounds)
            {
                RefreshPlantingBounds();
            }

            return hasPlantingBounds ? plantingBounds.max.y : transform.position.y;
        }

        private void RefreshPlantingBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            hasPlantingBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasPlantingBounds)
                {
                    plantingBounds = renderer.bounds;
                    hasPlantingBounds = true;
                }
                else
                {
                    plantingBounds.Encapsulate(renderer.bounds);
                }
            }
        }
    }
}
