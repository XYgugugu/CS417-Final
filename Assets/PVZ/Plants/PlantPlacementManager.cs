using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Grid;
using PVZ3D.Resources;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PlantPlacementManager : MonoBehaviour
    {
        public static PlantPlacementManager Instance { get; private set; }

        [Header("Plant Catalog")]
        [SerializeField] private List<PlantDefinition> plantDefinitions = new List<PlantDefinition>();
        [SerializeField] private Transform plantRuntimeParent;

        [Header("Default Definition - Sunflower")]
        [SerializeField] private int defaultSunflowerCost = 50;
        [SerializeField] private float defaultSunflowerHealth = 100f;
        [SerializeField] private int defaultSunflowerDropAmount = 25;
        [SerializeField] private float defaultSunflowerDropInterval = 5f;
        [SerializeField] private float defaultSunflowerCooldown = 5f;

        [Header("Default Definition - Peashooter")]
        [SerializeField] private int defaultPeashooterCost = 100;
        [SerializeField] private float defaultPeashooterHealth = 100f;
        [SerializeField] private float defaultPeashooterDamage = 24f;
        [SerializeField] private float defaultPeashooterRate = 1f;
        [SerializeField] private float defaultPeashooterRange = 9f;
        [SerializeField] private float defaultPeashooterCooldown = 5f;

        [Header("Default Definition - Wallnut")]
        [SerializeField] private int defaultWallnutCost = 50;
        [SerializeField] private float defaultWallnutHealth = 500f;
        [SerializeField] private float defaultWallnutCooldown = 15f;

        private int selectedPlantIndex = -1;
        private readonly Dictionary<string, float> cooldownReadyTimes = new Dictionary<string, float>();

        public IReadOnlyList<PlantDefinition> PlantDefinitions => plantDefinitions;
        public int SelectedPlantIndex => selectedPlantIndex;
        public PlantDefinition SelectedPlant => selectedPlantIndex >= 0 && selectedPlantIndex < plantDefinitions.Count ? plantDefinitions[selectedPlantIndex] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureDefaultDefinitions();
        }

        public void SelectPlantByIndex(int index)
        {
            selectedPlantIndex = index >= 0 && index < plantDefinitions.Count ? index : -1;
        }

        public int IndexOfDefinition(PlantDefinition definition)
        {
            return definition == null ? -1 : plantDefinitions.IndexOf(definition);
        }

        public bool TryPlaceSelectedInLane(int lane)
        {
            GridCell target = LawnGridManager.Instance != null ? LawnGridManager.Instance.GetFirstEmptyCellInLane(lane) : null;
            if (target == null)
            {
                GameEvents.RaisePurchaseResult(false, "No empty cell in lane");
                return false;
            }

            return TryPlaceSelectedAt(target);
        }

        public bool TryPlaceSelectedAt(GridCell cell)
        {
            return TryPlaceDefinitionAt(SelectedPlant, cell, true);
        }

        public bool TryPlaceDefinitionAt(PlantDefinition definition, GridCell cell, bool selectPlacedDefinition)
        {
            if (cell == null)
            {
                GameEvents.RaisePurchaseResult(false, "No target cell");
                return false;
            }

            if (GameManager.Instance == null)
            {
                return false;
            }

            GamePhase phase = GameManager.Instance.State.Phase;
            if (phase != GamePhase.Prep && phase != GamePhase.Battle)
            {
                GameEvents.RaisePurchaseResult(false, "Can only place during match");
                return false;
            }

            if (definition == null)
            {
                GameEvents.RaisePurchaseResult(false, "No plant selected");
                return false;
            }

            if (selectPlacedDefinition)
            {
                int index = IndexOfDefinition(definition);
                if (index >= 0)
                {
                    selectedPlantIndex = index;
                }
            }

            if (cell.IsOccupied)
            {
                if (TryUpgradePlant(definition, cell.Occupant))
                {
                    return true;
                }

                GameEvents.RaisePurchaseResult(false, "Cell occupied");
                return false;
            }

            float remainingCooldown = GetRemainingCooldown(definition);
            if (remainingCooldown > 0f)
            {
                GameEvents.RaisePurchaseResult(false, $"{definition.DisplayName} cooling down ({remainingCooldown:F1}s)");
                return false;
            }

            if (!GameManager.Instance.CheatModeEnabled && (ResourceManager.Instance == null || !ResourceManager.Instance.CanAffordSun(definition.SunCost)))
            {
                GameEvents.RaisePurchaseResult(false, "Not enough sun");
                return false;
            }

            if (!GameManager.Instance.CheatModeEnabled && !ResourceManager.Instance.SpendSun(definition.SunCost))
            {
                GameEvents.RaisePurchaseResult(false, "Spend failed");
                return false;
            }

            PlantBase plant = SpawnPlant(definition, cell);
            if (plant == null)
            {
                if (!GameManager.Instance.CheatModeEnabled && ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.AddSun(definition.SunCost, false);
                }

                GameEvents.RaisePurchaseResult(false, "Plant spawn failed");
                return false;
            }

            GameManager.Instance.RegisterPlantPlaced(cell.LaneIndex, cell.ColumnIndex);
            StartCooldown(definition);
            GameEvents.RaisePurchaseResult(true, $"Placed {definition.DisplayName}");
            return true;
        }

        public float GetRemainingCooldown(PlantDefinition definition)
        {
            if (definition == null || definition.Cooldown <= 0f)
            {
                return 0f;
            }

            string key = GetCooldownKey(definition);
            return cooldownReadyTimes.TryGetValue(key, out float readyTime) ? Mathf.Max(0f, readyTime - Time.time) : 0f;
        }

        private PlantBase SpawnPlant(PlantDefinition definition, GridCell cell)
        {
            if (plantRuntimeParent == null)
            {
                GameObject runtime = GameObject.Find("Runtime") ?? new GameObject("Runtime");
                GameObject root = GameObject.Find("Runtime/Plants") ?? new GameObject("Plants");
                root.transform.SetParent(runtime.transform, false);
                plantRuntimeParent = root.transform;
            }

            GameObject plantObject;
            if (definition.Prefab != null)
            {
                plantObject = Instantiate(definition.Prefab, plantRuntimeParent);
            }
            else
            {
                plantObject = CreateFallbackPlantObject(definition);
                plantObject.transform.SetParent(plantRuntimeParent, true);
            }

            PlantBase plant = plantObject.GetComponent<PlantBase>();
            if (plant == null)
            {
                plant = definition.Archetype switch
                {
                    PlantArchetype.Sunflower => plantObject.AddComponent<SunflowerPlant>(),
                    PlantArchetype.Peashooter => plantObject.AddComponent<PeashooterPlant>(),
                    PlantArchetype.Wallnut => plantObject.AddComponent<WallnutPlant>(),
                    _ => plantObject.AddComponent<PlantBase>()
                };
            }

            plant.Initialize(definition, cell);
            return plant;
        }

        private GameObject CreateFallbackPlantObject(PlantDefinition definition)
        {
            GameObject obj = new GameObject(definition.DisplayName);

            switch (definition.Archetype)
            {
                case PlantArchetype.Sunflower:
                    BuildSunflowerVisual(obj.transform);
                    break;
                case PlantArchetype.Peashooter:
                    BuildPeashooterVisual(obj.transform);
                    break;
                case PlantArchetype.Wallnut:
                    BuildWallnutVisual(obj.transform);
                    break;
            }

            return obj;
        }

        private static void BuildSunflowerVisual(Transform root)
        {
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.56f, 0.06f, 0.56f), new Color(0.18f, 0.42f, 0.18f));
            CreateVisualPart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(0.11f, 0.56f, 0.11f), new Color(0.22f, 0.66f, 0.24f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0f), new Vector3(0.18f, 0.18f, 0.18f), new Color(0.52f, 0.33f, 0.11f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0.08f), new Vector3(0.34f, 0.34f, 0.1f), new Color(0.42f, 0.24f, 0.08f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.18f, 1.02f, 0.02f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.98f, 0.86f, 0.22f), Quaternion.Euler(0f, 0f, 22f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.1f, 1.18f, 0.02f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.98f, 0.86f, 0.22f), Quaternion.Euler(0f, 0f, 58f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.08f, 1.2f, 0.02f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.98f, 0.86f, 0.22f), Quaternion.Euler(0f, 0f, 100f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.2f, 1.04f, 0.02f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.98f, 0.86f, 0.22f), Quaternion.Euler(0f, 0f, 145f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.12f, 0.82f, 0.02f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.98f, 0.86f, 0.22f), Quaternion.Euler(0f, 0f, -145f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.08f, 0.78f, 0.02f), new Vector3(0.2f, 0.12f, 0.08f), new Color(0.98f, 0.86f, 0.22f), Quaternion.Euler(0f, 0f, -92f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.16f, 0.45f, 0f), new Vector3(0.18f, 0.08f, 0.28f), new Color(0.34f, 0.72f, 0.26f), Quaternion.Euler(0f, 0f, 34f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.15f, 0.56f, 0f), new Vector3(0.22f, 0.08f, 0.3f), new Color(0.3f, 0.68f, 0.24f), Quaternion.Euler(0f, 0f, -42f));
        }

        private static void BuildPeashooterVisual(Transform root)
        {
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.58f, 0.06f, 0.58f), new Color(0.18f, 0.44f, 0.19f));
            CreateVisualPart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(0.13f, 0.62f, 0.13f), new Color(0.23f, 0.68f, 0.25f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.14f, 0.92f, 0f), new Vector3(0.34f, 0.26f, 0.3f), new Color(0.28f, 0.78f, 0.32f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.24f, 0.92f, 0f), new Vector3(0.26f, 0.2f, 0.22f), new Color(0.3f, 0.8f, 0.34f));
            CreateVisualPart(root, PrimitiveType.Cylinder, new Vector3(0.38f, 0.92f, 0f), new Vector3(0.1f, 0.28f, 0.1f), new Color(0.18f, 0.6f, 0.2f), Quaternion.Euler(90f, 0f, 0f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.49f, 0.92f, 0f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.14f, 0.46f, 0.14f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.03f, 1.04f, 0f), new Vector3(0.12f, 0.12f, 0.12f), new Color(0.1f, 0.3f, 0.1f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.1f, 0.82f, 0.16f), new Vector3(0.1f, 0.1f, 0.1f), new Color(0.08f, 0.24f, 0.08f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.16f, 0.48f, 0f), new Vector3(0.2f, 0.08f, 0.3f), new Color(0.31f, 0.72f, 0.25f), Quaternion.Euler(0f, 0f, -36f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.08f, 0.58f, 0.18f), new Vector3(0.18f, 0.08f, 0.24f), new Color(0.34f, 0.74f, 0.28f), Quaternion.Euler(24f, 0f, 12f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.02f, 0.72f, 0f), new Vector3(0.14f, 0.18f, 0.14f), new Color(0.38f, 0.82f, 0.34f));
        }

        private static void BuildWallnutVisual(Transform root)
        {
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.62f, 0.06f, 0.62f), new Color(0.2f, 0.42f, 0.18f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.62f, 0f), new Vector3(0.72f, 0.92f, 0.7f), new Color(0.56f, 0.36f, 0.18f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.78f, 0.1f), new Vector3(0.56f, 0.54f, 0.4f), new Color(0.62f, 0.4f, 0.2f));
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(-0.02f, 0.89f, 0.26f), new Vector3(0.28f, 0.05f, 0.06f), new Color(0.18f, 0.09f, 0.04f), Quaternion.Euler(0f, 0f, -10f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.08f, 0.84f, 0.28f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.08f, 0.05f, 0.02f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.1f, 0.82f, 0.28f), new Vector3(0.07f, 0.07f, 0.07f), new Color(0.08f, 0.05f, 0.02f));
            CreateVisualPart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.42f, -0.12f), new Vector3(0.1f, 0.14f, 0.1f), new Color(0.48f, 0.3f, 0.16f), Quaternion.Euler(18f, 0f, 90f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.2f, 0.12f, 0f), new Vector3(0.2f, 0.08f, 0.24f), new Color(0.28f, 0.62f, 0.22f), Quaternion.Euler(0f, 0f, -24f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.18f, 0.1f, 0.02f), new Vector3(0.18f, 0.08f, 0.22f), new Color(0.26f, 0.58f, 0.2f), Quaternion.Euler(0f, 0f, 28f));
        }

        private static GameObject CreateVisualPart(Transform parent, PrimitiveType primitive, Vector3 localPos, Vector3 localScale, Color color, Quaternion? localRot = null)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRot ?? Quaternion.identity;

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

            return part;
        }

        private void EnsureDefaultDefinitions()
        {
            if (plantDefinitions.Count > 0)
            {
                return;
            }

            plantDefinitions.Add(CreateRuntimeDefinition("sunflower", "Sunflower", PlantArchetype.Sunflower, defaultSunflowerCost, defaultSunflowerHealth, 0f, 0f, 0f, defaultSunflowerDropAmount, defaultSunflowerDropInterval, defaultSunflowerCooldown));
            plantDefinitions.Add(CreateRuntimeDefinition("peashooter", "Peashooter", PlantArchetype.Peashooter, defaultPeashooterCost, defaultPeashooterHealth, defaultPeashooterDamage, defaultPeashooterRate, defaultPeashooterRange, 0, 0f, defaultPeashooterCooldown));
            plantDefinitions.Add(CreateRuntimeDefinition("wallnut", "Wallnut", PlantArchetype.Wallnut, defaultWallnutCost, defaultWallnutHealth, 0f, 0f, 0f, 0, 0f, defaultWallnutCooldown));
        }

        private PlantDefinition CreateRuntimeDefinition(string id, string name, PlantArchetype type, int cost, float maxHealth, float attackDamage, float attackRate, float attackRange, int sunPerDrop, float sunDropInterval, float cooldown)
        {
            PlantDefinition def = ScriptableObject.CreateInstance<PlantDefinition>();
            def.PlantId = id;
            def.DisplayName = name;
            def.Archetype = type;
            def.SunCost = cost;
            def.MaxHealth = maxHealth;
            def.AttackDamage = attackDamage;
            def.AttackRate = attackRate;
            def.AttackRange = attackRange;
            def.SunPerDrop = sunPerDrop;
            def.SunDropInterval = sunDropInterval;
            def.Cooldown = cooldown;
            return def;
        }

        private void StartCooldown(PlantDefinition definition)
        {
            if (definition != null && definition.Cooldown > 0f)
            {
                cooldownReadyTimes[GetCooldownKey(definition)] = Time.time + definition.Cooldown;
            }
        }

        private static string GetCooldownKey(PlantDefinition definition)
        {
            return string.IsNullOrWhiteSpace(definition.PlantId) ? definition.name : definition.PlantId;
        }

        private bool TryUpgradePlant(PlantDefinition definition, PlantBase occupant)
        {
            if (definition == null || occupant == null || !occupant.CanUpgradeWith(definition))
            {
                return false;
            }

            int upgradeCost = occupant.GetUpgradeCost(definition);
            string upgradeName = occupant.GetUpgradeName(definition);
            if (!GameManager.Instance.CheatModeEnabled)
            {
                if (ResourceManager.Instance == null || !ResourceManager.Instance.CanAffordSun(upgradeCost))
                {
                    GameEvents.RaisePurchaseResult(false, $"Not enough sun for {upgradeName} ({upgradeCost})");
                    return true;
                }

                if (!ResourceManager.Instance.SpendSun(upgradeCost))
                {
                    GameEvents.RaisePurchaseResult(false, "Upgrade spend failed");
                    return true;
                }
            }

            if (!occupant.ApplyUpgrade(definition))
            {
                if (!GameManager.Instance.CheatModeEnabled && ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.AddSun(upgradeCost, false);
                }

                GameEvents.RaisePurchaseResult(false, "Upgrade failed");
                return true;
            }

            GameEvents.RaisePurchaseResult(true, $"Upgraded to {upgradeName}");
            return true;
        }
    }
}
