using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// Drives a fillable health bar from <see cref="GameState.OnHealthChanged"/>.
    /// Attach to the HealthBar GameObject; assign <see cref="fillImage"/> to a
    /// child Image with Image Type = Filled.
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [Tooltip("Image with Image Type = Filled (Horizontal). Fill amount drives the bar width.")]
        [SerializeField] private Image fillImage;

        [Tooltip("Optional label, e.g. \"75 / 100\".")]
        [SerializeField] private TMP_Text label;

        [Header("Color Lerp")]
        [Tooltip("Color when HP is full.")]
        [SerializeField] private Color fullColor = new(0.35f, 0.85f, 0.35f);
        [Tooltip("Color when HP approaches 0.")]
        [SerializeField] private Color emptyColor = new(0.85f, 0.2f, 0.2f);

        private void OnEnable()
        {
            GameState.OnHealthChanged += HandleHealthChanged;
            // Repaint immediately to match current state.
            HandleHealthChanged(GameState.Health, GameState.MaxHealth);
        }

        private void OnDisable()
        {
            GameState.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (max <= 0) max = 1;
            var pct = Mathf.Clamp01(current / (float)max);

            if (fillImage != null)
            {
                fillImage.fillAmount = pct;
                fillImage.color = Color.Lerp(emptyColor, fullColor, pct);
            }
            if (label != null)
            {
                label.text = $"{current} / {max}";
            }
        }
    }
}
