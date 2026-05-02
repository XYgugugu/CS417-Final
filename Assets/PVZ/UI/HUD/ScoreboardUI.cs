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

        [Header("Score")]
        [FormerlySerializedAs("waveLabel")]
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private string scoreFormat = "Score: {0}";

        [Header("Optional Progress Fill")]
        [FormerlySerializedAs("waveProgressFill")]
        [SerializeField] private Image progressFill;

        private int lastScore = int.MinValue;

        private void OnEnable()
        {
            Refresh(true);
        }

        private void Update()
        {
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
            if (!force && score == lastScore)
            {
                return;
            }

            lastScore = score;
            if (scoreLabel != null)
            {
                scoreLabel.text = string.Format(scoreFormat, score);
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = 0f;
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
    }
}
