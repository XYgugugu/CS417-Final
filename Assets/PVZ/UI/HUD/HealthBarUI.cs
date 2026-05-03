using TMPro;
using PVZ3D.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Image with Image Type = Filled (Horizontal). Fill amount drives the bar width.")]
        [SerializeField] private Image fillImage;

        [Tooltip("Optional label, e.g. \"75 / 100\".")]
        [SerializeField] private TMP_Text label;

        [Header("Color Lerp")]
        [Tooltip("Color when HP is full.")]
        [SerializeField] private Color fullColor = new(0.35f, 0.85f, 0.35f);
        [Tooltip("Color when HP approaches 0.")]
        [SerializeField] private Color emptyColor = new(0.85f, 0.2f, 0.2f);

        private int lastHealth = int.MinValue;
        private int lastMaxHealth = int.MinValue;

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            PlayerManager playerManager = ResolvePlayerManager();
            if (playerManager == null)
            {
                return;
            }

            int current = playerManager.HP;
            int max = playerManager.MaxHP;
            if (current == lastHealth && max == lastMaxHealth)
            {
                return;
            }

            lastHealth = current;
            lastMaxHealth = max;
            Repaint(current, max);
        }

        private PlayerManager ResolvePlayerManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager != null ? gameManager.PlayerManager : null;
        }

        private void Repaint(int current, int max)
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
