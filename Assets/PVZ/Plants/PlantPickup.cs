using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Plants
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class PlantPickup : MonoBehaviour
    {
        [SerializeField] private PlantType plantType;
        [SerializeField] private float snapRadius = 1.4f;
        [SerializeField] private Vector3 homePosition;

        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private bool isHeld;

        public void Initialize(PlantType type, Vector3 spawnPosition)
        {
            plantType = type;
            homePosition = spawnPosition;
            transform.position = spawnPosition;
        }

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        private void OnEnable()
        {
            grabInteractable.selectEntered.AddListener(HandleSelectEntered);
            grabInteractable.selectExited.AddListener(HandleSelectExited);
        }

        private void OnDisable()
        {
            grabInteractable.selectEntered.RemoveListener(HandleSelectEntered);
            grabInteractable.selectExited.RemoveListener(HandleSelectExited);
        }

        private void LateUpdate()
        {
            if (!isHeld)
            {
                ReturnHome();
            }
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            isHeld = true;
            rb.isKinematic = false;
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            isHeld = false;
            TryPlantOrReturnHome();
        }

        private void TryPlantOrReturnHome()
        {
            PlantingCell cell = PlantingCell.FindBestCellForPlant(transform.position, snapRadius, plantType);
            if (cell != null && cell.TryPlant(plantType))
            {
                PlantVisualFactory.CreatePickup(plantType, homePosition);
                Destroy(gameObject);
                return;
            }

            Debug.Log($"PlantPickup: {plantType} released at {transform.position}, but no available soil cell was found below it.");
            ReturnHome();
        }

        private void ReturnHome()
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(homePosition, Quaternion.identity);
        }
    }
}
