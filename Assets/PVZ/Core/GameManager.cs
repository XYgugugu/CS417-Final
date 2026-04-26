using UnityEngine;
using PVZ3D.Resources;

namespace PVZ3D.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Gameplay State")]
        [SerializeField] private GamePhase startingPhase = GamePhase.Prep;
        [SerializeField] private int startingSun = 200;
        [SerializeField] private int startingCoins = 0;
        [SerializeField] private int baseMaxHealth = 10;
        [SerializeField] private bool cheatModeEnabled;

        [Header("Runtime")]
        [SerializeField] private GameState state = new GameState();
        [SerializeField] private int collectedSunThisRun;
        [SerializeField] private int earnedCoinsThisRun;

        public GameState State => state;
        public int BaseMaxHealth => baseMaxHealth;
        public bool CheatModeEnabled => cheatModeEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeState();
        }

        private void Start()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.Initialize(state.Sun, state.Coins);
            }
        }

        private void InitializeState()
        {
            state.Phase = startingPhase;
            state.CurrentWave = 0;
            state.TotalWaves = 0;
            state.BaseHealth = Mathf.Max(1, baseMaxHealth);
            state.Sun = Mathf.Max(0, startingSun);
            state.Coins = Mathf.Max(0, startingCoins);
            state.PlacedPlants = 0;

            GameEvents.RaiseGamePhaseChanged(state.Phase);
            GameEvents.RaiseBaseHealthChanged(state.BaseHealth, baseMaxHealth);
            GameEvents.RaiseSunChanged(state.Sun);
            GameEvents.RaiseCoinsChanged(state.Coins);
        }

        public void SetPhase(GamePhase phase)
        {
            state.Phase = phase;
            GameEvents.RaiseGamePhaseChanged(state.Phase);
        }

        public void SetCheatMode(bool enabled)
        {
            cheatModeEnabled = enabled;
            GameEvents.RaiseCheatModeChanged(enabled);
        }

        public void RegisterPlantPlaced(int lane, int col)
        {
            state.PlacedPlants++;
            GameEvents.RaisePlantPlaced(lane, col);
        }

        public void RegisterPlantRemoved(int lane, int col)
        {
            state.PlacedPlants = Mathf.Max(0, state.PlacedPlants - 1);
            GameEvents.RaisePlantRemoved(lane, col);
        }

        public void AddCollectedSunStat(int amount)
        {
            collectedSunThisRun += Mathf.Max(0, amount);
            state.Sun += Mathf.Max(0, amount);
        }

        public void AddEarnedCoinsStat(int amount)
        {
            earnedCoinsThisRun += Mathf.Max(0, amount);
            state.Coins += Mathf.Max(0, amount);
        }

        public void DamageBase(int amount)
        {
            state.BaseHealth = Mathf.Max(0, state.BaseHealth - Mathf.Max(0, amount));
            GameEvents.RaiseBaseHealthChanged(state.BaseHealth, baseMaxHealth);

            if (state.BaseHealth <= 0)
            {
                SetPhase(GamePhase.Defeat);
            }
        }
    }
}
