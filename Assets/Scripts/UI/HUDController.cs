using System.Collections;
using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Plants;
using PVZ3D.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text sunText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text waveText;
        [SerializeField] private Text baseHealthText;
        [SerializeField] private Text selectedPlantText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private Text guideText;
        [SerializeField] private Button menuButton;

        [SerializeField] private readonly List<Button> plantButtons = new List<Button>();
        [SerializeField] private readonly List<Button> laneButtons = new List<Button>();
        [SerializeField] private readonly List<Text> plantButtonTexts = new List<Text>();
        [SerializeField] private Color plantButtonNormalColor = new Color(0.18f, 0.35f, 0.19f, 0.95f);
        [SerializeField] private Color plantButtonSelectedColor = new Color(0.26f, 0.58f, 0.29f, 0.98f);
        [SerializeField] private Color unaffordableTextColor = new Color(0.75f, 0.75f, 0.75f, 0.92f);
        [SerializeField] private Color affordableTextColor = Color.white;

        private Coroutine feedbackPulseRoutine;
        private bool runtimeBindingsApplied;
        private bool eventsBound;

        public void BuildUI(int laneCount)
        {
            RectTransform root = EnsureRectTransform();
            root.sizeDelta = new Vector2(1100f, 620f);

            GameObject panel = UIFactory.CreatePanel("HUDPanel", transform, root.sizeDelta, new Color(0.08f, 0.1f, 0.13f, 0.78f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = Vector2.zero;

            UIFactory.CreateText("TopLabel", panel.transform, "Resources", 20, TextAnchor.MiddleLeft, new Vector2(260f, 34f), new Vector2(-430f, 286f));
            sunText = UIFactory.CreateText("SunText", panel.transform, "Sun: 0", 24, TextAnchor.MiddleLeft, new Vector2(250f, 40f), new Vector2(-410f, 255f));
            coinsText = UIFactory.CreateText("CoinsText", panel.transform, "Coins: 0", 24, TextAnchor.MiddleLeft, new Vector2(250f, 40f), new Vector2(-140f, 255f));
            UIFactory.CreateText("StateLabel", panel.transform, "Match", 20, TextAnchor.MiddleLeft, new Vector2(220f, 34f), new Vector2(160f, 286f));
            waveText = UIFactory.CreateText("WaveText", panel.transform, "Wave: 0/0", 24, TextAnchor.MiddleLeft, new Vector2(250f, 40f), new Vector2(130f, 255f));
            baseHealthText = UIFactory.CreateText("BaseText", panel.transform, "Base: 0", 24, TextAnchor.MiddleLeft, new Vector2(250f, 40f), new Vector2(380f, 255f));
            baseHealthText.color = new Color(1f, 0.92f, 0.74f);

            selectedPlantText = UIFactory.CreateText("SelectedPlantText", panel.transform, "Selected Plant: None", 22, TextAnchor.MiddleLeft, new Vector2(620f, 40f), new Vector2(-220f, 195f));
            feedbackText = UIFactory.CreateText("FeedbackText", panel.transform, "", 20, TextAnchor.MiddleLeft, new Vector2(980f, 38f), new Vector2(0f, 160f));
            feedbackText.color = new Color(0.93f, 0.9f, 0.64f);
            guideText = UIFactory.CreateText("GuideText", panel.transform, "Guide: 扣 Trigger 抓桌面 Seed，拖到格子松开种植。手柄射线扫到 Sun 即收集。右摇杆可摇头看向。", 20, TextAnchor.MiddleLeft, new Vector2(1020f, 36f), new Vector2(0f, -94f));
            guideText.color = new Color(0.79f, 0.9f, 1f);

            float plantStartX = -310f;
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Button button = UIFactory.CreateButton($"Plant_{i}", panel.transform, $"Plant {i + 1}", new Vector2(280f, 78f), new Vector2(plantStartX + (i * 315f), 84f), new Color(0.18f, 0.35f, 0.19f, 0.95f));
                button.onClick.AddListener(() =>
                {
                    PlantPlacementManager.Instance?.SelectPlantByIndex(idx);
                    RefreshSelectedPlant();
                    RefreshAffordability();
                });

                plantButtons.Add(button);
                Text btnText = button.GetComponentInChildren<Text>();
                plantButtonTexts.Add(btnText);
            }

            UIFactory.CreateText("LaneLabel", panel.transform, "Quick Place Lane:", 22, TextAnchor.MiddleLeft, new Vector2(280f, 40f), new Vector2(-404f, -8f));
            float laneStartX = -220f;
            for (int lane = 0; lane < laneCount; lane++)
            {
                int laneCapture = lane;
                Button laneButton = UIFactory.CreateButton($"Lane_{lane + 1}", panel.transform, (lane + 1).ToString(), new Vector2(100f, 66f), new Vector2(laneStartX + (lane * 114f), -12f), new Color(0.17f, 0.27f, 0.42f, 0.95f));
                laneButton.onClick.AddListener(() =>
                {
                    bool success = PlantPlacementManager.Instance != null && PlantPlacementManager.Instance.TryPlaceSelectedInLane(laneCapture);
                    if (!success)
                    {
                        SetFeedback("Placement failed", false);
                    }
                });
                laneButtons.Add(laneButton);
            }

            menuButton = UIFactory.CreateButton("ReturnMenu", panel.transform, "Menu", new Vector2(150f, 58f), new Vector2(446f, -260f), new Color(0.2f, 0.3f, 0.48f, 0.95f));

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            EnsureRuntimeBindings();
            BindEvents();
        }

        private void OnEnable()
        {
            EnsureRuntimeBindings();
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void Update()
        {
            EnsureRuntimeBindings();
            RefreshSelectedPlant();
            RefreshAffordability();
        }

        public void BindPlantDefinitions(IReadOnlyList<PlantDefinition> definitions)
        {
            for (int i = 0; i < plantButtons.Count; i++)
            {
                bool valid = definitions != null && i < definitions.Count;
                plantButtons[i].gameObject.SetActive(valid);
                if (!valid)
                {
                    continue;
                }

                PlantDefinition def = definitions[i];
                if (plantButtonTexts[i] != null)
                {
                    plantButtonTexts[i].text = BuildPlantButtonLabel(def);
                }
            }

            RefreshSelectedPlant();
            RefreshAffordability();
        }

        public void RefreshAllFromState(GameState state)
        {
            if (state == null)
            {
                return;
            }

            HandleSunChanged(state.Sun);
            HandleCoinsChanged(state.Coins);
            HandleWaveChanged(state.CurrentWave, state.TotalWaves);
            HandleBaseChanged(state.BaseHealth, GameManager.Instance != null ? GameManager.Instance.BaseMaxHealth : state.BaseHealth);
            RefreshSelectedPlant();
            RefreshAffordability();
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.GetComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            gameObject.SetActive(visible);
        }

        public void RefreshSelectedPlant()
        {
            PlantDefinition selected = PlantPlacementManager.Instance != null ? PlantPlacementManager.Instance.SelectedPlant : null;
            selectedPlantText.text = selected == null
                ? "Selected Plant: None"
                : BuildSelectedPlantLabel(selected);

            int selectedIndex = PlantPlacementManager.Instance != null ? PlantPlacementManager.Instance.SelectedPlantIndex : -1;
            for (int i = 0; i < plantButtons.Count; i++)
            {
                if (plantButtons[i] == null)
                {
                    continue;
                }

                Image image = plantButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = i == selectedIndex ? plantButtonSelectedColor : plantButtonNormalColor;
                }
            }
        }

        public void RefreshAffordability()
        {
            IReadOnlyList<PlantDefinition> defs = PlantPlacementManager.Instance != null ? PlantPlacementManager.Instance.PlantDefinitions : null;
            int sun = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentSun : 0;

            for (int i = 0; i < plantButtons.Count; i++)
            {
                if (defs == null || i >= defs.Count)
                {
                    continue;
                }

                bool canAfford = GameManager.Instance != null && GameManager.Instance.CheatModeEnabled
                    ? true
                    : sun >= defs[i].SunCost;
                bool onCooldown = PlantPlacementManager.Instance != null && PlantPlacementManager.Instance.IsOnCooldown(defs[i]);
                plantButtons[i].interactable = canAfford && !onCooldown;
                if (plantButtonTexts[i] != null)
                {
                    plantButtonTexts[i].text = BuildPlantButtonLabel(defs[i]);
                    plantButtonTexts[i].color = canAfford && !onCooldown ? affordableTextColor : unaffordableTextColor;
                }
            }
        }

        private string BuildSelectedPlantLabel(PlantDefinition definition)
        {
            if (definition == null)
            {
                return "Selected Plant: None";
            }

            float remaining = PlantPlacementManager.Instance != null ? PlantPlacementManager.Instance.GetRemainingCooldown(definition) : 0f;
            int upgradeCost = PlantPlacementManager.Instance != null ? PlantPlacementManager.Instance.GetUpgradeCostPreview(definition) : 0;
            string upgradeSuffix = upgradeCost > 0 ? $", Upg {upgradeCost} sun" : string.Empty;
            return remaining > 0f
                ? $"Selected Plant: {definition.DisplayName} ({definition.SunCost} sun{upgradeSuffix}, CD {remaining:F1}s)"
                : $"Selected Plant: {definition.DisplayName} ({definition.SunCost} sun{upgradeSuffix})";
        }

        private string BuildPlantButtonLabel(PlantDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            float remaining = PlantPlacementManager.Instance != null ? PlantPlacementManager.Instance.GetRemainingCooldown(definition) : 0f;
            int upgradeCost = PlantPlacementManager.Instance != null ? PlantPlacementManager.Instance.GetUpgradeCostPreview(definition) : 0;
            string upgradeSuffix = upgradeCost > 0 ? $" / U{upgradeCost}" : string.Empty;
            return remaining > 0f
                ? $"{definition.DisplayName} ({definition.SunCost}{upgradeSuffix}) [{remaining:F1}s]"
                : $"{definition.DisplayName} ({definition.SunCost}{upgradeSuffix})";
        }

        public void SetFeedback(string message, bool success)
        {
            if (feedbackText == null)
            {
                return;
            }

            feedbackText.text = message;
            feedbackText.color = success ? new Color(0.6f, 1f, 0.65f) : new Color(1f, 0.55f, 0.55f);
            if (feedbackPulseRoutine != null)
            {
                StopCoroutine(feedbackPulseRoutine);
            }
            feedbackPulseRoutine = StartCoroutine(PulseFeedbackText());
        }

        public void SetGuideText(string message)
        {
            if (guideText != null && !string.IsNullOrWhiteSpace(message))
            {
                guideText.text = $"Guide: {message}";
            }
        }

        private void HandleSunChanged(int value)
        {
            sunText.text = $"Sun: {value}";
            RefreshAffordability();
        }

        private void HandleCoinsChanged(int value)
        {
            coinsText.text = $"Coins: {value}";
        }

        private void HandleWaveChanged(int current, int total)
        {
            waveText.text = $"Wave: {current}/{total}";
        }

        private void HandleBaseChanged(int current, int max)
        {
            baseHealthText.text = $"Base: {current}/{max}";
            float ratio = max > 0 ? current / (float)max : 0f;
            baseHealthText.color = Color.Lerp(new Color(1f, 0.4f, 0.38f), new Color(0.95f, 1f, 0.72f), ratio);
        }

        private void HandlePurchaseResult(bool success, string msg)
        {
            SetFeedback(msg, success);
        }

        private RectTransform EnsureRectTransform()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private void EnsureRuntimeBindings()
        {
            if (runtimeBindingsApplied)
            {
                return;
            }

            CacheButtonsFromSceneIfNeeded();

            if (plantButtons.Count == 0 || laneButtons.Count == 0 || menuButton == null)
            {
                return;
            }

            for (int i = 0; i < plantButtons.Count; i++)
            {
                Button button = plantButtons[i];
                int idx = i;
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    PlantPlacementManager.Instance?.SelectPlantByIndex(idx);
                    RefreshSelectedPlant();
                    RefreshAffordability();
                });
            }

            for (int lane = 0; lane < laneButtons.Count; lane++)
            {
                Button laneButton = laneButtons[lane];
                int laneCapture = lane;
                if (laneButton == null)
                {
                    continue;
                }

                laneButton.onClick.RemoveAllListeners();
                laneButton.onClick.AddListener(() =>
                {
                    bool success = PlantPlacementManager.Instance != null && PlantPlacementManager.Instance.TryPlaceSelectedInLane(laneCapture);
                    if (!success)
                    {
                        SetFeedback("Placement failed", false);
                    }
                });
            }

            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(HandleMenuButtonClicked);

            runtimeBindingsApplied = true;
        }

        private void CacheButtonsFromSceneIfNeeded()
        {
            if (plantButtons.Count > 0 && laneButtons.Count > 0 && menuButton != null)
            {
                return;
            }

            plantButtons.Clear();
            laneButtons.Clear();
            plantButtonTexts.Clear();
            menuButton = null;

            Button[] allButtons = GetComponentsInChildren<Button>(true);
            SortedDictionary<int, Button> plants = new SortedDictionary<int, Button>();
            SortedDictionary<int, Button> lanes = new SortedDictionary<int, Button>();

            for (int i = 0; i < allButtons.Length; i++)
            {
                Button button = allButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (button.name.StartsWith("Plant_") && int.TryParse(button.name.Substring("Plant_".Length), out int plantIndex))
                {
                    plants[plantIndex] = button;
                    continue;
                }

                if (button.name.StartsWith("Lane_") && int.TryParse(button.name.Substring("Lane_".Length), out int laneNumber))
                {
                    lanes[Mathf.Max(0, laneNumber - 1)] = button;
                    continue;
                }

                if (button.name == "ReturnMenu")
                {
                    menuButton = button;
                }
            }

            foreach (var kvp in plants)
            {
                plantButtons.Add(kvp.Value);
                plantButtonTexts.Add(kvp.Value != null ? kvp.Value.GetComponentInChildren<Text>() : null);
            }

            foreach (var kvp in lanes)
            {
                laneButtons.Add(kvp.Value);
            }
        }

        private void HandleMenuButtonClicked()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null)
            {
                return;
            }

            if (manager.State.Phase == GamePhase.Prep || manager.State.Phase == GamePhase.Battle)
            {
                manager.PauseMatch();
                UIManager.Instance?.ShowMainMenuInFront();
                return;
            }

            if (manager.State.Phase == GamePhase.Paused)
            {
                manager.ResumePausedMatch();
                UIManager.Instance?.ShowGameplayHud();
            }
        }

        private void BindEvents()
        {
            if (eventsBound)
            {
                return;
            }

            GameEvents.OnSunChanged += HandleSunChanged;
            GameEvents.OnCoinsChanged += HandleCoinsChanged;
            GameEvents.OnWaveChanged += HandleWaveChanged;
            GameEvents.OnBaseHealthChanged += HandleBaseChanged;
            GameEvents.OnPurchaseResult += HandlePurchaseResult;
            eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!eventsBound)
            {
                return;
            }

            GameEvents.OnSunChanged -= HandleSunChanged;
            GameEvents.OnCoinsChanged -= HandleCoinsChanged;
            GameEvents.OnWaveChanged -= HandleWaveChanged;
            GameEvents.OnBaseHealthChanged -= HandleBaseChanged;
            GameEvents.OnPurchaseResult -= HandlePurchaseResult;
            eventsBound = false;
        }

        private IEnumerator PulseFeedbackText()
        {
            RectTransform rect = feedbackText != null ? feedbackText.rectTransform : null;
            if (rect == null)
            {
                yield break;
            }

            Vector3 baseScale = Vector3.one;
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.unscaledDeltaTime;
                float p = t / 0.12f;
                rect.localScale = Vector3.Lerp(baseScale * 1.08f, baseScale, p);
                yield return null;
            }

            rect.localScale = baseScale;
        }
    }
}
