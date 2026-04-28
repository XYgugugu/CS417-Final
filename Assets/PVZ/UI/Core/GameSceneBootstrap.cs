using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PVZ3D.UI
{
    /// <summary>
    /// Place ONE of these in the gameplay scene (Level1_Farm). Drives:
    ///   1. Loading save data (or creating a fresh game).
    ///   2. Applying idle-progress rewards from offline time.
    ///   3. Ticking plant cooldowns each frame.
    ///   4. Auto-saving on quit/pause/scene-unload.
    ///
    /// It's the only MonoBehaviour in the UI/Core layer that lives in the
    /// scene — everything else is either a ScriptableObject asset or a UI
    /// component on the canvas hierarchy.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameSceneBootstrap : MonoBehaviour
    {
        public static GameSceneBootstrap Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private GameSettings settings;

        [Header("Behaviour")]
        [SerializeField] private bool autoLoadOnStart = true;
        [SerializeField] private bool autoSaveOnQuit = true;
        [SerializeField] private float periodicAutoSaveSeconds = 30f;

        public GameSettings Settings => settings;

        public IdleProgressCalculator.Result LastIdleResult { get; private set; }
        public event Action<IdleProgressCalculator.Result> OnIdleProgressApplied;

        private float _autoSaveTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (settings == null)
            {
                Debug.LogError("[GameSceneBootstrap] No GameSettings assigned. " +
                               "Create one (Create → PVZ → Game Settings) and drag it into the Inspector.");
            }
        }

        private void Start()
        {
            if (!autoLoadOnStart) return;

            // If the start menu requested a fresh run, it'll have called
            // ResetForFreshGame() which sets a flag in PlayerPrefs.
            if (StartMenuRequest.ConsumeFreshGameRequest())
            {
                BeginFreshGame();
            }
            else
            {
                BeginFromSaveOrFresh();
            }
        }

        private void Update()
        {
            if (GameState.IsGameOver) return;
            GameState.TickCooldowns(Time.deltaTime);

            if (periodicAutoSaveSeconds > 0f)
            {
                _autoSaveTimer += Time.deltaTime;
                if (_autoSaveTimer >= periodicAutoSaveSeconds)
                {
                    _autoSaveTimer = 0f;
                    SaveSystem.Save();
                }
            }
        }

        // ============================================================
        //  Boot paths
        // ============================================================

        public void BeginFromSaveOrFresh()
        {
            GameState.ResetAll();

            if (SaveSystem.HasSave)
            {
                var save = SaveSystem.Load();
                if (save != null)
                {
                    ApplyCheatPrefs();
                    ApplyIdleRewards(save);
                    return;
                }
            }
            // No save (or load failed) → fresh game.
            ApplyFreshDefaults();
            ApplyCheatPrefs();
        }

        public void BeginFreshGame()
        {
            SaveSystem.Delete();
            GameState.ResetAll();
            ApplyFreshDefaults();
            ApplyCheatPrefs();
            SaveSystem.Save();
        }

        /// <summary>Honor the cheat-mode toggle from the start menu (PlayerPrefs).</summary>
        private void ApplyCheatPrefs()
        {
            if (StartMenuRequest.ConsumeCheatMode())
            {
                GameState.SetCheatMode(true);
            }
        }

        private void ApplyFreshDefaults()
        {
            if (settings == null) return;

            GameState.SetHealth(settings.playerMaxHealth, settings.playerMaxHealth);
            GameState.AddSun(settings.startingSun);
            GameState.AddCoins(settings.startingCoins);

            if (settings.defaultUnlockedPlants != null)
            {
                foreach (var card in settings.defaultUnlockedPlants)
                {
                    if (card != null && card.unlockedByDefault && !string.IsNullOrEmpty(card.plantId))
                    {
                        GameState.UnlockPlant(card.plantId);
                    }
                }
            }
        }

        private void ApplyIdleRewards(SaveData save)
        {
            if (settings == null) return;
            var result = IdleProgressCalculator.Calculate(save, settings, DateTime.UtcNow);
            LastIdleResult = result;
            if (result.sunGained > 0) GameState.AddSun(result.sunGained);
            if (result.coinsGained > 0) GameState.AddCoins(result.coinsGained);
            if (result.sunGained > 0 || result.coinsGained > 0)
            {
                Debug.Log($"[IdleProgress] Away {IdleProgressCalculator.FormatDuration(result.secondsAway)}: " +
                          $"+{result.sunGained} sun, +{result.coinsGained} coins" +
                          (result.wasCapped ? " (capped)" : ""));
            }
            OnIdleProgressApplied?.Invoke(result);
        }

        // ============================================================
        //  Save triggers
        // ============================================================

        public void SaveNow() => SaveSystem.Save();

        private void OnApplicationPause(bool pause)
        {
            // Never write back a Game-Over snapshot — that would corrupt the next launch.
            if (pause && autoSaveOnQuit && !GameState.IsGameOver) SaveSystem.Save();
        }

        private void OnApplicationQuit()
        {
            if (autoSaveOnQuit && !GameState.IsGameOver) SaveSystem.Save();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (autoSaveOnQuit && !GameState.IsGameOver) SaveSystem.Save();
        }

        // ============================================================
        //  Scene helpers (used by Restart / Main Menu buttons)
        // ============================================================

        public void RestartGameplayScene()
        {
            // Wipe save so the next load is a fresh run.
            SaveSystem.Delete();
            GameState.ResetAll();
            var sceneName = settings != null && !string.IsNullOrEmpty(settings.gameplaySceneName)
                ? settings.gameplaySceneName
                : SceneManager.GetActiveScene().name;
            StartMenuRequest.RequestFreshGame();
            SceneManager.LoadScene(sceneName);
        }

        public void GoToStartMenu()
        {
            SaveSystem.Save();
            var sceneName = settings != null && !string.IsNullOrEmpty(settings.startMenuSceneName)
                ? settings.startMenuSceneName
                : "StartMenu";
            SceneManager.LoadScene(sceneName);
        }

        public void QuitGame()
        {
            SaveSystem.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
