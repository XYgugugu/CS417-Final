using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// Displays "Wave X / Y" and the running zombie kill count. Implements the
    /// "Win Condition / Scoreboard (2 pts)" deliverable.
    ///
    /// The wave progress bar smoothly lerps toward its target each frame, and
    /// an optional flag <see cref="flagTransform"/> slides along with the fill
    /// edge, mimicking the original PvZ "wave flag" indicator.
    /// </summary>
    public class ScoreboardUI : MonoBehaviour
    {
        [Header("Wave Labels")]
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private string waveFormat = "Wave {0} / {1}";

        [Header("Wave Progress Bar")]
        [Tooltip("Image with Type=Filled, Method=Horizontal. Its rect width determines the bar's pixel range.")]
        [SerializeField] private Image waveProgressFill;

        [Tooltip("Optional rect that rides along the right edge of the fill (e.g. a PvZ-style banner).")]
        [SerializeField] private RectTransform flagTransform;

        [Tooltip("How fast the fill lerps toward its target ratio. Higher = snappier.")]
        [SerializeField] private float fillLerpSpeed = 4f;

        [Header("Kills")]
        [SerializeField] private TMP_Text killsLabel;
        [SerializeField] private string killsFormat = "Zombies: {0}";

        [Header("High Score")]
        [SerializeField] private TMP_Text highestLabel;
        [SerializeField] private string highestFormat = "Best: Wave {0}";

        private float _targetFill;
        private float _displayedFill;
        private RectTransform _fillRect;

        private void Awake()
        {
            if (waveProgressFill != null) _fillRect = waveProgressFill.rectTransform;
        }

        private void OnEnable()
        {
            GameState.OnWaveProgressed += HandleWave;
            GameState.OnZombieDefeated += HandleKill;
            GameState.OnStateReset += HandleReset;

            // Snap to current state on enable.
            UpdateLabels();
            _targetFill = ComputeTargetFill();
            _displayedFill = _targetFill;
            ApplyFill(_displayedFill);
        }

        private void OnDisable()
        {
            GameState.OnWaveProgressed -= HandleWave;
            GameState.OnZombieDefeated -= HandleKill;
            GameState.OnStateReset -= HandleReset;
        }

        private void Update()
        {
            if (waveProgressFill == null) return;

            // Smooth lerp toward target.
            if (!Mathf.Approximately(_displayedFill, _targetFill))
            {
                _displayedFill = Mathf.MoveTowards(
                    _displayedFill,
                    _targetFill,
                    fillLerpSpeed * Time.deltaTime);
                ApplyFill(_displayedFill);
            }
        }

        // ============================================================
        //  Event handlers
        // ============================================================

        private void HandleWave(int current, int total)
        {
            UpdateLabels();
            _targetFill = ComputeTargetFill();
        }

        private void HandleKill(int total)
        {
            if (killsLabel != null) killsLabel.text = string.Format(killsFormat, total);
        }

        private void HandleReset()
        {
            UpdateLabels();
            _targetFill = ComputeTargetFill();
            _displayedFill = _targetFill;
            ApplyFill(_displayedFill);
        }

        // ============================================================
        //  Helpers
        // ============================================================

        private void UpdateLabels()
        {
            if (waveLabel != null) waveLabel.text = string.Format(waveFormat, GameState.CurrentWave, GameState.TotalWaves);
            if (killsLabel != null) killsLabel.text = string.Format(killsFormat, GameState.ZombiesDefeated);
            if (highestLabel != null) highestLabel.text = string.Format(highestFormat, GameState.HighestWaveReached);
        }

        private static float ComputeTargetFill()
        {
            if (GameState.TotalWaves <= 0) return 0f;
            return Mathf.Clamp01(GameState.CurrentWave / (float)GameState.TotalWaves);
        }

        private void ApplyFill(float ratio)
        {
            if (waveProgressFill != null)
            {
                waveProgressFill.fillAmount = ratio;
            }
            if (flagTransform != null && _fillRect != null)
            {
                // Position the flag along the bar's local X axis.
                // The fill rect's pivot is assumed to be (0, 0.5) — left-edge anchored.
                // If pivot differs, the flag still tracks the right edge of the visible fill.
                var width = _fillRect.rect.width;
                var pivotX = _fillRect.pivot.x;
                // Right-edge of the visible fill in fillRect's local space:
                //   x_right_local = -pivotX * width + width * ratio
                var localX = (-pivotX + ratio) * width;
                var pos = flagTransform.anchoredPosition;
                pos.x = localX;
                flagTransform.anchoredPosition = pos;
            }
        }
    }
}
