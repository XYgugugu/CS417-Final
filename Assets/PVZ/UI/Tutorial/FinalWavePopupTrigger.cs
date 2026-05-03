using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.UI.Tutorial
{
    /// <summary>
    /// "Final wave!" — fires the moment the last wave starts (wave == totalWaves).
    /// </summary>
    [RequireComponent(typeof(TutorialPopup))]
    public class FinalWavePopupTrigger : MonoBehaviour
    {
        [TextArea]
        [SerializeField] private string message = "Final wave!";

        private TutorialPopup _popup;

        private void Awake() => _popup = GetComponent<TutorialPopup>();

        private void OnEnable()  => ZombieSpawner.OnWaveStarted += HandleWave;
        private void OnDisable() => ZombieSpawner.OnWaveStarted -= HandleWave;

        private void HandleWave(int wave, int totalWaves)
        {
            if (wave != totalWaves) return;
            if (_popup == null) _popup = GetComponent<TutorialPopup>();
            if (_popup != null) _popup.Trigger(message);
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
    }
}
