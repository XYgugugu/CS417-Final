using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// Drop-in visual for tombstone-styled buttons. Adds a subtle "rise out of
    /// the ground" hover/poke animation on top of a regular Unity Button.
    ///
    /// Place this on the same GameObject as a Button. In VR with the XR UI
    /// Input Module, pointer events are dispatched the same as a mouse, so
    /// IPointerEnter/Exit fire on poke-near and poke-leave automatically.
    ///
    /// <para>Implementation note:</para>
    /// The base Y position is captured AFTER an explicit LayoutRebuild in
    /// <see cref="Start"/> — this is critical when the button is a child of a
    /// LayoutGroup (VerticalLayoutGroup / HorizontalLayoutGroup). Capturing in
    /// Awake or in the first Update is too early: Canvas layout passes run
    /// between Update and rendering, so the position read in Awake/Update is
    /// still the scene-file value (typically 0,0). Capturing the wrong base
    /// would make Update lerp every frame back toward 0, dragging all buttons
    /// on top of each other.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TombstoneButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [Header("Hover Animation")]
        [Tooltip("How far (in local units) the tombstone lifts when hovered.")]
        [SerializeField] private float hoverRise = 8f;

        [Tooltip("Animation speed.")]
        [SerializeField] private float lerpSpeed = 12f;

        [Header("Press Animation")]
        [SerializeField] private float pressDip = 4f;
        [SerializeField] private float pressDuration = 0.1f;

        [Header("SFX (optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip pressClip;

        private RectTransform _rt;
        private bool _baseCaptured;
        private float _baseY;
        private float _targetOffset; // hover/press delta on top of _baseY
        private float _pressTimer;

        private void Awake()
        {
            _rt = transform as RectTransform;
        }

        private void OnEnable()
        {
            // Re-capture on every enable: layout state may have changed (e.g.
            // a sibling button became inactive, shifting our row).
            _baseCaptured = false;
        }

        private void Start()
        {
            CaptureBaseAfterLayout();
        }

        /// <summary>
        /// Forces an immediate layout rebuild on the parent layout group,
        /// then snapshots the resulting Y position as the "rest" target.
        /// </summary>
        private void CaptureBaseAfterLayout()
        {
            if (_rt == null) return;
            var parent = _rt.parent as RectTransform;
            if (parent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
            _baseY = _rt.anchoredPosition.y;
            _baseCaptured = true;
        }

        private void Update()
        {
            if (_rt == null || !_baseCaptured) return;

            var goalOffset = _targetOffset;
            if (_pressTimer > 0f)
            {
                _pressTimer -= Time.unscaledDeltaTime;
                goalOffset -= pressDip;
            }

            var pos = _rt.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, _baseY + goalOffset, Time.unscaledDeltaTime * lerpSpeed);
            _rt.anchoredPosition = pos;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetOffset = hoverRise;
            if (audioSource != null && hoverClip != null) audioSource.PlayOneShot(hoverClip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetOffset = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressTimer = pressDuration;
            if (audioSource != null && pressClip != null) audioSource.PlayOneShot(pressClip);
        }
    }
}
