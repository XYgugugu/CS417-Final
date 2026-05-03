using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Plants
{
    public class shovel : MonoBehaviour
    {
        private Rigidbody body;
        private Collider shovelCollider;
        private XRGrabInteractable interactable;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            shovelCollider = GetComponent<Collider>();
            interactable = GetComponent<XRGrabInteractable>();
            originalPosition = transform.position;
            originalRotation = transform.rotation;

            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
        }

        private void OnDestroy()
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            body.useGravity = true;
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            RemoveCollidingPlant();

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            transform.SetPositionAndRotation(originalPosition, originalRotation);
        }

        private void RemoveCollidingPlant()
        {
            Bounds bounds = shovelCollider.bounds;
            Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation);

            foreach (Collider hit in hits)
            {
                if (!hit.CompareTag("Plant"))
                {
                    continue;
                }

                PlantBase plant = hit.GetComponent<PlantBase>();
                if (plant != null && plant.IsPlaced)
                {
                    plant.OccupiedCell?.ClearPlant(plant);
                    Destroy(plant.gameObject);
                    return;
                }
            }
        }
    }
}
