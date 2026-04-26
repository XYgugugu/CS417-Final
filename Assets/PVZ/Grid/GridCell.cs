using PVZ3D.Plants;
using PVZ3D.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Grid
{
    public class GridCell : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int laneIndex;
        [SerializeField] private int columnIndex;
        [SerializeField] private PlantBase occupant;
        [SerializeField] private Renderer tileRenderer;
        [SerializeField] private Renderer[] tileRenderers;
        [SerializeField] private Color emptyColor = new Color(0.26f, 0.56f, 0.23f);
        [SerializeField] private Color occupiedColor = new Color(0.18f, 0.35f, 0.15f);
        [SerializeField] private Color hoverColor = new Color(0.38f, 0.7f, 0.34f);
        [SerializeField] private bool enableXrInteractable = true;

        private XRSimpleInteractable xrInteractable;
        private bool xrHovering;

        public int LaneIndex => laneIndex;
        public int ColumnIndex => columnIndex;
        public PlantBase Occupant => occupant;
        public bool IsOccupied => occupant != null;

        public void Initialize(int lane, int column)
        {
            laneIndex = lane;
            columnIndex = column;
            EnsureInteractionBindings();
            UpdateVisual();
        }

        public bool CanPlacePlant()
        {
            return occupant == null;
        }

        public void AssignPlant(PlantBase plant)
        {
            occupant = plant;
            UpdateVisual();
        }

        public void ClearPlant(PlantBase plant)
        {
            if (occupant == plant)
            {
                occupant = null;
                UpdateVisual();
            }
        }

        private void OnMouseDown()
        {
            TryPlaceFromInteraction();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TryPlaceFromInteraction();
        }

        private void OnEnable()
        {
            EnsureInteractionBindings();
        }

        private void OnDisable()
        {
            UnbindXrListeners();
            xrHovering = false;
        }

        private void OnDestroy()
        {
            UnbindXrListeners();
        }

        private void EnsureInteractionBindings()
        {
            if (!enableXrInteractable)
            {
                return;
            }

            if (xrInteractable == null)
            {
                xrInteractable = GetComponent<XRSimpleInteractable>();
                if (xrInteractable == null)
                {
                    xrInteractable = gameObject.AddComponent<XRSimpleInteractable>();
                }
            }

            xrInteractable.selectMode = InteractableSelectMode.Single;
            UnbindXrListeners();
            xrInteractable.hoverEntered.AddListener(HandleHoverEntered);
            xrInteractable.hoverExited.AddListener(HandleHoverExited);
            xrInteractable.selectEntered.AddListener(HandleSelectEntered);
        }

        private void UnbindXrListeners()
        {
            if (xrInteractable == null)
            {
                return;
            }

            xrInteractable.hoverEntered.RemoveListener(HandleHoverEntered);
            xrInteractable.hoverExited.RemoveListener(HandleHoverExited);
            xrInteractable.selectEntered.RemoveListener(HandleSelectEntered);
        }

        private void HandleHoverEntered(HoverEnterEventArgs args)
        {
            xrHovering = true;
            UpdateVisual();
        }

        private void HandleHoverExited(HoverExitEventArgs args)
        {
            xrHovering = false;
            UpdateVisual();
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            TryPlaceFromInteraction();
        }

        private void TryPlaceFromInteraction()
        {
            Plants.PlantPlacementManager.Instance?.TryPlaceSelectedAt(this);
        }

        private void UpdateVisual()
        {
            EnsureRendererCache();

            Color targetColor;
            if (IsOccupied)
            {
                targetColor = occupiedColor;
            }
            else if (xrHovering)
            {
                targetColor = hoverColor;
            }
            else
            {
                targetColor = emptyColor;
            }

            if (tileRenderers == null || tileRenderers.Length == 0)
            {
                return;
            }

            for (int i = 0; i < tileRenderers.Length; i++)
            {
                Renderer r = tileRenderers[i];
                if (r != null)
                {
                    RuntimeVisualMaterialUtility.ApplyColor(r, targetColor);
                }
            }
        }

        private void EnsureRendererCache()
        {
            if (tileRenderers == null || tileRenderers.Length == 0)
            {
                tileRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (tileRenderer == null && tileRenderers != null && tileRenderers.Length > 0)
            {
                tileRenderer = tileRenderers[0];
            }
        }
    }
}