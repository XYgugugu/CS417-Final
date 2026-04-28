using TMPro;
using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// "Welcome back! While you were away (2h 15m), you earned +405 sun, +81 coins."
    /// Auto-hides after a few seconds.
    /// </summary>
    public class IdleProgressToastUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float displaySeconds = 5f;

        [TextArea]
        [SerializeField] private string format =
            "Welcome back!\nAway for <b>{0}</b>\n+{1} sun, +{2} coins";

        private float _hideAt = -1f;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        public void Show(IdleProgressCalculator.Result result)
        {
            if (result.sunGained <= 0 && result.coinsGained <= 0) return;
            if (root != null) root.SetActive(true);
            if (label != null)
            {
                label.text = string.Format(format,
                    IdleProgressCalculator.FormatDuration(result.secondsAway),
                    result.sunGained,
                    result.coinsGained);
            }
            _hideAt = Time.unscaledTime + displaySeconds;
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.unscaledTime >= _hideAt)
            {
                _hideAt = -1f;
                if (root != null) root.SetActive(false);
            }
        }
    }
}
