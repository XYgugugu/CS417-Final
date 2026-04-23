using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Plants;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PVZ3D.Interaction
{
    public class PlantTraySpawner : MonoBehaviour
    {
        [Header("Tray Placement")]
        [SerializeField] private string roofPath = "Environment/HouseBase/Roof";
        [SerializeField] private Vector3 fallbackTrayCenter = new Vector3(-1.9f, 1.45f, 0f);
        [SerializeField] private float slotSpacing = 1.08f;
        [SerializeField] private float trayLiftOverSurface = 0.14f;

        [Header("Visual")]
        [SerializeField] private Vector3 seedScale = new Vector3(0.33f, 0.2f, 0.24f);
        [SerializeField] private Color sunflowerColor = new Color(0.9f, 0.75f, 0.18f);
        [SerializeField] private Color peashooterColor = new Color(0.22f, 0.68f, 0.25f);
        [SerializeField] private Color wallnutColor = new Color(0.58f, 0.37f, 0.2f);

        private Transform trayRoot;
        private readonly List<GameObject> seedObjects = new List<GameObject>();

        private void Start()
        {
            BuildOrRefreshTray();
            GameEvents.OnGameStarted += BuildOrRefreshTray;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStarted -= BuildOrRefreshTray;
        }

        private void BuildOrRefreshTray()
        {
            PlantPlacementManager placement = PlantPlacementManager.Instance;
            if (placement == null)
            {
                placement = FindFirstObjectByType<PlantPlacementManager>();
            }

            placement?.EnsureDefinitionsForAuthoring();
            if (placement == null || placement.PlantDefinitions == null || placement.PlantDefinitions.Count == 0)
            {
                return;
            }

            if (trayRoot == null)
            {
                GameObject trayObj = new GameObject("PlantTray");
                trayRoot = trayObj.transform;
                Transform env = GameObject.Find("Environment")?.transform;
                if (env != null)
                {
                    trayRoot.SetParent(env, false);
                }
            }

            PositionTrayRoot();
            EnsureSeeds(placement);
        }

#if UNITY_EDITOR
        [ContextMenu("Authoring/Rebuild Plant Tray")]
        public void BuildTrayForAuthoring()
        {
            if (Application.isPlaying)
            {
                return;
            }

            BuildOrRefreshTray();
            EditorUtility.SetDirty(gameObject);
        }
#endif

        private void PositionTrayRoot()
        {
            Transform roof = GameObject.Find(roofPath)?.transform;
            if (roof == null)
            {
                trayRoot.position = fallbackTrayCenter;
                return;
            }

            Renderer roofRenderer = roof.GetComponent<Renderer>();
            if (roofRenderer == null)
            {
                trayRoot.position = roof.position + Vector3.up * trayLiftOverSurface;
                return;
            }

            Bounds b = roofRenderer.bounds;
            trayRoot.position = new Vector3(b.center.x, b.max.y + trayLiftOverSurface, b.center.z);
        }

        private void EnsureSeeds(PlantPlacementManager placement)
        {
            while (seedObjects.Count > 0)
            {
                GameObject old = seedObjects[seedObjects.Count - 1];
                seedObjects.RemoveAt(seedObjects.Count - 1);
                if (old != null)
                {
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(old);
                    }
                    else
                    {
                        Destroy(old);
                    }
                }
            }

            int count = Mathf.Min(3, placement.PlantDefinitions.Count);
            float centerOffset = (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                PlantDefinition def = placement.PlantDefinitions[i];
                Vector3 localPos = new Vector3(0f, 0f, (i - centerOffset) * slotSpacing);
                GameObject seed = CreateSeedObject(def, localPos);
                seedObjects.Add(seed);
            }
        }

        private GameObject CreateSeedObject(PlantDefinition definition, Vector3 localPosition)
        {
            GameObject seed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seed.name = $"Seed_{definition.DisplayName}";
            seed.transform.SetParent(trayRoot, false);
            seed.transform.localPosition = localPosition;
            seed.transform.localRotation = Quaternion.identity;
            seed.transform.localScale = seedScale;

            Renderer renderer = seed.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, GetColorForPlant(definition.Archetype));
            }

            Rigidbody rb = seed.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = seed.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            XRGrabInteractable grab = seed.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                grab = seed.AddComponent<XRGrabInteractable>();
            }

            PlantDragSeed dragSeed = seed.GetComponent<PlantDragSeed>();
            if (dragSeed == null)
            {
                dragSeed = seed.AddComponent<PlantDragSeed>();
            }
            dragSeed.Initialize(definition, PlantPlacementManager.Instance);

            CreateSeedLabel(seed.transform, definition.DisplayName);
            return seed;
        }

        private void CreateSeedLabel(Transform seedRoot, string text)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(seedRoot, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.06f;
            textMesh.fontSize = 42;
            textMesh.color = new Color(0.96f, 0.98f, 0.92f);
        }

        private Color GetColorForPlant(PlantArchetype archetype)
        {
            return archetype switch
            {
                PlantArchetype.Sunflower => sunflowerColor,
                PlantArchetype.Peashooter => peashooterColor,
                PlantArchetype.Wallnut => wallnutColor,
                _ => Color.white
            };
        }
    }
}
