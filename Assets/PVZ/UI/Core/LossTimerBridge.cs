using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// Adapts <see cref="PVZ3D.Core.GameManager"/>'s LossTimer into the
    /// decoupled <see cref="GameState"/> event bus that Aaron's UI listens to.
    ///
    /// <para>Behavior:</para>
    /// <list type="bullet">
    ///   <item>If a <c>GameManager</c> exists in the scene, this bridge mirrors
    ///         its LossTimer each frame and feeds GameState — meaning the
    ///         Countdown UI auto-syncs once Encheng's gameplay code starts the
    ///         timer.</item>
    ///   <item>If no GameManager is found AND <see cref="standaloneDuration"/>
    ///         is &gt; 0, this component runs its own countdown driven by
    ///         <c>Time.deltaTime</c>. Useful for previewing the UI before
    ///         gameplay code is wired up.</item>
    /// </list>
    /// </summary>
    public class LossTimerBridge : MonoBehaviour
    {
        [Tooltip("If no GameManager is found, run a standalone countdown for this many seconds (set 0 to disable).")]
        [SerializeField] private float standaloneDuration = 0f;

        [Tooltip("Auto-start the GameManager's loss timer with this duration on Start. Set 0 to leave whatever GameManager set itself.")]
        [SerializeField] private float overrideGameManagerDuration = 0f;

        private PVZ3D.Core.GameManager _gameManager;
        private bool _bridgeStarted;
        private bool _runningStandalone;

        private void Start()
        {
#if UNITY_2022_2_OR_NEWER
            _gameManager = Object.FindFirstObjectByType<PVZ3D.Core.GameManager>();
#else
            _gameManager = Object.FindObjectOfType<PVZ3D.Core.GameManager>();
#endif

            if (_gameManager != null)
            {
                if (overrideGameManagerDuration > 0f)
                {
                    _gameManager.LossTimer.StartTimer(overrideGameManagerDuration);
                }
                return;
            }

            if (standaloneDuration > 0f)
            {
                GameState.StartLossTimer(standaloneDuration);
                _runningStandalone = true;
            }
        }

        private void Update()
        {
            if (_gameManager != null)
            {
                MirrorGameManagerTimer();
                return;
            }

            if (_runningStandalone && GameState.LossTimerRunning && !GameState.IsGameOver)
            {
                GameState.TickLossTimer(GameState.LossTimerRemain - Time.deltaTime);
            }
        }

        private void MirrorGameManagerTimer()
        {
            var t = _gameManager.LossTimer;
            if (!t.IsRunning)
            {
                if (_bridgeStarted)
                {
                    GameState.StopLossTimer();
                    _bridgeStarted = false;
                }
                return;
            }

            if (!_bridgeStarted)
            {
                // Capture the initial value as "total" so the UI bar can scale.
                GameState.StartLossTimer(t.TimeRemain);
                _bridgeStarted = true;
                return;
            }

            GameState.TickLossTimer(t.TimeRemain);
        }
    }
}
