using PVZ3D.Plants;
using PVZ3D.Zombies;
using UnityEngine;
using System.Collections;

namespace PVZ3D.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerManager playerManager = new PlayerManager();
        public PlayerManager LossTimer => playerManager;

        [Header("Loss Timer")]
        [SerializeField] private LossTimer lossTimer = new LossTimer();
        public LossTimer LossTimer => lossTimer;

        private void Awake()
        {
            lossTimer.StartTimer(10f);
        }

        private void Update()
        {
            lossTimer.Update(Time.deltaTime);
        }
    }

    [System.Serializable]
    public class LossTimer
    {
        [SerializeField] private float timeRemain;

        private bool isRunning;
        private bool isPaused;

        public float TimeRemain => timeRemain;
        public bool IsRunning => isRunning;
        public bool IsPaused => isPaused;

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

            Debug.Log("Loss timer finished. Player loses.");
        }
    }

    [System.Serializable]
    public class PlayerManager
    {
        [SerializeField] private int HP = 100;

        public void SetHealth(int value)
        {
            if (value <= 0) return;
            HP = value;
        }
        public void GainHealth(int value)
        {
            if (value <= 0) return;
            HP += value;
        }
        public void LoseHealth(int value)
        {
            if (value <= 0) return;
            HP -= value;
        }
    }
}