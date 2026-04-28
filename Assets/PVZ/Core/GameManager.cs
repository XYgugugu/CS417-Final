using System;
using UnityEngine;

namespace PVZ3D.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerManager playerManager = new PlayerManager();
        public PlayerManager PlayerManager => playerManager;

        [Header("Loss Timer")]
        [SerializeField] private LossTimer lossTimer = new LossTimer();
        public LossTimer LossTimer => lossTimer;

        [Header("Game State")]
        [SerializeField] private bool gameOver;
        public bool GameOver => gameOver;

        private void Awake()
        {
            playerManager.Initialize(OnLoseConditionMet);
            lossTimer.Initialize(OnLoseConditionMet);

            lossTimer.StartTimer(10000f);
        }

        private void Update()
        {
            if (gameOver) return;

            lossTimer.Update(Time.deltaTime);
        }

        private void OnLoseConditionMet()
        {
            if (gameOver) return;

            gameOver = true;
            Debug.Log("Game Over.");
        }
    }

    [System.Serializable]
    public class LossTimer
    {
        [SerializeField] private float timeRemain;

        private bool isRunning;
        private bool isPaused;
        private Action onTimerFinished;

        public float TimeRemain => timeRemain;
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;

        public void Initialize(Action onTimerFinished)
        {
            this.onTimerFinished = onTimerFinished;
        }

        public void StartTimer(float duration)
        {
            StopTimer();

            timeRemain = Mathf.Max(0f, duration);
            isRunning = true;
            isPaused = false;
        }

        public void PauseTimer()
        {
            if (!isRunning) return;
            isPaused = true;
        }

        public void StopTimer()
        {
            timeRemain = 0f;
            isRunning = false;
            isPaused = false;
        }

        public void Update(float deltaTime)
        {
            if (!isRunning || isPaused) return;

            timeRemain -= deltaTime;

            if (timeRemain > 0f) return;

            timeRemain = 0f;
            isRunning = false;
            isPaused = false;

            onTimerFinished?.Invoke();
        }
    }

    [System.Serializable]
    public class PlayerManager
    {
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int hp = 100;

        private Action onPlayerDead;

        public int MaxHP => maxHP;
        public int HP => hp;

        private void Awake()
        {
            hp = maxHP;
        }

        public void Initialize(Action onPlayerDead)
        {
            this.onPlayerDead = onPlayerDead;
        }

        public void SetHealth(int value)
        {
            hp = Mathf.Clamp(value, 0, maxHP);

            if (hp <= 0)
            {
                onPlayerDead?.Invoke();
            }
        }

        public void GainHealth(int value)
        {
            if (value <= 0) return;

            hp = Mathf.Min(maxHP, hp + value);
        }

        public void LoseHealth(int value)
        {
            if (value <= 0) return;

            hp = Mathf.Max(0, hp - value);
            Debug.Log($"HP: {hp}/{maxHP} - Lost {value}.");

            if (hp <= 0)
            {
                onPlayerDead?.Invoke();
            }
        }
    }
}