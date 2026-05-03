using TMPro;
using PVZ3D.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public class ScoreboardUI : MonoBehaviour
    {
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

        [Header("Wave")]
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private string waveFormat = "Wave {0} / {1} | Score: {2}";

        [Header("Kills")]
        [SerializeField] private TMP_Text killsLabel;
        [SerializeField] private string killsFormat = "Zombies: {0}";

        [Header("Score")]
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private string scoreFormat = "Score: {0}";

        [Header("Optional Progress Fill")]
        [FormerlySerializedAs("waveProgressFill")]
        [SerializeField] private Image progressFill;

        private int lastScore = int.MinValue;
        private int lastKills = int.MinValue;
        private int lastCurrentWave = int.MinValue;
        private int lastTotalWaves = int.MinValue;
        private GameManager subscribedGameManager;

        private void OnEnable()
        {
            Subscribe(ResolveGameManager());
            Refresh(true);
        }

        private void OnDisable()
        {
            Subscribe(null);
        }

        private void Update()
        {
            Subscribe(ResolveGameManager());
            Refresh(false);
        }

        private void Refresh(bool force)
        {
            GameManager gm = ResolveGameManager();
            if (gm == null)
            {
                return;
            }

            int score = gm.Score;
            int kills = gm.ZombieKills;
            int currentWave = gm.CurrentWave;
            int totalWaves = gm.TotalWaves;
            if (!force
                && score == lastScore
                && kills == lastKills
                && currentWave == lastCurrentWave
                && totalWaves == lastTotalWaves)
            {
                return;
            }

            lastScore = score;
            lastKills = kills;
            lastCurrentWave = currentWave;
            lastTotalWaves = totalWaves;

            if (waveLabel != null)
            {
                waveLabel.text = string.Format(waveFormat, currentWave, totalWaves, score, kills);
            }

            if (killsLabel != null)
            {
                killsLabel.text = string.Format(killsFormat, kills, score, currentWave, totalWaves);
            }

            if (scoreLabel != null && scoreLabel != waveLabel && scoreLabel != killsLabel)
            {
                scoreLabel.text = string.Format(scoreFormat, score, kills, currentWave, totalWaves);
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = totalWaves > 0 ? (float)currentWave / totalWaves : 0f;
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

        private void Subscribe(GameManager manager)
        {
            if (subscribedGameManager == manager)
            {
                return;
            }

            if (subscribedGameManager != null)
            {
                subscribedGameManager.OnStatsChanged -= HandleStatsChanged;
            }

            subscribedGameManager = manager;

            if (subscribedGameManager != null)
            {
                subscribedGameManager.OnStatsChanged += HandleStatsChanged;
            }
        }

        private void HandleStatsChanged()
        {
            Refresh(true);
        }
    }
}
