using PVZ3D.Plants;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PVZ3D.Core
{
    [DisallowMultipleComponent]
    public class InventoryCollectible : MonoBehaviour
    {
        private XRGrabInteractable grabInteractable;

        private void Awake()
        {
            EnsureGrabInteractable();
        }

        private void OnEnable()
        {
            EnsureGrabInteractable();

            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnSelectEntered);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        public void EnsureGrabInteractable()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                return;
            }

            Collider itemCollider = GetComponent<Collider>();
            if (itemCollider == null)
            {
                itemCollider = gameObject.AddComponent<BoxCollider>();
            }

            itemCollider.isTrigger = false;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = true;
            body.isKinematic = false;

            grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grabInteractable.throwOnDetach = false;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactorObject == null ||
                args.interactorObject.handedness != InteractorHandedness.Left)
            {
                return;
            }

            PlantBase plant = GetComponent<PlantBase>();
            if (plant != null && plant.IsPlaced)
            {
                return;
            }

            InventoryManager inventory = InventoryManager.Instance;
            if (inventory != null)
            {
                inventory.TryCollect(gameObject);
            }
        }
    }
}
