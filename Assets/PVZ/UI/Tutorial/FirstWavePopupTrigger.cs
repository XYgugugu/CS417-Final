using PVZ3D.Zombies;
using UnityEngine;

namespace PVZ3D.UI.Tutorial
{
    /// <summary>
    /// "The zombies are coming!" — fires the moment the spawner coroutine
    /// kicks off (game start), BEFORE the first zombie actually spawns. Gives
    /// the player a few seconds to read and pre-place plants.
    ///
    /// Subscribes to <see cref="ZombieSpawner.OnSpawnerStarted"/>; no per-frame
    /// polling.
    /// </summary>
    [RequireComponent(typeof(TutorialPopup))]
    public class FirstWavePopupTrigger : MonoBehaviour
    {
        [TextArea]
        [SerializeField] private string message = "The zombies are coming!";

        private TutorialPopup _popup;

        private void Awake() => _popup = GetComponent<TutorialPopup>();

        private void OnEnable()  => ZombieSpawner.OnSpawnerStarted += HandleSpawnerStarted;
        private void OnDisable() => ZombieSpawner.OnSpawnerStarted -= HandleSpawnerStarted;

        private void HandleSpawnerStarted()
        {
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
