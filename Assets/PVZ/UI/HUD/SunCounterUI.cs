using TMPro;
using PVZ3D.Core;
using UnityEngine;

namespace PVZ3D.UI
{
    public class SunCounterUI : MonoBehaviour
    {
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

        [SerializeField] private TMP_Text label;
        [Tooltip("Optional format string. {0} = current sun.")]
        [SerializeField] private string format = "{0}";

        [Header("Pulse on Change (optional)")]
        [SerializeField] private bool pulseOnGain = true;
        [SerializeField] private float pulseScale = 1.25f;
        [SerializeField] private float pulseDuration = 0.25f;

        private int _lastValue = int.MinValue;
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
            RefreshSun();
        }

        private void Update()
        {
            RefreshSun();

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

        private void RefreshSun()
        {
            PlantsEconomy plantsEconomy = ResolvePlantsEconomy();
            if (plantsEconomy == null)
            {
                return;
            }

            int value = plantsEconomy.Sun;
            if (value == _lastValue)
            {
                return;
            }

            HandleSunChanged(value);
        }

        private PlantsEconomy ResolvePlantsEconomy()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager != null ? gameManager.PlantsEconomy : null;
        }

        private void HandleSunChanged(int value)
        {
            if (label != null) label.text = string.Format(format, value);
            if (pulseOnGain && value > _lastValue) _pulseTimer = pulseDuration;
            _lastValue = value;
        }
    }
}
