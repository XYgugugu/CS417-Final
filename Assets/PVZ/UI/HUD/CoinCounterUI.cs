using TMPro;
using PVZ3D.Core;
using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>Tiny TMP-text driver bound to the active <see cref="GameManager"/>'s resources.</summary>
    public class CoinCounterUI : MonoBehaviour
    {
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

        [SerializeField] private TMP_Text label;
        [SerializeField] private string format = "{0}";

        private int lastValue = int.MinValue;

        private void OnEnable()
        {
            RefreshCoins();
        }

        private void Update()
        {
            RefreshCoins();
        }

        private void RefreshCoins()
        {
            ResourceManager resourceManager = ResolveResourceManager();
            if (resourceManager == null)
            {
                return;
            }

            int value = resourceManager.Coins;
            if (value == lastValue)
            {
                return;
            }

            lastValue = value;
            Repaint(value);
        }

        private ResourceManager ResolveResourceManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager != null ? gameManager.ResourceManager : null;
        }

        private void Repaint(int value)
        {
            if (label != null) label.text = string.Format(format, value);
        }
    }
}
