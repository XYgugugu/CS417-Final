using TMPro;
using PVZ3D.Core;
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
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

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
        private float _lastRemain = float.NaN;
        private float _lastTotal = float.NaN;
        private bool _lastRunning;

        private void Awake()
        {
            _rt = transform as RectTransform;
            if (_rt != null) _baseScale = _rt.localScale;
            if (visibilityRoot == null) visibilityRoot = gameObject;
        }

        private void OnEnable()
        {
            RefreshTimer(true);
        }

        private void Update()
        {
            RefreshTimer(false);

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
            LossTimer lossTimer = ResolveLossTimer();
            return lossTimer != null &&
                   lossTimer.IsRunning &&
                   lossTimer.TimeRemain > 0f &&
                   lossTimer.TimeRemain <= warningThreshold;
        }

        private void RefreshTimer(bool force)
        {
            LossTimer lossTimer = ResolveLossTimer();
            if (lossTimer == null)
            {
                ApplyVisible(!hideWhenIdle);
                return;
            }

            float remain = lossTimer.TimeRemain;
            float total = lossTimer.TotalTime;
            bool running = lossTimer.IsRunning;

            if (!force &&
                Mathf.Approximately(remain, _lastRemain) &&
                Mathf.Approximately(total, _lastTotal) &&
                running == _lastRunning)
            {
                return;
            }

            _lastRemain = remain;
            _lastTotal = total;
            _lastRunning = running;
            Repaint(remain, total, running);
        }

        private LossTimer ResolveLossTimer()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager != null ? gameManager.LossTimer : null;
        }

        private void Repaint(float remain, float total, bool running)
        {
            ApplyVisible(running || !hideWhenIdle);

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

        private void ApplyVisible(bool visible)
        {
            if (!hideWhenIdle) return;
            if (visibilityRoot == null || visibilityRoot == gameObject) return; // never self-disable
            if (visibilityRoot.activeSelf != visible) visibilityRoot.SetActive(visible);
        }
    }
}
