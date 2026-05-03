using PVZ3D.Core;
using UnityEngine;

namespace PVZ3D.UI.Tutorial
{
    /// <summary>
    /// Watches <see cref="PlayerManager.HP"/> each frame and fires the attached
    /// <see cref="TutorialPopup"/> the first time the ratio drops below
    /// <see cref="thresholdRatio"/>. Implements the "Critical health" tutorial
    /// pop-up — it teaches the player that they're about to die.
    /// </summary>
    [RequireComponent(typeof(TutorialPopup))]
    public class LowHealthPopupTrigger : MonoBehaviour
    {
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Ratio of HP / MaxHP at or below which the popup fires. Default 0.10 = 10% HP.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float thresholdRatio = 0.10f;

        [TextArea]
        [SerializeField] private string message =
            "Critical health! One more hit and you're out!";

        private TutorialPopup _popup;

        private void Awake()
        {
            _popup = GetComponent<TutorialPopup>();
        }

        private void Update()
        {
            if (_popup == null || _popup.HasFired) return;

            PlayerManager pm = ResolvePlayerManager();
            if (pm == null) return;
            if (pm.MaxHP <= 0) return;

            // Don't fire on the very first frame when HP equals MaxHP
            // (or below threshold but not from damage).
            if (pm.HP <= 0) return;

            float ratio = pm.HP / (float)pm.MaxHP;
            if (ratio <= thresholdRatio)
            {
                _popup.Trigger(message);
            }
        }

        [ContextMenu("Test ▶ Force Trigger Now")]
        private void DebugForceTrigger()
        {
            if (_popup == null) _popup = GetComponent<TutorialPopup>();
            if (_popup != null)
            {
                _popup.Reset();
                _popup.Trigger(message);
            }
        }

        private PlayerManager ResolvePlayerManager()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
            return gameManager != null ? gameManager.PlayerManager : null;
        }
    }
}
