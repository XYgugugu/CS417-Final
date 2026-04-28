using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// Loss-timer countdown display: MM:SS text + optional fill bar that drains
    /// left-to-right. Pulses and turns red when below <see cref="warningThreshold"/>
    /// seconds remaining, mimicking PvZ's "wave timer running out" feedback.
    /// </summary>
    public class CountdownUI : MonoBehaviour
    {
        [Header("Time Label")]
        [SerializeField] private TMP_Text timeLabel;
        [Tooltip("How the time is rendered. Args: 0=minutes (zero-padded), 1=seconds (zero-padded), 2=total seconds (int).")]
        [SerializeField] private string timeFormat = "{0:D2}:{1:D2}";

        [Header("Fill Bar (optional)")]
        [Tooltip("Image with Type=Filled, Method=Horizontal. Drains as time runs out.")]
        [SerializeField] private Image fillImage;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new(1f, 0.95f, 0.55f);
        [SerializeField] private Color warningColor = new(0.95f, 0.25f, 0.25f);

        [Header("Warning Pulse")]
        [Tooltip("Time-remaining (seconds) at which the warning color + pulse kick in.")]
        [SerializeField] private float warningThreshold = 10f;
        [SerializeField] private float pulseSpeed = 6f;
        [SerializeField] private float pulseScale = 1.2f;

        [Header("Hide When Idle")]
        [Tooltip("If true, the visibilityRoot is hidden when the loss timer isn't running. Don't enable this without providing a SEPARATE visibilityRoot — toggling self off would deactivate this script.")]
        [SerializeField] private bool hideWhenIdle = false;

        [Tooltip("Optional root to toggle. Must NOT be this GameObject (would self-disable).")]
        [SerializeField] private GameObject visibilityRoot;

        private RectTransform _rt;
        private Vector3 _baseScale = Vector3.one;
        private float _pulseT;

        private void Awake()
        {
            _rt = transform as RectTransform;
            if (_rt != null) _baseScale = _rt.localScale;
            if (visibilityRoot == null) visibilityRoot = gameObject;
        }

        private void OnEnable()
        {
            GameState.OnLossTimerTick += HandleTick;
            GameState.OnStateReset += HandleReset;
            ApplyVisible(GameState.LossTimerRunning);
            HandleTick(GameState.LossTimerRemain, GameState.LossTimerTotal);
        }

        private void OnDisable()
        {
            GameState.OnLossTimerTick -= HandleTick;
            GameState.OnStateReset -= HandleReset;
        }

        private void Update()
        {
            // Only animate the warning pulse when actually below threshold.
            if (!ShouldPulse())
            {
                if (_rt != null && _rt.localScale != _baseScale) _rt.localScale = _baseScale;
                return;
            }
            _pulseT += Time.unscaledDeltaTime * pulseSpeed;
            var s = 1f + (Mathf.Sin(_pulseT) * 0.5f + 0.5f) * (pulseScale - 1f);
            if (_rt != null) _rt.localScale = _baseScale * s;
        }

        private bool ShouldPulse()
        {
            return GameState.LossTimerRunning &&
                   GameState.LossTimerRemain > 0f &&
                   GameState.LossTimerRemain <= warningThreshold;
        }

        private void HandleTick(float remain, float total)
        {
            ApplyVisible(GameState.LossTimerRunning || !hideWhenIdle);

            int totalSeconds = Mathf.CeilToInt(remain);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            if (timeLabel != null) timeLabel.text = string.Format(timeFormat, minutes, seconds, totalSeconds);

            var ratio = total > 0f ? Mathf.Clamp01(remain / total) : 0f;
            if (fillImage != null) fillImage.fillAmount = ratio;

            var warning = ShouldPulse();
            var c = warning ? warningColor : normalColor;
            if (timeLabel != null) timeLabel.color = c;
            if (fillImage != null) fillImage.color = c;
        }

        private void HandleReset()
        {
            ApplyVisible(GameState.LossTimerRunning || !hideWhenIdle);
        }

        private void ApplyVisible(bool visible)
        {
            if (!hideWhenIdle) return;
            if (visibilityRoot == null || visibilityRoot == gameObject) return; // never self-disable
            if (visibilityRoot.activeSelf != visible) visibilityRoot.SetActive(visible);
        }
    }
}
