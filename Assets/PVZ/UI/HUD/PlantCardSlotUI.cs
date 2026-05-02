using TMPro;
using PVZ3D.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public class PlantCardSlotUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlantCardData card;

        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

        [Header("Visuals")]
        [Tooltip("The plant portrait.")]
        [SerializeField] private Image iconImage;

        [Tooltip("Black-tinted Image stretched over the whole card. Its alpha fades linearly from cooldownStartAlpha down to 0 over the cooldown.")]
        [SerializeField] private Image cooldownOverlay;

        [Tooltip("Alpha of the black overlay at the moment a plant is used. Decays linearly to 0 over the cooldown.")]
        [Range(0f, 1f)]
        [SerializeField] private float cooldownStartAlpha = 0.7f;

        [Tooltip("Sun cost label.")]
        [SerializeField] private TMP_Text costLabel;

        [Tooltip("Optional remaining-seconds label shown during cooldown.")]
        [SerializeField] private TMP_Text cooldownLabel;

        [Tooltip("Optional Button. Clicking attempts to exchange sun for this plant and starts its cooldown.")]
        [SerializeField] private Button button;

        [Header("Tint")]
        [SerializeField] private Color readyTint = Color.white;
        [SerializeField] private Color disabledTint = new(0.5f, 0.5f, 0.5f, 1f);

        [Header("Ready Flash FX (cooldown completed)")]
        [SerializeField] private Image flashOverlay;
        [SerializeField] private float readyFlashDuration = 0.45f;
        [SerializeField] private float readyFlashScale = 1.18f;
        [SerializeField] private Color readyFlashColor = new(1f, 0.95f, 0.55f, 0.85f);

        public PlantCardData Card => card;
        public bool IsReady { get; private set; }

        private RectTransform rt;
        private Vector3 baseScale = Vector3.one;
        private float previousRemain;
        private float flashTimer;
        private PlantType plantType;
        private bool hasPlantType;
        private int lastSun = int.MinValue;
        private float lastCooldownRemain = float.NaN;
        private float lastCooldownTotal = float.NaN;

        private void Awake()
        {
            rt = transform as RectTransform;
            if (rt != null)
            {
                baseScale = rt.localScale;
            }

            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnEnable()
        {
            ApplyCardVisuals();
            RefreshEconomyState(true);
        }

        public void SetCard(PlantCardData newCard)
        {
            card = newCard;
            ApplyCardVisuals();
            RefreshEconomyState(true);
        }

        private void ApplyCardVisuals()
        {
            if (card == null)
            {
                return;
            }

            hasPlantType = TryResolvePlantType(card, out plantType);

            if (iconImage != null && card.icon != null)
            {
                iconImage.sprite = card.icon;
            }

            PlantsEconomy plantsEconomy = ResolvePlantsEconomy();
            int cost = hasPlantType && plantsEconomy != null
                ? plantsEconomy.GetPlantCost(plantType)
                : card.sunCost;
            if (costLabel != null)
            {
                costLabel.text = cost.ToString();
            }

            UpdateCooldownVisual(0f, 0f);

            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(false);
                Color color = readyFlashColor;
                color.a = 0f;
                flashOverlay.color = color;
            }
        }

        private void Update()
        {
            RefreshEconomyState(false);
            UpdateReadyFlash();
        }

        private void RefreshEconomyState(bool force)
        {
            if (card == null)
            {
                return;
            }

            if (!hasPlantType && !TryResolvePlantType(card, out plantType))
            {
                SetUnavailable();
                return;
            }

            hasPlantType = true;

            PlantsEconomy plantsEconomy = ResolvePlantsEconomy();
            if (plantsEconomy == null)
            {
                SetUnavailable();
                return;
            }

            int sun = plantsEconomy.Sun;
            float remain = plantsEconomy.GetPlantCooldownRemaining(plantType);
            float total = plantsEconomy.GetPlantCooldownDuration(plantType);

            if (!force &&
                sun == lastSun &&
                Mathf.Approximately(remain, lastCooldownRemain) &&
                Mathf.Approximately(total, lastCooldownTotal))
            {
                return;
            }

            lastSun = sun;
            lastCooldownRemain = remain;
            lastCooldownTotal = total;

            if (previousRemain > 0f && remain <= 0f)
            {
                TriggerReadyFlash();
            }

            previousRemain = remain;
            UpdateCooldownVisual(remain, total);
            RefreshAffordability(plantsEconomy);
        }

        private PlantsEconomy ResolvePlantsEconomy()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager != null ? gameManager.PlantsEconomy : null;
        }

        private void RefreshAffordability(PlantsEconomy plantsEconomy)
        {
            int cost = plantsEconomy.GetPlantCost(plantType);
            bool canAfford = plantsEconomy.Sun >= cost;
            bool cooldownReady = plantsEconomy.IsPlantReady(plantType);

            IsReady = canAfford && cooldownReady;

            if (iconImage != null)
            {
                iconImage.color = IsReady ? readyTint : disabledTint;
            }

            if (costLabel != null)
            {
                costLabel.text = cost.ToString();
                costLabel.color = canAfford ? Color.white : new Color(1f, 0.4f, 0.4f);
            }

            if (button != null)
            {
                button.interactable = IsReady;
            }
        }

        private void SetUnavailable()
        {
            IsReady = false;
            if (iconImage != null)
            {
                iconImage.color = disabledTint;
            }

            if (button != null)
            {
                button.interactable = false;
            }
        }

        private void UpdateCooldownVisual(float remain, float total)
        {
            bool active = remain > 0f && total > 0f;
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(active);
                Color color = cooldownOverlay.color;
                color.a = active ? cooldownStartAlpha * Mathf.Clamp01(remain / total) : 0f;
                cooldownOverlay.color = color;
            }

            if (cooldownLabel != null)
            {
                cooldownLabel.gameObject.SetActive(active);
                if (active)
                {
                    cooldownLabel.text = Mathf.CeilToInt(remain).ToString();
                }
            }
        }

        private void TriggerReadyFlash()
        {
            flashTimer = readyFlashDuration;
            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(true);
            }
        }

        private void UpdateReadyFlash()
        {
            if (flashTimer <= 0f)
            {
                if (rt != null && rt.localScale != baseScale)
                {
                    rt.localScale = baseScale;
                }

                if (flashOverlay != null && flashOverlay.gameObject.activeSelf)
                {
                    flashOverlay.gameObject.SetActive(false);
                }

                return;
            }

            flashTimer -= Time.deltaTime;
            float progress = Mathf.Clamp01(1f - flashTimer / Mathf.Max(0.0001f, readyFlashDuration));
            float bell = 1f - Mathf.Abs(progress * 2f - 1f);

            if (rt != null)
            {
                rt.localScale = baseScale * Mathf.Lerp(1f, readyFlashScale, bell);
            }

            if (flashOverlay != null)
            {
                Color color = readyFlashColor;
                color.a = readyFlashColor.a * bell;
                flashOverlay.color = color;
            }
        }

        public void HandleClicked()
        {
            if (!IsReady || card == null || !hasPlantType)
            {
                return;
            }

            PlantsEconomy plantsEconomy = ResolvePlantsEconomy();
            if (plantsEconomy == null)
            {
                return;
            }

            if (plantsEconomy.ExchangePlant(plantType))
            {
                RefreshEconomyState(true);
            }
        }

        private static bool TryResolvePlantType(PlantCardData card, out PlantType resolvedType)
        {
            string id = NormalizePlantId(card != null ? card.plantId : null);
            switch (id)
            {
                case "sunflower":
                    resolvedType = PlantType.SunFlower;
                    return true;
                case "peashooter":
                    resolvedType = PlantType.PeaShooter;
                    return true;
                case "wallnut":
                    resolvedType = PlantType.WallNut;
                    return true;
                default:
                    resolvedType = default;
                    return false;
            }
        }

        private static string NormalizePlantId(string plantId)
        {
            if (string.IsNullOrWhiteSpace(plantId))
            {
                return string.Empty;
            }

            return plantId
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }

#if UNITY_EDITOR
        [ContextMenu("Sync Icon From Card Data")]
        private void SyncIconFromCardEditor()
        {
            if (card == null)
            {
                Debug.LogWarning($"[PlantCardSlotUI] '{name}' has no PlantCardData assigned.", this);
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
                PlantsEconomy plantsEconomy = ResolvePlantsEconomy();
                costLabel.text = TryResolvePlantType(card, out PlantType resolvedType) && plantsEconomy != null
                    ? plantsEconomy.GetPlantCost(resolvedType).ToString()
                    : card.sunCost.ToString();
                UnityEditor.EditorUtility.SetDirty(costLabel);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[PlantCardSlotUI] Synced '{name}' from '{card.name}'.", this);
        }
#endif
    }
}
