using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PVZ3D.Inventory
{
    public class InventorySlotUI : MonoBehaviour,
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Slot Identity")]
        [SerializeField] private bool isQuickSlot;
        [SerializeField] private int index;
        [SerializeField] private Vector2Int gridPosition;

        [Header("Visuals")]
        [SerializeField] private Image background;
        [SerializeField] private Color emptyColor = new Color(0.16f, 0.2f, 0.18f, 1f);
        [SerializeField] private Color occupiedColor = new Color(0.18f, 0.38f, 0.27f, 1f);
        [SerializeField] private Color pointedColor = new Color(0.95f, 0.82f, 0.28f, 1f);

        public bool IsQuickSlot => isQuickSlot;
        public int Index => index;
        public Vector2Int GridPosition => gridPosition;

        public void ConfigureSlot(bool quickSlot, int slotIndex, Vector2Int position)
        {
            isQuickSlot = quickSlot;
            index = slotIndex;
            gridPosition = position;
        }

        private bool isOccupied;
        private bool isPointed;

        private void Awake()
        {
            if (background == null)
            {
                background = GetComponent<Image>();
            }
        }

        public void SetOccupied(bool occupied)
        {
            isOccupied = occupied;
            RefreshVisual();
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                isPointed = false;
            }

            gameObject.SetActive(visible);
        }

        public void OnDrop(PointerEventData eventData)
        {
            InventoryItemIconUI icon = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventoryItemIconUI>()
                : null;

            if (icon == null)
            {
                return;
            }

            icon.MarkDroppedOnSlot();
            InventoryManager.Instance?.MoveItemToSlot(icon.ItemId, this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointed = true;
            RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointed = false;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (background != null)
            {
                background.color = isPointed
                    ? pointedColor
                    : isOccupied ? occupiedColor : emptyColor;
            }
        }
    }
}
