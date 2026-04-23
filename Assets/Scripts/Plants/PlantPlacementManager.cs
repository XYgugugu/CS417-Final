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
        [Tooltip("If empty, runtime default definitions below are used.")]
        [SerializeField] private List<PlantDefinition> plantDefinitions = new List<PlantDefinition>();
        [Header("Runtime Parents")]
        [Tooltip("Optional override. If null, Runtime/Plants is used.")]
        [SerializeField] private Transform plantRuntimeParent;

        [Header("Default Definition - Sunflower")]
        [SerializeField] private int defaultSunflowerCost = 45;
        [SerializeField] private float defaultSunflowerHealth = 95f;
        [SerializeField] private int defaultSunflowerDropAmount = 30;
        [SerializeField] private float defaultSunflowerDropInterval = 6.2f;

        [Header("Default Definition - Peashooter")]
        [SerializeField] private int defaultPeashooterCost = 90;
        [SerializeField] private float defaultPeashooterHealth = 105f;
        [SerializeField] private float defaultPeashooterDamage = 24f;
        [SerializeField] private float defaultPeashooterRate = 1f;
        [SerializeField] private float defaultPeashooterRange = 9f;

        [Header("Default Definition - Wallnut")]
        [SerializeField] private int defaultWallnutCost = 70;
        [SerializeField] private float defaultWallnutHealth = 500f;

        private int selectedPlantIndex = -1;

        public IReadOnlyList<PlantDefinition> PlantDefinitions => plantDefinitions;
        public int SelectedPlantIndex => selectedPlantIndex;
        public PlantDefinition SelectedPlant => selectedPlantIndex >= 0 && selectedPlantIndex < plantDefinitions.Count
            ? plantDefinitions[selectedPlantIndex]
            : null;

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

        public void EnsureDefinitionsForAuthoring()
        {
            EnsureDefaultDefinitions();
        }

        public void SelectPlantByIndex(int index)
        {
            if (index < 0 || index >= plantDefinitions.Count)
            {
                selectedPlantIndex = -1;
                return;
            }

            selectedPlantIndex = index;
        }

        public int IndexOfDefinition(PlantDefinition definition)
        {
            if (definition == null)
            {
                return -1;
            }

            return plantDefinitions.IndexOf(definition);
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
                GameEvents.RaisePurchaseResult(false, "Cell occupied");
                return false;
            }

            if (!ResourceManager.Instance.CanAffordSun(definition.SunCost) && !GameManager.Instance.CheatModeEnabled)
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
                if (!GameManager.Instance.CheatModeEnabled)
                {
                    ResourceManager.Instance.AddSun(definition.SunCost, false);
                }
                GameEvents.RaisePurchaseResult(false, "Plant spawn failed");
                return false;
            }

            GameManager.Instance.RegisterPlantPlaced(cell.LaneIndex, cell.ColumnIndex);
            GameEvents.RaisePurchaseResult(true, $"Placed {definition.DisplayName}");
            return true;
        }

        private PlantBase SpawnPlant(PlantDefinition definition, GridCell cell)
        {
            if (plantRuntimeParent == null)
            {
                GameObject root = GameObject.Find("Runtime/Plants");
                if (root == null)
                {
                    GameObject runtime = GameObject.Find("Runtime") ?? new GameObject("Runtime");
                    root = new GameObject("Plants");
                    root.transform.SetParent(runtime.transform, false);
                }

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
            obj.name = definition.DisplayName;
            obj.transform.localScale = Vector3.one;

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
                default:
                    BuildPeashooterVisual(obj.transform);
                    break;
            }

            return obj;
        }

        private static void BuildSunflowerVisual(Transform root)
        {
            CreateVisualPart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.16f, 0.45f, 0.16f), new Color(0.22f, 0.66f, 0.24f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.76f, 0f), new Vector3(0.42f, 0.42f, 0.18f), new Color(0.96f, 0.88f, 0.18f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.76f, 0f), new Vector3(0.2f, 0.2f, 0.1f), new Color(0.44f, 0.28f, 0.08f));
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.5f, 0.06f, 0.5f), new Color(0.2f, 0.48f, 0.2f));
        }

        private static void BuildPeashooterVisual(Transform root)
        {
            CreateVisualPart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.28f, 0f), new Vector3(0.2f, 0.58f, 0.2f), new Color(0.23f, 0.68f, 0.25f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0.08f, 0.84f, 0f), new Vector3(0.34f, 0.28f, 0.28f), new Color(0.28f, 0.78f, 0.32f));
            CreateVisualPart(root, PrimitiveType.Cylinder, new Vector3(0.25f, 0.84f, 0f), new Vector3(0.09f, 0.23f, 0.09f), new Color(0.18f, 0.6f, 0.2f), Quaternion.Euler(90f, 0f, 0f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.1f, 0.92f, 0f), new Vector3(0.12f, 0.12f, 0.12f), new Color(0.1f, 0.3f, 0.1f));
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.52f, 0.06f, 0.52f), new Color(0.2f, 0.5f, 0.2f));
        }

        private static void BuildWallnutVisual(Transform root)
        {
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.58f, 0f), new Vector3(0.74f, 0.9f, 0.7f), new Color(0.56f, 0.36f, 0.18f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.75f, 0.24f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.08f, 0.05f, 0.02f));
            CreateVisualPart(root, PrimitiveType.Sphere, new Vector3(-0.13f, 0.75f, 0.24f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.08f, 0.05f, 0.02f));
            CreateVisualPart(root, PrimitiveType.Cube, new Vector3(0f, 0.03f, 0f), new Vector3(0.56f, 0.06f, 0.56f), new Color(0.23f, 0.47f, 0.2f));
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

            plantDefinitions.Add(CreateRuntimeDefinition("sunflower", "Sunflower", PlantArchetype.Sunflower, defaultSunflowerCost, defaultSunflowerHealth, 0f, 0f, 0f, defaultSunflowerDropAmount, defaultSunflowerDropInterval));
            plantDefinitions.Add(CreateRuntimeDefinition("peashooter", "Peashooter", PlantArchetype.Peashooter, defaultPeashooterCost, defaultPeashooterHealth, defaultPeashooterDamage, defaultPeashooterRate, defaultPeashooterRange, 0, 0f));
            plantDefinitions.Add(CreateRuntimeDefinition("wallnut", "Wallnut", PlantArchetype.Wallnut, defaultWallnutCost, defaultWallnutHealth, 0f, 0f, 0f, 0, 0f));
        }

        private PlantDefinition CreateRuntimeDefinition(
            string id,
            string name,
            PlantArchetype type,
            int cost,
            float maxHealth,
            float attackDamage,
            float attackRate,
            float attackRange,
            int sunPerDrop,
            float sunDropInterval)
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
            return def;
        }
    }
}
