using UnityEngine;

namespace PVZ3D.Plants
{
    public class SunCollectible : MonoBehaviour
    {
        [SerializeField] private int value = 25;
        [SerializeField] private float triggerRadius = 0.45f;

        public int Value => value;

        private void Awake()
        {
            EnsurePrefabSetup();
        }

        public void SetValue(int sunValue)
        {
            value = Mathf.Max(0, sunValue);
        }

        private void EnsurePrefabSetup()
        {
            if (GetComponentInChildren<Renderer>(true) == null)
            {
                CreateSunVisualPart("Sun Core", Vector3.zero, Vector3.one * 0.32f, PrimitiveType.Sphere, new Color(1f, 0.86f, 0.1f));
                CreateSunVisualPart("Sun Ray", new Vector3(0.34f, 0f, 0f), new Vector3(0.18f, 0.04f, 0.04f), PrimitiveType.Cube, new Color(1f, 0.72f, 0.08f));
                CreateSunVisualPart("Sun Ray", new Vector3(-0.34f, 0f, 0f), new Vector3(0.18f, 0.04f, 0.04f), PrimitiveType.Cube, new Color(1f, 0.72f, 0.08f));
                CreateSunVisualPart("Sun Ray", new Vector3(0f, 0.34f, 0f), new Vector3(0.04f, 0.18f, 0.04f), PrimitiveType.Cube, new Color(1f, 0.72f, 0.08f));
                CreateSunVisualPart("Sun Ray", new Vector3(0f, -0.34f, 0f), new Vector3(0.04f, 0.18f, 0.04f), PrimitiveType.Cube, new Color(1f, 0.72f, 0.08f));
            }

            SphereCollider collider = GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<SphereCollider>();
            }

            collider.radius = triggerRadius;
            collider.isTrigger = true;
        }

        private void CreateSunVisualPart(string partName, Vector3 localPosition, Vector3 localScale, PrimitiveType primitive, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}
