using UnityEngine;

namespace PVZ3D.Plants
{
    public static class PlantVisualFactory
    {
        private const float PickupHeight = 0.75f;
        private const float PlantedHeight = 0.95f;
        private static readonly Quaternion FaceGardenRotation = Quaternion.Euler(0f, 90f, 0f);

        public static PlantPickup CreatePickup(PlantType type, Vector3 position)
        {
            GameObject root = new GameObject($"{GetDisplayName(type)} Pickup");
            root.transform.position = position;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;

            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.radius = 0.35f;
            collider.center = Vector3.up * 0.35f;

            root.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            PlantPickup pickup = root.AddComponent<PlantPickup>();
            pickup.Initialize(type, position);
            AddVisual(type, root.transform, PickupHeight);
            return pickup;
        }

        public static PlantBase CreatePlantedPlant(PlantType type, Vector3 position)
        {
            GameObject root = new GameObject(GetDisplayName(type));
            root.transform.position = position;
            PlantBase plant = root.AddComponent<PlantBase>();
            AddVisual(type, root.transform, PlantedHeight);
            AddBehavior(type, root);
            return plant;
        }

        private static void AddVisual(PlantType type, Transform parent, float targetHeight)
        {
            GameObject model = Resources.Load<GameObject>(GetResourcePath(type));
            if (model != null)
            {
                GameObject visual = Object.Instantiate(model, parent);
                visual.name = $"{GetDisplayName(type)} Model";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = FaceGardenRotation;
                NormalizeVisualSize(visual, targetHeight);
                return;
            }

            CreateFallbackVisual(type, parent);
        }

        private static void NormalizeVisualSize(GameObject visual, float targetHeight)
        {
            Bounds bounds = CalculateBounds(visual);
            if (bounds.size.y <= 0.001f)
            {
                visual.transform.localScale = Vector3.one * 0.25f;
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

        private static void CreateFallbackVisual(PlantType type, Transform parent)
        {
            Color color = type switch
            {
                PlantType.Sunflower => new Color(1f, 0.78f, 0.08f),
                PlantType.Peashooter => new Color(0.18f, 0.72f, 0.24f),
                PlantType.WallNut => new Color(0.63f, 0.38f, 0.16f),
                _ => Color.white
            };

            GameObject body = GameObject.CreatePrimitive(type == PlantType.WallNut ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            body.name = $"{GetDisplayName(type)} Fallback";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = Vector3.up * 0.35f;
            body.transform.localRotation = FaceGardenRotation;
            body.transform.localScale = type == PlantType.WallNut
                ? new Vector3(0.55f, 0.7f, 0.55f)
                : new Vector3(0.35f, 0.7f, 0.35f);

            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            Collider collider = body.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        private static string GetResourcePath(PlantType type)
        {
            return type switch
            {
                PlantType.Sunflower => "Models/Plants/SunFlower/source/Sunflower/sunflower_defaultflower_mesh",
                PlantType.Peashooter => "Models/Plants/pea-repeater-plants-vs-zombies/source/PeaRepeater",
                PlantType.WallNut => "Models/Plants/wall-nut-plants-vs-zombies-1/source/WallNut-PVZ",
                _ => string.Empty
            };
        }

        private static void AddBehavior(PlantType type, GameObject root)
        {
            switch (type)
            {
                case PlantType.Sunflower:
                    root.AddComponent<SunflowerPlant>();
                    break;
                case PlantType.Peashooter:
                    root.AddComponent<PeashooterPlant>();
                    break;
                case PlantType.WallNut:
                    root.AddComponent<WallNutPlant>();
                    break;
            }
        }

        private static string GetDisplayName(PlantType type)
        {
            return type switch
            {
                PlantType.Sunflower => "Sunflower",
                PlantType.Peashooter => "Peashooter",
                PlantType.WallNut => "WallNut",
                _ => "Plant"
            };
        }
    }
}
