using UnityEngine;

namespace PVZ3D.Plants
{
    public enum PlantVisualKind
    {
        Sunflower,
        Peashooter,
        WallNut
    }

    public static class PlantVisualUtility
    {
        private static readonly Quaternion ModelRotation = Quaternion.Euler(0f, 90f, 0f);
        private static readonly Quaternion SunRotation = Quaternion.Euler(0f, 90f, 0f);

        public static void EnsurePlantVisual(Transform parent, PlantVisualKind kind)
        {
            if (parent == null || HasVisibleRenderer(parent))
            {
                return;
            }

            GameObject model = Resources.Load<GameObject>(GetModelPath(kind));
            if (model != null)
            {
                GameObject visual = Object.Instantiate(model, parent);
                visual.name = $"{kind} Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = ModelRotation;
                NormalizeVisual(visual, GetTargetHeight(kind));
                return;
            }

            CreateFallbackPlantVisual(parent, kind);
        }

        public static GameObject CreateSunVisual(Vector3 position)
        {
            return CreateSunVisual(position, 1f, 25);
        }

        public static Transform FindVisualRoot(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.GetComponentInChildren<Renderer>(true) != null)
                {
                    return child;
                }
            }

            return null;
        }

        public static void ScaleVisualRoot(Transform parent, Vector3 scaleMultiplier)
        {
            Transform visualRoot = FindVisualRoot(parent);
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localScale = new Vector3(
                visualRoot.localScale.x * scaleMultiplier.x,
                visualRoot.localScale.y * scaleMultiplier.y,
                visualRoot.localScale.z * scaleMultiplier.z);
        }

        public static GameObject CreateSunVisual(Vector3 position, float visualScale, int sunValue)
        {
            GameObject root = new GameObject("Sun Visual");
            root.transform.position = position;
            root.transform.rotation = SunRotation;
            root.transform.localScale = Vector3.one * Mathf.Max(0.1f, visualScale);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Sun Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = Vector3.one * 0.32f;
            ApplyColor(core.GetComponent<Renderer>(), new Color(1f, 0.86f, 0.1f));
            DisableCollider(core);

            CreateSunRay(root.transform, new Vector3(0.34f, 0f, 0f), new Vector3(0.18f, 0.04f, 0.04f));
            CreateSunRay(root.transform, new Vector3(-0.34f, 0f, 0f), new Vector3(0.18f, 0.04f, 0.04f));
            CreateSunRay(root.transform, new Vector3(0f, 0.34f, 0f), new Vector3(0.04f, 0.18f, 0.04f));
            CreateSunRay(root.transform, new Vector3(0f, -0.34f, 0f), new Vector3(0.04f, 0.18f, 0.04f));

            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.radius = 0.45f;
            trigger.isTrigger = true;

            SunCollectible collectible = root.AddComponent<SunCollectible>();
            collectible.SetValue(sunValue);

            return root;
        }

        private static bool HasVisibleRenderer(Transform parent)
        {
            Renderer[] renderers = parent.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateFallbackPlantVisual(Transform parent, PlantVisualKind kind)
        {
            PrimitiveType primitive = kind == PlantVisualKind.WallNut ? PrimitiveType.Sphere : PrimitiveType.Capsule;
            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.name = $"{kind} Fallback Visual";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = Vector3.up * 0.45f;
            visual.transform.localScale = kind == PlantVisualKind.WallNut
                ? new Vector3(0.55f, 0.7f, 0.55f)
                : new Vector3(0.35f, 0.9f, 0.35f);

            Color color = kind switch
            {
                PlantVisualKind.Sunflower => new Color(1f, 0.78f, 0.08f),
                PlantVisualKind.Peashooter => new Color(0.18f, 0.72f, 0.24f),
                PlantVisualKind.WallNut => new Color(0.63f, 0.38f, 0.16f),
                _ => Color.white
            };

            ApplyColor(visual.GetComponent<Renderer>(), color);
            DisableCollider(visual);
        }

        private static void CreateSunRay(Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            GameObject ray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ray.name = "Sun Ray";
            ray.transform.SetParent(parent, false);
            ray.transform.localPosition = localPosition;
            ray.transform.localScale = localScale;
            ApplyColor(ray.GetComponent<Renderer>(), new Color(1f, 0.72f, 0.08f));
            DisableCollider(ray);
        }

        private static void NormalizeVisual(GameObject visual, float targetHeight)
        {
            Bounds bounds = CalculateBounds(visual);
            if (bounds.size.y <= 0.001f)
            {
                return;
            }

            float scale = targetHeight / bounds.size.y;
            visual.transform.localScale *= scale;

            bounds = CalculateBounds(visual);
            Transform parent = visual.transform.parent;
            Vector3 localCenter = parent.InverseTransformPoint(bounds.center);
            Vector3 localMin = parent.InverseTransformPoint(bounds.min);
            visual.transform.localPosition -= new Vector3(localCenter.x, localMin.y, localCenter.z);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static string GetModelPath(PlantVisualKind kind)
        {
            return kind switch
            {
                PlantVisualKind.Sunflower => "Models/Plants/SunFlower/source/Sunflower/sunflower_defaultflower_mesh",
                PlantVisualKind.Peashooter => "Models/Plants/pea-repeater-plants-vs-zombies/source/PeaRepeater",
                PlantVisualKind.WallNut => "Models/Plants/wall-nut-plants-vs-zombies-1/source/WallNut-PVZ",
                _ => string.Empty
            };
        }

        private static float GetTargetHeight(PlantVisualKind kind)
        {
            return kind == PlantVisualKind.WallNut ? 0.85f : 0.95f;
        }

        private static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private static void DisableCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }
    }
}
