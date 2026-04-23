using PVZ3D.Plants;
using PVZ3D.Resources;
using PVZ3D.Save;
using PVZ3D.Waves;
using PVZ3D.Zombies;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PVZ3D.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Match Defaults")]
        [Tooltip("Starting sun for a new or fresh save run.")]
        [SerializeField] private int startingSun = 200;
        [Tooltip("Starting coins for a new or fresh save run.")]
        [SerializeField] private int startingCoins;
        [Tooltip("Base HP. Reaching 0 triggers defeat.")]
        [SerializeField] private int baseMaxHealth = 9;
        [Tooltip("How many waves are played in one run.")]
        [SerializeField] private int totalWaves = 3;
        [Tooltip("Prep duration before zombies begin spawning.")]
        [SerializeField] private float prepDurationSeconds = 4.5f;

        [Header("Idle Progress Tuning")]
        [Tooltip("Offline sun gain per minute.")]
        [SerializeField] private int idleSunPerMinute = 4;
        [Tooltip("Offline coin gain per minute.")]
        [SerializeField] private int idleCoinsPerMinute = 1;
        [Tooltip("Maximum offline minutes counted for idle rewards.")]
        [SerializeField] private int idleMaxMinutes = 30;

        [Header("Debug")]
        [SerializeField] private bool cheatModeEnabled;

        public GameState State { get; } = new GameState();
        public RunStats CurrentRunStats { get; } = new RunStats();
        public IdleProgressResult LastIdleProgressResult { get; private set; }

        public int BaseMaxHealth => baseMaxHealth;
        public float PrepDurationSeconds => prepDurationSeconds;
        public bool CheatModeEnabled => cheatModeEnabled;

        private bool allWavesSpawned;
        private bool resultTriggered;
        private int runStartSun;
        private int runStartCoins;
        private GamePhase phaseBeforePause = GamePhase.Battle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            State.TotalWaves = totalWaves;
            State.Phase = GamePhase.Menu;
            State.BaseHealth = baseMaxHealth;
        }

        private void Start()
        {
            GameEvents.OnSunChanged += HandleSunChanged;
            GameEvents.OnCoinsChanged += HandleCoinsChanged;
            InitializeFromSave();
            EnterMenuState();
        }

        private void OnDestroy()
        {
            GameEvents.OnSunChanged -= HandleSunChanged;
            GameEvents.OnCoinsChanged -= HandleCoinsChanged;
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Instance?.SaveCurrentState();
        }

        public void InitializeFromSave()
        {
            SaveData save = SaveSystem.Instance != null ? SaveSystem.Instance.CurrentData : SaveData.CreateDefault();
            cheatModeEnabled = save.CheatModeEnabled;
            State.CheatMode = cheatModeEnabled;

            IdleProgressResult idle = IdleProgressCalculator.Calculate(
                save.LastSessionUtc,
                System.DateTime.UtcNow,
                idleSunPerMinute,
                idleCoinsPerMinute,
                idleMaxMinutes);
            LastIdleProgressResult = idle;
            int loadedSun = Mathf.Max(startingSun, save.LastKnownSun) + idle.AwardedSun;
            int loadedCoins = Mathf.Max(startingCoins, save.LastKnownCoins) + idle.AwardedCoins;
            runStartSun = loadedSun;
            runStartCoins = loadedCoins;

            ResourceManager.Instance.Initialize(loadedSun, loadedCoins);
            State.Sun = loadedSun;
            State.Coins = loadedCoins;

            if (idle.HasRewards)
            {
                GameEvents.RaiseIdleProgressApplied(idle);
            }

            GameEvents.RaiseCheatModeChanged(cheatModeEnabled);
        }

        public void EnterMenuState()
        {
            Time.timeScale = 1f;
            StopCurrentMatch();
            CleanupRuntimeEntities();
            SetPhase(GamePhase.Menu);
        }

        public void StartMatch()
        {
            Time.timeScale = 1f;
            StopCurrentMatch();
            CleanupRuntimeEntities();

            CurrentRunStats.Reset();
            resultTriggered = false;
            allWavesSpawned = false;
            State.CurrentWave = 0;
            State.TotalWaves = totalWaves;
            State.BaseHealth = baseMaxHealth;
            State.PlacedPlants = 0;
            State.AliveZombies = 0;
            ResourceManager.Instance?.Initialize(runStartSun, runStartCoins);
            State.Sun = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentSun : runStartSun;
            State.Coins = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentCoins : runStartCoins;
            PlantPlacementManager.Instance?.SelectPlantByIndex(0);

            SetPhase(GamePhase.Prep);
            GameEvents.RaiseBaseHealthChanged(State.BaseHealth, baseMaxHealth);
            GameEvents.RaiseWaveChanged(State.CurrentWave, State.TotalWaves);
            GameEvents.RaiseGameStarted();
            SunSpawner.Instance?.ResetTimer();

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.BeginMatch(State.TotalWaves, prepDurationSeconds);
            }
            else
            {
                Debug.LogError("GameManager: WaveManager missing; cannot start match.");
            }
        }

        public void PauseMatch()
        {
            if (State.Phase != GamePhase.Prep && State.Phase != GamePhase.Battle)
            {
                return;
            }

            phaseBeforePause = State.Phase;
            SetPhase(GamePhase.Paused);
            Time.timeScale = 0f;
        }

        public void ResumePausedMatch()
        {
            if (State.Phase != GamePhase.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            if (phaseBeforePause != GamePhase.Prep && phaseBeforePause != GamePhase.Battle)
            {
                phaseBeforePause = GamePhase.Battle;
            }

            SetPhase(phaseBeforePause);
        }

        public void RestartMatch()
        {
            StartMatch();
        }

        public void ReturnToMenu()
        {
            EnterMenuState();
        }

        public void QuitGame()
        {
            SaveSystem.Instance?.SaveCurrentState();
#if UNITY_EDITOR
            Debug.Log("Quit requested (Editor): stopping play mode is expected in editor runtime.");
#endif
            Application.Quit();
        }

        public void SetCheatMode(bool enabled)
        {
            cheatModeEnabled = enabled;
            State.CheatMode = enabled;
            GameEvents.RaiseCheatModeChanged(enabled);
            SaveSystem.Instance?.SaveCurrentState();
        }

        public void SetPhase(GamePhase phase)
        {
            State.Phase = phase;
            GameEvents.RaiseGamePhaseChanged(phase);
        }

        public void OnPrepEnded()
        {
            if (State.Phase == GamePhase.Prep)
            {
                SetPhase(GamePhase.Battle);
            }
        }

        public void SetWave(int currentWave, int maxWaves)
        {
            State.CurrentWave = currentWave;
            State.TotalWaves = maxWaves;
            CurrentRunStats.WavesCleared = Mathf.Max(CurrentRunStats.WavesCleared, currentWave - 1);
            GameEvents.RaiseWaveChanged(currentWave, maxWaves);
        }

        public void MarkWaveCompleted(int completedWave)
        {
            CurrentRunStats.WavesCleared = Mathf.Max(CurrentRunStats.WavesCleared, completedWave);
            GameEvents.RaiseWaveCompleted(completedWave);
        }

        public void MarkAllWavesSpawned()
        {
            allWavesSpawned = true;
            GameEvents.RaiseAllWavesCompleted();
            TryResolveWinState();
        }

        public void DamageBase(int amount)
        {
            if (resultTriggered || amount <= 0)
            {
                return;
            }

            State.BaseHealth = Mathf.Max(0, State.BaseHealth - amount);
            GameEvents.RaiseBaseDamaged(amount);
            GameEvents.RaiseBaseHealthChanged(State.BaseHealth, baseMaxHealth);

            if (State.BaseHealth <= 0)
            {
                TriggerLose();
            }
        }

        public void RegisterPlantPlaced(int lane, int col)
        {
            State.PlacedPlants++;
            CurrentRunStats.PlantsPlaced++;
            GameEvents.RaisePlantPlaced(lane, col);
        }

        public void RegisterPlantRemoved(int lane, int col)
        {
            State.PlacedPlants = Mathf.Max(0, State.PlacedPlants - 1);
            GameEvents.RaisePlantRemoved(lane, col);
        }

        public void RegisterZombieSpawned(int lane)
        {
            State.AliveZombies++;
            GameEvents.RaiseZombieSpawned(lane);
        }

        public void RegisterZombieRemoved(int lane, bool killedByPlayer)
        {
            State.AliveZombies = Mathf.Max(0, State.AliveZombies - 1);
            if (killedByPlayer)
            {
                CurrentRunStats.ZombiesDefeated++;
                GameEvents.RaiseZombieKilled(lane);
            }
            TryResolveWinState();
        }

        public void AddCollectedSunStat(int amount)
        {
            CurrentRunStats.TotalSunCollected += Mathf.Max(0, amount);
        }

        public void AddEarnedCoinsStat(int amount)
        {
            CurrentRunStats.TotalCoinsEarned += Mathf.Max(0, amount);
        }

        public void TriggerWin()
        {
            if (resultTriggered)
            {
                return;
            }

            Time.timeScale = 1f;
            StopCurrentMatch();
            resultTriggered = true;
            CurrentRunStats.Won = true;
            SetPhase(GamePhase.Win);
            GameEvents.RaiseGameWon();
            GameEvents.RaiseRunEnded(CurrentRunStats.Clone(), true);
            SaveSystem.Instance?.SaveCurrentState();
        }

        public void TriggerLose()
        {
            if (resultTriggered)
            {
                return;
            }

            Time.timeScale = 1f;
            StopCurrentMatch();
            resultTriggered = true;
            CurrentRunStats.Won = false;
            SetPhase(GamePhase.Lose);
            GameEvents.RaiseGameLost();
            GameEvents.RaiseRunEnded(CurrentRunStats.Clone(), false);
            SaveSystem.Instance?.SaveCurrentState();
        }

        public void ForceResetScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void TryResolveWinState()
        {
            if (resultTriggered)
            {
                return;
            }

            if (!allWavesSpawned)
            {
                return;
            }

            if (State.AliveZombies <= 0 && State.BaseHealth > 0)
            {
                TriggerWin();
            }
        }

        private void StopCurrentMatch()
        {
            WaveManager.Instance?.StopAllWaveCoroutines();
        }

        private void CleanupRuntimeEntities()
        {
            PlantBase.DestroyAllPlants();
            ZombieBase.DestroyAllZombies();
            SunPickup.DestroyAll();
            CoinPickup.DestroyAll();
        }

        private void HandleSunChanged(int value)
        {
            State.Sun = value;
        }

        private void HandleCoinsChanged(int value)
        {
            State.Coins = value;
        }

#if UNITY_EDITOR
        [ContextMenu("Demo/Force Win")]
        private void DemoForceWin()
        {
            TriggerWin();
        }

        [ContextMenu("Demo/Force Lose")]
        private void DemoForceLose()
        {
            TriggerLose();
        }
#endif
    }
}
