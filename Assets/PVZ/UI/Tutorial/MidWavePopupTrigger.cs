using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.UI.Tutorial
{
    /// <summary>
    /// "A huge wave of zombies is approaching!" — fires the first time the
    /// wave-progress ratio (current wave ÷ total waves) crosses
    /// <see cref="progressThreshold"/>, but never on the final wave (the
    /// final-wave popup handles that case).
    ///
    /// The ratio test means this adapts automatically when designers change
    /// <c>ZombieSpawner.totalWaves</c>:
    ///   threshold = 2/3, totalWaves = 3 → fires on wave 2 (2/3 ≥ 0.667).
    ///   threshold = 2/3, totalWaves = 6 → fires on wave 4 (4/6 ≥ 0.667).
    ///   threshold = 2/3, totalWaves ≤ 2 → never fires (only first / final exist).
    /// </summary>
    [RequireComponent(typeof(TutorialPopup))]
    public class MidWavePopupTrigger : MonoBehaviour
    {
        [TextArea]
        [SerializeField] private string message = "A huge wave of zombies is approaching!";

        [Tooltip("Fraction of total waves at which to fire (0..1). 2/3 = popup arrives ~67% of the way through.")]
        [Range(0.1f, 0.95f)]
        [SerializeField] private float progressThreshold = 2f / 3f;

        private TutorialPopup _popup;

        private void Awake() => _popup = GetComponent<TutorialPopup>();

        private void OnEnable()  => ZombieSpawner.OnWaveStarted += HandleWave;
        private void OnDisable() => ZombieSpawner.OnWaveStarted -= HandleWave;

        private void HandleWave(int wave, int totalWaves)
        {
            if (totalWaves <= 0) return;
            if (wave >= totalWaves) return;       // final wave belongs to FinalWavePopupTrigger
            if ((float)wave / totalWaves < progressThreshold) return;

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
