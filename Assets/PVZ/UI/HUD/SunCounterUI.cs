using TMPro;
using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>Tiny TMP-text driver bound to <see cref="GameState.OnSunChanged"/>.</summary>
    public class SunCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [Tooltip("Optional format string. {0} = current sun.")]
        [SerializeField] private string format = "{0}";

        [Header("Pulse on Change (optional)")]
        [SerializeField] private bool pulseOnGain = true;
        [SerializeField] private float pulseScale = 1.25f;
        [SerializeField] private float pulseDuration = 0.25f;

        private int _lastValue;
        private float _pulseTimer;
        private Vector3 _baseScale = Vector3.one;
        private RectTransform _rt;

        private void Awake()
        {
            _rt = transform as RectTransform;
            if (_rt != null) _baseScale = _rt.localScale;
        }

        private void OnEnable()
        {
            GameState.OnSunChanged += HandleSunChanged;
            HandleSunChanged(GameState.Sun);
        }

        private void OnDisable()
        {
            GameState.OnSunChanged -= HandleSunChanged;
        }

        private void HandleSunChanged(int value)
        {
            if (label != null) label.text = string.Format(format, value);
            if (pulseOnGain && value > _lastValue) _pulseTimer = pulseDuration;
            _lastValue = value;
        }

        private void Update()
        {
            if (_rt == null) return;
            if (_pulseTimer > 0f)
            {
                _pulseTimer -= Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(_pulseTimer / Mathf.Max(0.01f, pulseDuration));
                var s = Mathf.Lerp(1f, pulseScale, t);
                _rt.localScale = _baseScale * s;
            }
            else if (_rt.localScale != _baseScale)
            {
                _rt.localScale = _baseScale;
            }
        }
    }
}
