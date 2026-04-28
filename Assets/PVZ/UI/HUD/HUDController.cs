using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// Owns the top-level HUD canvas and toggles the WinLose overlay when
    /// the game ends. All actual data binding happens in the per-element
    /// components (HealthBarUI, SunCounterUI, etc.) so this script stays thin.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Canvases")]
        [Tooltip("The main gameplay HUD root (kept active during play, hidden on win/lose).")]
        [SerializeField] private GameObject hudRoot;

        [Tooltip("The Win/Lose overlay root (hidden during play, shown on game over).")]
        [SerializeField] private GameObject winLoseRoot;

        [Tooltip("Optional toast that pops up after applying idle progress.")]
        [SerializeField] private IdleProgressToastUI idleToast;

        private void OnEnable()
        {
            GameState.OnGameWon += HandleGameOver;
            GameState.OnGameLost += HandleGameOver;
            GameState.OnStateReset += HandleReset;

            if (hudRoot != null) hudRoot.SetActive(true);
            if (winLoseRoot != null) winLoseRoot.SetActive(false);

            if (idleToast != null && GameSceneBootstrap.Instance != null)
            {
                GameSceneBootstrap.Instance.OnIdleProgressApplied += idleToast.Show;
            }
        }

        private void OnDisable()
        {
            GameState.OnGameWon -= HandleGameOver;
            GameState.OnGameLost -= HandleGameOver;
            GameState.OnStateReset -= HandleReset;

            if (idleToast != null && GameSceneBootstrap.Instance != null)
            {
                GameSceneBootstrap.Instance.OnIdleProgressApplied -= idleToast.Show;
            }
        }

        private void HandleGameOver()
        {
            if (winLoseRoot != null) winLoseRoot.SetActive(true);
            // We intentionally LEAVE the HUD visible behind the overlay so the
            // player can still see the final scoreboard. If you'd rather hide
            // it, uncomment the line below.
            // if (hudRoot != null) hudRoot.SetActive(false);
        }

        private void HandleReset()
        {
            if (hudRoot != null) hudRoot.SetActive(true);
            if (winLoseRoot != null) winLoseRoot.SetActive(false);
        }
    }
}
