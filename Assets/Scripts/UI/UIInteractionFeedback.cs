using PVZ3D.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public class UIInteractionFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressedScale = 0.95f;
        [SerializeField] private float lerpSpeed = 14f;

        private RectTransform rectTransform;
        private Image targetImage;
        private Vector3 baseScale = Vector3.one;
        private Color baseColor = Color.white;
        private Color hoverColor = Color.white;
        private Vector3 targetScale = Vector3.one;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                baseScale = rectTransform.localScale;
                targetScale = baseScale;
            }

            targetImage = GetComponent<Image>();
            if (targetImage != null)
            {
                baseColor = targetImage.color;
                hoverColor = new Color(
                    Mathf.Clamp01(baseColor.r + 0.12f),
                    Mathf.Clamp01(baseColor.g + 0.12f),
                    Mathf.Clamp01(baseColor.b + 0.12f),
                    baseColor.a);
            }
        }

        private void Update()
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * lerpSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = baseScale * hoverScale;
            if (targetImage != null)
            {
                targetImage.color = hoverColor;
            }

            AudioFeedbackManager.Instance?.PlayUiHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = baseScale;
            if (targetImage != null)
            {
                targetImage.color = baseColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = baseScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = baseScale * hoverScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioFeedbackManager.Instance?.PlayUiClick();
        }
    }
}
