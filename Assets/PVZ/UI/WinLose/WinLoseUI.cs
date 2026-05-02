using TMPro;
using PVZ3D.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// Drives the Win / Lose overlay. Shown by <see cref="HUDController"/> when
    /// the active <see cref="GameManager"/> ends the game.
    /// </summary>
    public class WinLoseUI : MonoBehaviour
    {
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

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
        [Tooltip("Args: 0=score")]
        [SerializeField] private string subtitleFormat = "Score: {0}";

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
            gameManager = ResolveGameManager();
            bool didWin = gameManager != null && gameManager.DidWin;
            int score = gameManager != null ? gameManager.Score : 0;

            if (titleLabel != null)
            {
                titleLabel.text = didWin ? winTitle : loseTitle;
                titleLabel.color = didWin ? winTitleColor : loseTitleColor;
            }
            if (subtitleLabel != null)
            {
                subtitleLabel.text = string.Format(subtitleFormat, score, 0, 0, score);
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

        // ============================================================
        //  Buttons
        // ============================================================

        public void HandleRestart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void HandleMainMenu()
        {
            SceneManager.LoadScene("StartMenu");
        }

        public void HandleQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
