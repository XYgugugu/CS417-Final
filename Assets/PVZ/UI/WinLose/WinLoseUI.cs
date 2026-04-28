using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// Drives the Win / Lose overlay. Shown by <see cref="HUDController"/> when
    /// <see cref="GameState.OnGameWon"/> or <see cref="GameState.OnGameLost"/>
    /// fires. Owns the Restart and Quit buttons (1 pt each).
    /// </summary>
    public class WinLoseUI : MonoBehaviour
    {
        [Header("Title / Subtitle")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;

        [Header("Win Copy")]
        [SerializeField] private string winTitle = "VICTORY!";
        [SerializeField] private Color winTitleColor = new(1f, 0.85f, 0.2f);

        [Header("Lose Copy")]
        [SerializeField] private string loseTitle = "THE ZOMBIES ATE YOUR BRAINS";
        [SerializeField] private Color loseTitleColor = new(0.85f, 0.2f, 0.2f);

        [Header("Subtitle Format")]
        [Tooltip("Args: 0=current wave, 1=total waves, 2=zombies defeated, 3=highest wave reached")]
        [SerializeField] private string subtitleFormat =
            "Reached Wave {0} / {1}\nZombies defeated: {2}\nBest ever: Wave {3}";

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            if (restartButton != null) restartButton.onClick.AddListener(HandleRestart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(HandleMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);
        }

        private void OnEnable()
        {
            // Refresh content the moment the overlay appears.
            Refresh();
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestart);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(HandleMainMenu);
            if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);
        }

        private void Refresh()
        {
            if (titleLabel != null)
            {
                titleLabel.text = GameState.DidWin ? winTitle : loseTitle;
                titleLabel.color = GameState.DidWin ? winTitleColor : loseTitleColor;
            }
            if (subtitleLabel != null)
            {
                subtitleLabel.text = string.Format(subtitleFormat,
                    GameState.CurrentWave,
                    Mathf.Max(1, GameState.TotalWaves),
                    GameState.ZombiesDefeated,
                    GameState.HighestWaveReached);
            }
        }

        // ============================================================
        //  Buttons
        // ============================================================

        public void HandleRestart()
        {
            if (GameSceneBootstrap.Instance != null)
            {
                GameSceneBootstrap.Instance.RestartGameplayScene();
            }
            else
            {
                // Fallback: reload current scene.
                SaveSystem.Delete();
                StartMenuRequest.RequestFreshGame();
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
        }

        public void HandleMainMenu()
        {
            if (GameSceneBootstrap.Instance != null) GameSceneBootstrap.Instance.GoToStartMenu();
        }

        public void HandleQuit()
        {
            if (GameSceneBootstrap.Instance != null) GameSceneBootstrap.Instance.QuitGame();
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}
