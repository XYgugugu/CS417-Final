using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// One card in the HUD's plant tray. Shows the icon, sun cost, and a
    /// black cooldown overlay (alpha fades linearly from <see cref="cooldownStartAlpha"/>
    /// down to 0 over the plant's cooldown). Greys out if:
    ///   - the player can't afford it (sun &lt; cost), OR
    ///   - the cooldown is still active, OR
    ///   - the plant is locked.
    ///
    /// Listens to <see cref="GameState.OnPlantCooldownTick"/> and friends; never
    /// references gameplay code directly.
    ///
    /// <para>=== HOW TO ADD A NEW PLANT ===</para>
    /// <list type="number">
    ///   <item>Drop a square portrait PNG into <c>Assets/PVZ/UI/Sprites/PlantCards/</c>
    ///         and confirm Unity imported it as <b>Sprite (2D and UI)</b> + Single mode.</item>
    ///   <item>In Project window: right-click → <c>Create → PVZ → Plant Card Data</c>.
    ///         Name the asset <c>PlantCard_&lt;Name&gt;</c> (e.g. <c>PlantCard_SnowPea</c>).</item>
    ///   <item>Fill its fields: <c>plantId</c> (stable string used in save files; never change),
    ///         <c>displayName</c>, <c>icon</c> (drag the sprite), <c>sunCost</c>,
    ///         <c>cooldownSeconds</c>, <c>unlockedByDefault</c>.</item>
    ///   <item>If the plant should be available from the start, also drag the
    ///         <c>PlantCardData</c> asset into <c>GameSettings_Default →
    ///         Default Unlocked Plants</c>.</item>
    ///   <item>Add a slot in the HUD: select an existing <c>Card_*</c> GameObject under
    ///         <c>HUDCanvas/HUDRoot/PlantTray</c>, press <kbd>Cmd/Ctrl-D</kbd> to
    ///         duplicate, rename to <c>Card_&lt;Name&gt;</c>, then on the new card's
    ///         <c>PlantCardSlotUI</c> component drag the new <c>PlantCardData</c>
    ///         asset into the <c>card</c> field.</item>
    ///   <item>Right-click the <c>PlantCardSlotUI</c> header → <b>Sync Icon From Card Data</b>
    ///         to bake the sprite into the scene so editor previews match Play mode.</item>
    /// </list>
    ///
    /// <para>=== HOW TO REPLACE AN EXISTING PLANT ===</para>
    /// <list type="bullet">
    ///   <item><b>Cosmetic only</b> (same plant, new art / cost / cooldown): edit the
    ///         linked <c>PlantCardData</c> asset's fields. The card auto-updates next
    ///         Play. After editing, right-click <c>PlantCardSlotUI</c> →
    ///         <b>Sync Icon From Card Data</b> to refresh the editor preview too.</item>
    ///   <item><b>Different plant</b>: drag a different <c>PlantCardData</c> asset onto
    ///         this component's <c>card</c> field, then run the
    ///         <b>Sync Icon From Card Data</b> context menu.</item>
    /// </list>
    /// </summary>
    public class PlantCardSlotUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlantCardData card;

        [Header("Visuals")]
        [Tooltip("The plant portrait.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Black-tinted Image stretched over the whole card. Its alpha fades linearly from cooldownStartAlpha (just-planted) down to 0 (ready) as the cooldown ticks. Image Type can be Simple — fillAmount is no longer used.")]
        [SerializeField] private Image cooldownOverlay;

        [Tooltip("Alpha of the black overlay at the moment a plant is used. Decays linearly to 0 over the cooldown.")]
        [Range(0f, 1f)]
        [SerializeField] private float cooldownStartAlpha = 0.7f;

        [Tooltip("Sun cost label.")]
        [SerializeField] private TMP_Text costLabel;

        [Tooltip("Optional remaining-seconds label shown during cooldown.")]
        [SerializeField] private TMP_Text cooldownLabel;

        [Tooltip("Optional Button (clicking simulates planting). Most projects will trigger planting via the world grid; this is for menu testing.")]
        [SerializeField] private Button button;

        [Header("Tint")]
        [SerializeField] private Color readyTint = Color.white;
        [SerializeField] private Color disabledTint = new(0.5f, 0.5f, 0.5f, 1f);

        [Header("Ready Flash FX (cooldown completed)")]
        [Tooltip("White-ish overlay image that flashes when the cooldown finishes. Should be a child Image stretched over the card.")]
        [SerializeField] private Image flashOverlay;

        [Tooltip("How long the flash + scale-bump lasts.")]
        [SerializeField] private float readyFlashDuration = 0.45f;

        [Tooltip("Peak scale during the flash.")]
        [SerializeField] private float readyFlashScale = 1.18f;

        [Tooltip("Tint at flash peak.")]
        [SerializeField] private Color readyFlashColor = new(1f, 0.95f, 0.55f, 0.85f);

        public PlantCardData Card => card;
        public bool IsReady { get; private set; }

        private RectTransform _rt;
        private Vector3 _baseScale = Vector3.one;
        private float _previousRemain;
        private float _flashTimer;

        private void Awake()
        {
            _rt = transform as RectTransform;
            if (_rt != null) _baseScale = _rt.localScale;
            if (button != null) button.onClick.AddListener(HandleClicked);
        }

        private void OnEnable()
        {
            ApplyCardVisuals();

            GameState.OnPlantCooldownTick += HandleCooldownTick;
            GameState.OnSunChanged += HandleSunChanged;
            GameState.OnPlantUnlocked += HandleUnlocked;
            GameState.OnStateReset += HandleStateReset;

            RefreshAffordability();
        }

        private void OnDisable()
        {
            GameState.OnPlantCooldownTick -= HandleCooldownTick;
            GameState.OnSunChanged -= HandleSunChanged;
            GameState.OnPlantUnlocked -= HandleUnlocked;
            GameState.OnStateReset -= HandleStateReset;
        }

        public void SetCard(PlantCardData newCard)
        {
            card = newCard;
            ApplyCardVisuals();
            RefreshAffordability();
        }

        private void ApplyCardVisuals()
        {
            if (card == null) return;
            if (iconImage != null && card.icon != null) iconImage.sprite = card.icon;
            if (costLabel != null) costLabel.text = card.sunCost.ToString();
            if (cooldownOverlay != null)
            {
                var c = cooldownOverlay.color; c.a = 0f; cooldownOverlay.color = c;
                cooldownOverlay.gameObject.SetActive(false);
            }
            if (cooldownLabel != null) cooldownLabel.gameObject.SetActive(false);
            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(false);
                var c = readyFlashColor; c.a = 0; flashOverlay.color = c;
            }
        }

        private void HandleCooldownTick(string plantId, float remain, float total)
        {
            if (card == null || plantId != card.plantId) return;

            // Detect cooldown JUST finished — fire the "ready" celebration.
            if (_previousRemain > 0f && remain <= 0f)
            {
                TriggerReadyFlash();
            }
            _previousRemain = remain;

            UpdateCooldownVisual(remain, total);
            RefreshAffordability();
        }

        private void TriggerReadyFlash()
        {
            _flashTimer = readyFlashDuration;
            if (flashOverlay != null) flashOverlay.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_flashTimer <= 0f)
            {
                if (_rt != null && _rt.localScale != _baseScale)
                {
                    _rt.localScale = _baseScale;
                }
                if (flashOverlay != null && flashOverlay.gameObject.activeSelf)
                {
                    flashOverlay.gameObject.SetActive(false);
                }
                return;
            }

            _flashTimer -= Time.deltaTime;
            var t = Mathf.Clamp01(1f - _flashTimer / Mathf.Max(0.0001f, readyFlashDuration));

            // Bell curve so the bump peaks mid-animation.
            var bell = 1f - Mathf.Abs(t * 2f - 1f);

            if (_rt != null)
            {
                var s = Mathf.Lerp(1f, readyFlashScale, bell);
                _rt.localScale = _baseScale * s;
            }

            if (flashOverlay != null)
            {
                var c = readyFlashColor;
                c.a = readyFlashColor.a * bell;
                flashOverlay.color = c;
            }
        }

        private void UpdateCooldownVisual(float remain, float total)
        {
            var active = remain > 0f && total > 0f;
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(active);
                if (active)
                {
                    // Linear alpha fade: full at the start of the cooldown, 0 when ready.
                    var t = Mathf.Clamp01(remain / total);
                    var c = cooldownOverlay.color;
                    c.a = cooldownStartAlpha * t;
                    cooldownOverlay.color = c;
                }
            }
            if (cooldownLabel != null)
            {
                cooldownLabel.gameObject.SetActive(active);
                if (active) cooldownLabel.text = Mathf.CeilToInt(remain).ToString();
            }
        }

        private void HandleSunChanged(int _) => RefreshAffordability();
        private void HandleUnlocked(string id) { if (card != null && id == card.plantId) RefreshAffordability(); }
        private void HandleStateReset() { ApplyCardVisuals(); RefreshAffordability(); }

        private void RefreshAffordability()
        {
            if (card == null) return;

            var unlocked = GameState.IsPlantUnlocked(card.plantId);
            var cooldownReady = GameState.IsPlantReady(card.plantId);
            var canAfford = GameState.CheatModeEnabled || GameState.Sun >= card.sunCost;

            IsReady = unlocked && cooldownReady && canAfford;

            var tint = IsReady ? readyTint : disabledTint;
            if (iconImage != null) iconImage.color = tint;
            if (costLabel != null) costLabel.color = canAfford ? Color.white : new Color(1f, 0.4f, 0.4f);
            if (button != null) button.interactable = IsReady;
        }

        /// <summary>
        /// Called when the user clicks/pokes this card. Aaron's UI doesn't own
        /// "planting" — it just deducts cost and starts the cooldown, then fires
        /// OnPlantUsed for the gameplay layer to actually spawn the plant.
        /// </summary>
        public void HandleClicked()
        {
            if (!IsReady || card == null) return;
            if (!GameState.TrySpendSun(card.sunCost)) return;
            GameState.StartPlantCooldown(card.plantId, card.cooldownSeconds);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only helper: copies <c>card.icon</c> + <c>card.sunCost</c> into the
        /// referenced <c>iconImage</c> sprite and <c>costLabel</c> text so the card
        /// looks correct in the Scene/Game view <i>without</i> entering Play mode.
        /// At runtime <see cref="ApplyCardVisuals"/> already does this on every
        /// <see cref="OnEnable"/>; this method just bakes the same values into the
        /// scene file so editor previews aren't blank.
        ///
        /// Usage: in the Inspector, click the ⋮ (three-dot) menu next to the
        /// <c>Plant Card Slot UI</c> header → <b>Sync Icon From Card Data</b>.
        /// </summary>
        [ContextMenu("Sync Icon From Card Data")]
        private void SyncIconFromCardEditor()
        {
            if (card == null)
            {
                Debug.LogWarning($"[PlantCardSlotUI] '{name}' has no PlantCardData assigned — nothing to sync.", this);
                return;
            }
            if (iconImage != null && card.icon != null)
            {
                iconImage.sprite = card.icon;
                iconImage.color = readyTint;
                UnityEditor.EditorUtility.SetDirty(iconImage);
            }
            if (costLabel != null)
            {
                costLabel.text = card.sunCost.ToString();
                UnityEditor.EditorUtility.SetDirty(costLabel);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[PlantCardSlotUI] Synced '{name}' from '{card.name}'.", this);
        }
#endif
    }
}
