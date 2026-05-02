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

        [Header("Plant Economy")]
        [SerializeField] private PlantsEconomy plantsEconomy = new PlantsEconomy();
        public PlantsEconomy PlantsEconomy => plantsEconomy;

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

}
