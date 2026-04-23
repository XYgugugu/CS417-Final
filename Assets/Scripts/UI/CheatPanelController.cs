using PVZ3D.Core;
using PVZ3D.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public class CheatPanelController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text cheatStateText;
        [SerializeField] private Button addSunButton;
        [SerializeField] private Button addCoinsButton;

        [Header("Cheat Values")]
        [SerializeField] private int cheatSunAmount = 150;
        [SerializeField] private int cheatCoinAmount = 100;
        private bool runtimeBindingsApplied;
        private bool cheatEventBound;

        public void BuildUI()
        {
            RectTransform root = EnsureRectTransform();
            root.sizeDelta = new Vector2(450f, 280f);

            GameObject panel = UIFactory.CreatePanel("CheatPanel", transform, root.sizeDelta, new Color(0.12f, 0.08f, 0.08f, 0.88f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = Vector2.zero;

            cheatStateText = UIFactory.CreateText("CheatStateText", panel.transform, "Cheat: OFF", 24, TextAnchor.MiddleCenter, new Vector2(380f, 50f), new Vector2(0f, 88f));
            addSunButton = UIFactory.CreateButton("AddSun", panel.transform, $"+{cheatSunAmount} Sun", new Vector2(170f, 64f), new Vector2(-95f, -10f), new Color(0.43f, 0.35f, 0.12f, 0.95f));
            addCoinsButton = UIFactory.CreateButton("AddCoins", panel.transform, $"+{cheatCoinAmount} Coins", new Vector2(170f, 64f), new Vector2(95f, -10f), new Color(0.38f, 0.26f, 0.08f, 0.95f));

            EnsureRuntimeBindings();

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            BindEvents();
            SetCheatState(false);
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
        }

        private void HandleAddSunClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.CheatModeEnabled)
            {
                ResourceManager.Instance?.AddSun(cheatSunAmount, true);
            }
        }

        private void HandleAddCoinsClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.CheatModeEnabled)
            {
                ResourceManager.Instance?.AddCoins(cheatCoinAmount, true);
            }
        }

        private void EnsureRuntimeBindings()
        {
            if (runtimeBindingsApplied)
            {
                return;
            }

            if (addSunButton == null || addCoinsButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (addSunButton == null && button.name == "AddSun")
                    {
                        addSunButton = button;
                    }
                    else if (addCoinsButton == null && button.name == "AddCoins")
                    {
                        addCoinsButton = button;
                    }
                }
            }

            if (addSunButton == null || addCoinsButton == null)
            {
                return;
            }

            addSunButton.onClick.RemoveListener(HandleAddSunClicked);
            addSunButton.onClick.AddListener(HandleAddSunClicked);

            addCoinsButton.onClick.RemoveListener(HandleAddCoinsClicked);
            addCoinsButton.onClick.AddListener(HandleAddCoinsClicked);

            runtimeBindingsApplied = true;
        }

        private void BindEvents()
        {
            if (cheatEventBound)
            {
                return;
            }

            GameEvents.OnCheatModeChanged += SetCheatState;
            cheatEventBound = true;
        }

        private void UnbindEvents()
        {
            if (!cheatEventBound)
            {
                return;
            }

            GameEvents.OnCheatModeChanged -= SetCheatState;
            cheatEventBound = false;
        }

        public void SetCheatState(bool enabled)
        {
            if (cheatStateText != null)
            {
                cheatStateText.text = enabled ? "Cheat: ON" : "Cheat: OFF";
                cheatStateText.color = enabled ? new Color(1f, 0.7f, 0.7f) : Color.white;
            }

            if (addSunButton != null)
            {
                addSunButton.interactable = enabled;
            }

            if (addCoinsButton != null)
            {
                addCoinsButton.interactable = enabled;
            }

            SetVisible(enabled);
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
    }
}
