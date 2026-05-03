using System;
using UnityEngine;

namespace PVZ3D.Core
{
    public class GameManager : MonoBehaviour
    {
        public event Action<bool> OnGameEnded;
        public event Action OnStatsChanged;

        [Header("Player")]
        [SerializeField] private PlayerManager playerManager = new PlayerManager();
        public PlayerManager PlayerManager => playerManager;

        [Header("Loss Timer")]
        [SerializeField] public float GameTime = 600f;
        [SerializeField] private LossTimer lossTimer = new LossTimer();
        public LossTimer LossTimer => lossTimer;

        [Header("Plant Economy")]
        [SerializeField] private PlantsEconomy plantsEconomy = new PlantsEconomy();
        public PlantsEconomy PlantsEconomy => plantsEconomy;

        [Header("Resource Manager")]
        [SerializeField] private ResourceManager resourceManager = new ResourceManager();
        public ResourceManager ResourceManager => resourceManager;

        [Header("Game State")]
        [SerializeField] private bool gameOver;
        [SerializeField] private bool didWin;
        public bool GameOver => gameOver;
        public bool DidWin => didWin;

        [SerializeField] private int score;
        public int Score => score;

        [SerializeField] private int zombieKills;
        public int ZombieKills => zombieKills;

        [SerializeField] private int currentWave;
        public int CurrentWave => currentWave;

        [SerializeField] private int totalWaves;
        public int TotalWaves => totalWaves;

        private void Awake()
        {
            playerManager.Initialize(OnLoseConditionMet);
            lossTimer.Initialize(OnLoseConditionMet);
            plantsEconomy.Reset();
            resourceManager.Reset();

            lossTimer.StartTimer(GameTime);
        }

        private void Update()
        {
            if (gameOver) return;

            lossTimer.Update(Time.deltaTime);
            plantsEconomy.Update(Time.deltaTime);
        }

        private void OnLoseConditionMet()
        {
            EndGame(false);
        }

        public void WinGame()
        {
            EndGame(true);
        }

        private void EndGame(bool win)
        {
            if (gameOver) return;

            gameOver = true;
            didWin = win;
            OnGameEnded?.Invoke(didWin);
            Debug.Log(didWin ? "Victory." : "Game Over.");
        }

        public void EarnScore(int value)
        {
            if (value <= 0) return;
            score += value;
            OnStatsChanged?.Invoke();
        }

        public void RegisterZombieKilled(int scoreValue)
        {
            zombieKills++;
            if (scoreValue > 0)
            {
                score += scoreValue;
            }

            OnStatsChanged?.Invoke();
        }

        public void SetWaveProgress(int wave, int total)
        {
            int clampedTotal = Mathf.Max(0, total);
            int clampedWave = Mathf.Clamp(wave, 0, clampedTotal);

            if (currentWave == clampedWave && totalWaves == clampedTotal)
            {
                return;
            }

            currentWave = clampedWave;
            totalWaves = clampedTotal;
            OnStatsChanged?.Invoke();
        }
    }

}
