using PVZ3D.Plants;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Region
{
    public class GridCell : MonoBehaviour
    {
        [Header("Placement")]
        [SerializeField] private Transform snapPoint;
        [SerializeField] private bool parentPlacedPlantToCell;
        [SerializeField] private bool disableGrabAfterPlacement = true;

        [Header("Visual Debug")]
        [SerializeField] private Color emptyGizmoColor = new Color(0.2f, 0.8f, 0.25f, 0.35f);
        [SerializeField] private Color occupiedGizmoColor = new Color(0.9f, 0.25f, 0.2f, 0.35f);
        [SerializeField] private Vector3 gizmoSize = new Vector3(1f, 0.05f, 1f);

        private PlantBase currentPlant;

        public bool IsOccupied => currentPlant != null;
        public PlantBase CurrentPlant => currentPlant;

        private Vector3 PlacementPosition => snapPoint != null ? snapPoint.position : transform.position;
        private Quaternion PlacementRotation => snapPoint != null ? snapPoint.rotation : transform.rotation;

        private void Update()
        {
            if (currentPlant == null)
            {
                return;
            }

            if (currentPlant.IsDead)
            {
                currentPlant = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryAcceptPlantFromCollider(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryAcceptPlantFromCollider(other);
        }

        public bool TryPlacePlant(PlantBase plant)
        {
            if (plant == null || IsOccupied || !plant.CanPlaceOn(this))
            {
                return false;
            }

            currentPlant = plant;

            Transform plantTransform = plant.transform;
            plantTransform.SetPositionAndRotation(PlacementPosition, PlacementRotation);
            if (parentPlacedPlantToCell)
            {
                plantTransform.SetParent(transform, true);
            }

            Rigidbody body = plant.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.useGravity = false;
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            XRGrabInteractable grab = plant.GetComponent<XRGrabInteractable>();
            if (grab != null && disableGrabAfterPlacement)
            {
                grab.enabled = false;
            }

            plant.PlaceOnCell(this);
            return true;
        }

        public void ClearPlant(PlantBase plant)
        {
            if (currentPlant != plant)
            {
                return;
            }

            currentPlant = null;
        }

        private void TryAcceptPlantFromCollider(Collider other)
        {
            if (IsOccupied)
            {
                return;
            }

            PlantBase plant = other.GetComponentInParent<PlantBase>();
            if (plant == null || plant.IsPlaced || !plant.CanPlaceOn(this))
            {
                return;
            }

            XRGrabInteractable grab = plant.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected)
            {
                return;
            }

            TryPlacePlant(plant);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsOccupied ? occupiedGizmoColor : emptyGizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(PlacementPosition, PlacementRotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, gizmoSize);
        }
    }
}
