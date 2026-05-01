using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Plants
{
    public class SunPickup : MonoBehaviour
    {
        [SerializeField] private int sunValue = 25;
        [SerializeField] private float collectDelay = 0.25f;
        [SerializeField] private float autoCollectRadius = 0.75f;
        [SerializeField] private float lifetime = 12f;

        private float spawnTime;
        private bool collected;
        private Transform playerTarget;
        private XRSimpleInteractable interactable;

        public static SunPickup Create(Vector3 position)
        {
            return Create(position, 25, false);
        }

        public static SunPickup Create(Vector3 position, int value, bool largeSun)
        {
            GameObject sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sun.name = largeSun ? "Large Sun" : "Sun";
            sun.transform.position = position;
            sun.transform.localScale = Vector3.one * (largeSun ? 0.48f : 0.32f);

            Collider collider = sun.GetComponent<Collider>();
            if (collider is SphereCollider sphereCollider)
            {
                sphereCollider.isTrigger = false;
                sphereCollider.radius = 1.25f;
            }

            Rigidbody rb = sun.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            Renderer renderer = sun.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = largeSun
                    ? new Color(1f, 0.72f, 0.05f)
                    : new Color(1f, 0.86f, 0.1f);
            }

            sun.AddComponent<XRSimpleInteractable>();
            SunPickup pickup = sun.AddComponent<SunPickup>();
            pickup.sunValue = value;
            if (largeSun)
            {
                pickup.autoCollectRadius = 1.0f;
            }

            return pickup;
        }

        private void Awake()
        {
            spawnTime = Time.time;
            interactable = GetComponent<XRSimpleInteractable>();
            Destroy(gameObject, lifetime);
        }

        private void OnEnable()
        {
            if (interactable == null)
            {
                interactable = GetComponent<XRSimpleInteractable>();
            }

            if (interactable == null)
            {
                return;
            }

            interactable.hoverEntered.AddListener(HandleHoverEntered);
            interactable.selectEntered.AddListener(HandleSelectEntered);
        }

        private void OnDisable()
        {
            if (interactable == null)
            {
                return;
            }

            interactable.hoverEntered.RemoveListener(HandleHoverEntered);
            interactable.selectEntered.RemoveListener(HandleSelectEntered);
        }

        private void Update()
        {
            if (collected || Time.time - spawnTime < collectDelay)
            {
                return;
            }

            Transform target = GetPlayerTarget();
            if (target == null)
            {
                return;
            }

            float sqrDistance = (target.position - transform.position).sqrMagnitude;
            if (sqrDistance <= autoCollectRadius * autoCollectRadius)
            {
                Collect();
            }
        }

        private void HandleHoverEntered(HoverEnterEventArgs args)
        {
            if (Time.time - spawnTime >= collectDelay)
            {
                Collect();
            }
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (Time.time - spawnTime >= collectDelay)
            {
                Collect();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || Time.time - spawnTime < collectDelay)
            {
                return;
            }

            if (other.GetComponentInParent<PlantBase>() != null || other.GetComponentInParent<PlantPickup>() != null)
            {
                return;
            }

            Collect();
        }

        private void Collect()
        {
            if (collected)
            {
                return;
            }

            collected = true;
            PlantEconomy.Instance.AddSun(sunValue);
            Destroy(gameObject);
        }

        private Transform GetPlayerTarget()
        {
            if (playerTarget != null)
            {
                return playerTarget;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                playerTarget = mainCamera.transform;
                return playerTarget;
            }

            GameObject xrOrigin = GameObject.Find("XR Origin (XR Rig)");
            if (xrOrigin != null)
            {
                playerTarget = xrOrigin.transform;
            }

            return playerTarget;
        }
    }
}
