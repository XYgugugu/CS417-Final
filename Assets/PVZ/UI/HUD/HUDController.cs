using PVZ3D.Core;
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
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

        [Header("Canvases")]
        [Tooltip("The main gameplay HUD root (kept active during play, hidden on win/lose).")]
        [SerializeField] private GameObject hudRoot;

        [Tooltip("The Win/Lose overlay root (hidden during play, shown on game over).")]
        [SerializeField] private GameObject winLoseRoot;

        private void OnEnable()
        {
            gameManager = ResolveGameManager();
            if (gameManager != null)
            {
                gameManager.OnGameEnded += HandleGameOver;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnGameEnded -= HandleGameOver;
            }
        }

        private GameManager ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager;
        }

        private void HandleGameOver(bool didWin)
        {
            Refresh();
        }

        private void Refresh()
        {
            bool isGameOver = gameManager != null && gameManager.GameOver;

            if (hudRoot != null)
            {
                hudRoot.SetActive(true);
            }

            if (winLoseRoot != null)
            {
                winLoseRoot.SetActive(isGameOver);
            }
        }
    }
}
