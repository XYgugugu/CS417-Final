using PVZ3D.Core;
using PVZ3D.Grid;
using PVZ3D.Plants;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Interaction
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class PlantDragSeed : MonoBehaviour
    {
        [SerializeField] private PlantDefinition plantDefinition;
        [SerializeField] private float maxDropToCellDistance = 0.95f;
        [SerializeField] private float overlapProbeRadius = 0.26f;

        private PlantPlacementManager placementManager;
        private XRGrabInteractable grabInteractable;
        private Rigidbody body;
        private Transform homeParent;
        private Vector3 homeLocalPosition;
        private Quaternion homeLocalRotation;

        public void Initialize(PlantDefinition definition, PlantPlacementManager manager)
        {
            plantDefinition = definition;
            placementManager = manager;
            homeParent = transform.parent;
            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;
            EnsureComponents();
        }

        private void Awake()
        {
            EnsureComponents();
            homeParent = transform.parent;
            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            EnsureComponents();
            grabInteractable.selectExited.AddListener(HandleSelectExited);
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectExited.RemoveListener(HandleSelectExited);
            }
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            TryPlaceAtDropPoint();
            ReturnHome();
        }

        private void TryPlaceAtDropPoint()
        {
            if (placementManager == null)
            {
                placementManager = PlantPlacementManager.Instance;
            }

            if (placementManager == null || plantDefinition == null || LawnGridManager.Instance == null || GameManager.Instance == null)
            {
                return;
            }

            GamePhase phase = GameManager.Instance.State.Phase;
            if (phase != GamePhase.Prep && phase != GamePhase.Battle)
            {
                return;
            }

            GridCell target = FindDropTargetCell();
            if (target == null)
            {
                GameEvents.RaisePurchaseResult(false, "Drag to a grid cell");
                return;
            }

            bool placed = placementManager.TryPlaceDefinitionAt(plantDefinition, target, true);
            if (!placed)
            {
                GameEvents.RaisePurchaseResult(false, $"Cannot place {plantDefinition.DisplayName} here");
            }
        }

        private GridCell FindDropTargetCell()
        {
            Collider[] nearHits = Physics.OverlapSphere(transform.position, overlapProbeRadius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < nearHits.Length; i++)
            {
                GridCell nearCell = nearHits[i].GetComponentInParent<GridCell>();
                if (nearCell != null)
                {
                    return nearCell;
                }
            }

            Vector3 start = transform.position + Vector3.up * 0.35f;
            if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Collide))
            {
                GridCell hitCell = hit.collider.GetComponentInParent<GridCell>();
                if (hitCell != null)
                {
                    return hitCell;
                }
            }

            LawnGridManager grid = LawnGridManager.Instance;
            GridCell best = null;
            float bestDist = float.MaxValue;
            for (int lane = 0; lane < grid.Lanes; lane++)
            {
                for (int col = 0; col < grid.Columns; col++)
                {
                    GridCell cell = grid.GetCell(lane, col);
                    if (cell == null)
                    {
                        continue;
                    }

                    Vector3 cellPos = cell.transform.position;
                    float dist = Vector2.Distance(new Vector2(cellPos.x, cellPos.z), new Vector2(transform.position.x, transform.position.z));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = cell;
                    }
                }
            }

            if (bestDist <= maxDropToCellDistance)
            {
                return best;
            }

            return null;
        }

        private void ReturnHome()
        {
            if (homeParent != null && transform.parent != homeParent)
            {
                transform.SetParent(homeParent, true);
            }

            transform.localPosition = homeLocalPosition;
            transform.localRotation = homeLocalRotation;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void EnsureComponents()
        {
            if (grabInteractable == null)
            {
                grabInteractable = GetComponent<XRGrabInteractable>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body != null)
            {
                body.useGravity = false;
                body.isKinematic = true;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
        }
    }
}
