using System;
using UnityEngine;

namespace PVZ3D.Core
{
    [Serializable]
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
}
