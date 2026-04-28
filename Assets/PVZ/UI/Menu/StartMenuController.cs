using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// Owns the four tombstone buttons on the start menu:
    ///   - <b>New Game</b>  → wipe save, load gameplay scene
    ///   - <b>Continue</b>  → load gameplay scene with existing save (hidden if no save)
    ///   - <b>Cheat</b>     → toggle that grants 9999 sun/coins on the next run
    ///   - <b>Quit</b>      → exit application
    ///
    /// In VR this scene's canvas should be World-Space + TrackedDeviceGraphicRaycaster
    /// so XR pokes work. The Button components themselves are vanilla uGUI.
    /// </summary>
    public class StartMenuController : MonoBehaviour
    {
        [Header("Buttons (Tombstone-styled)")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Toggle cheatToggle;

        [Header("Save Preview")]
        [Tooltip("Optional. Displays existing save info next to the Continue button.")]
        [SerializeField] private TMP_Text savePreviewLabel;
        [SerializeField] private string savePreviewFormat =
            "Continue\n<size=60%>Wave {0}/{1} • {2} sun • {3} coins</size>";

        [Header("Cheat Toggle Visuals")]
        [SerializeField] private TMP_Text cheatStatusLabel;
        [SerializeField] private string cheatOnText = "CHEAT MODE: ON";
        [SerializeField] private string cheatOffText = "Cheat Mode";

        [Header("Scene")]
        [Tooltip("Build-settings scene name to load when starting/continuing. Falls back to GameSettings if assigned.")]
        [SerializeField] private string gameplaySceneName = "Level1_Farm";
        [SerializeField] private GameSettings settings;

        private void Awake()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(HandleNewGame);
            if (continueButton != null) continueButton.onClick.AddListener(HandleContinue);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);
            if (cheatToggle != null) cheatToggle.onValueChanged.AddListener(HandleCheatToggle);
        }

        private void Start()
        {
            RefreshContinueButton();
            RefreshCheatToggle();
        }

        private void OnDestroy()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(HandleNewGame);
            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinue);
            if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);
            if (cheatToggle != null) cheatToggle.onValueChanged.RemoveListener(HandleCheatToggle);
        }

        // ============================================================
        //  Continue button — preview save state
        // ============================================================

        private void RefreshContinueButton()
        {
            var save = SaveSystem.PeekLoad();
            var hasSave = save != null;

            if (continueButton != null) continueButton.gameObject.SetActive(hasSave);

            if (savePreviewLabel != null)
            {
                if (hasSave)
                {
                    savePreviewLabel.gameObject.SetActive(true);
                    savePreviewLabel.text = string.Format(
                        savePreviewFormat,
                        save.currentWave, Mathf.Max(1, save.totalWaves),
                        save.sun, save.coins);
                }
                else
                {
                    savePreviewLabel.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshCheatToggle()
        {
            var on = StartMenuRequest.ConsumeCheatMode(); // doesn't actually consume, persists
            if (cheatToggle != null) cheatToggle.SetIsOnWithoutNotify(on);
            UpdateCheatLabel(on);
        }

        private void UpdateCheatLabel(bool on)
        {
            if (cheatStatusLabel != null) cheatStatusLabel.text = on ? cheatOnText : cheatOffText;
        }

        // ============================================================
        //  Button handlers
        // ============================================================

        public void HandleNewGame()
        {
            SaveSystem.Delete();
            StartMenuRequest.RequestFreshGame();
            LoadGameplayScene();
        }

        public void HandleContinue()
        {
            // No fresh-game flag → bootstrap will load from save.
            LoadGameplayScene();
        }

        public void HandleQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void HandleCheatToggle(bool on)
        {
            StartMenuRequest.SetCheatMode(on);
            UpdateCheatLabel(on);
        }

        private void LoadGameplayScene()
        {
            var name = settings != null && !string.IsNullOrEmpty(settings.gameplaySceneName)
                ? settings.gameplaySceneName
                : gameplaySceneName;
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("[StartMenuController] No gameplay scene name configured.");
                return;
            }
            SceneManager.LoadScene(name);
        }
    }
}
