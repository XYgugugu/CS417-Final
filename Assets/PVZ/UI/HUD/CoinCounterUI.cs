using TMPro;
using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>Same shape as SunCounterUI but for coins.</summary>
    public class CoinCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string format = "{0}";

        private void OnEnable()
        {
            GameState.OnCoinsChanged += Handle;
            Handle(GameState.Coins);
        }

        private void OnDisable()
        {
            GameState.OnCoinsChanged -= Handle;
        }

        private void Handle(int value)
        {
            if (label != null) label.text = string.Format(format, value);
        }
    }
}
