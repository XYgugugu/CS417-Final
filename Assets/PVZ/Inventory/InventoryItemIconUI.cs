using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PVZ3D.Inventory
{
    public class InventoryItemIconUI : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Visuals")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Color fallbackIconColor = new Color(0.78f, 0.82f, 0.76f, 1f);
        [SerializeField] private Outline fallbackSelectionOutline;

        private RectTransform rectTransform;
        private Canvas rootCanvas;
        private RectTransform dragRoot;

        private int itemId;
        private bool droppedOnSlot;

        public int ItemId => itemId;

        private void Awake()
        {
            rectTransform = transform as RectTransform;

            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (selectionHighlight == null)
            {
                fallbackSelectionOutline = GetComponent<Outline>();

                if (fallbackSelectionOutline == null)
                {
                    fallbackSelectionOutline = gameObject.AddComponent<Outline>();
                    fallbackSelectionOutline.effectColor = new Color(1f, 0.82f, 0.18f, 1f);
                    fallbackSelectionOutline.effectDistance = new Vector2(4f, -4f);
                }

                fallbackSelectionOutline.enabled = false;
            }
        }

        public void Initialize(int id, Sprite icon, Canvas canvas, RectTransform dragParent)
        {
            itemId = id;
            rootCanvas = canvas;
            dragRoot = dragParent;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.color = icon != null ? Color.white : fallbackIconColor;
                iconImage.preserveAspect = icon != null;
                iconImage.enabled = true;
                iconImage.raycastTarget = true;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.gameObject.SetActive(selected);
            }
            else if (fallbackSelectionOutline != null)
            {
                fallbackSelectionOutline.enabled = selected;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            InventoryManager.Instance?.SelectItem(itemId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            droppedOnSlot = false;

            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }

            if (dragRoot != null)
            {
                transform.SetParent(dragRoot, true);
            }
            else if (rootCanvas != null)
            {
                transform.SetParent(rootCanvas.transform, true);
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rectTransform == null)
            {
                return;
            }

            if (rootCanvas != null && rootCanvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    rootCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector3 worldPoint
                );

                rectTransform.position = worldPoint;
            }
            else
            {
                rectTransform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            InventoryManager.Instance?.EndIconDrag(itemId, droppedOnSlot);
        }

        public void MarkDroppedOnSlot()
        {
            droppedOnSlot = true;
        }
    }
}
