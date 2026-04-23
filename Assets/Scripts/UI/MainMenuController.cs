using PVZ3D.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Toggle cheatToggle;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text hintText;
        private bool runtimeBindingsApplied;

        public void BuildUI()
        {
            RectTransform root = EnsureRectTransform();
            root.sizeDelta = new Vector2(760f, 560f);

            GameObject panel = UIFactory.CreatePanel("MainMenuPanel", transform, root.sizeDelta, new Color(0.08f, 0.12f, 0.08f, 0.9f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = Vector2.zero;

            titleText = UIFactory.CreateText("Title", panel.transform, "PVZ 3D", 52, TextAnchor.MiddleCenter, new Vector2(680f, 110f), new Vector2(0f, 190f));
            titleText.color = new Color(0.92f, 1f, 0.72f);
            subtitleText = UIFactory.CreateText("Subtitle", panel.transform, "VR Vertical Slice Demo", 26, TextAnchor.MiddleCenter, new Vector2(680f, 58f), new Vector2(0f, 134f));
            subtitleText.color = new Color(0.8f, 0.9f, 1f);

            startButton = UIFactory.CreateButton("StartButton", panel.transform, "Start Match", new Vector2(340f, 76f), new Vector2(0f, 50f));
            quitButton = UIFactory.CreateButton("QuitButton", panel.transform, "Quit", new Vector2(340f, 76f), new Vector2(0f, -45f), new Color(0.48f, 0.2f, 0.2f, 0.95f));
            cheatToggle = UIFactory.CreateToggle("CheatToggle", panel.transform, "Cheat Mode", new Vector2(340f, 56f), new Vector2(0f, -142f));
            hintText = UIFactory.CreateText("HintText", panel.transform, "1) Start Match 开局  2) 扣 Trigger 抓桌面 Seed 拖到格子种植  3) 手柄射线挥过 Sun 自动收集  4) Y 键可调出菜单并恢复本局", 20, TextAnchor.MiddleCenter, new Vector2(700f, 84f), new Vector2(0f, -228f));
            hintText.color = new Color(0.84f, 0.9f, 0.83f);

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            EnsureRuntimeBindings();
        }

        private void HandlePrimaryActionClicked()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            if (GameManager.Instance.State.Phase == GamePhase.Paused)
            {
                GameManager.Instance.ResumePausedMatch();
                UIManager.Instance?.ShowGameplayHud();
                return;
            }

            GameManager.Instance.StartMatch();
        }

        private void HandleQuitClicked()
        {
            GameManager.Instance?.QuitGame();
        }

        private void HandleCheatToggleChanged(bool value)
        {
            GameManager.Instance?.SetCheatMode(value);
        }

        public void SetCheatToggle(bool enabled)
        {
            if (cheatToggle != null)
            {
                cheatToggle.SetIsOnWithoutNotify(enabled);
            }
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
            RefreshPrimaryActionLabel();
        }

        public void SetGuideText(string message)
        {
            if (hintText != null && !string.IsNullOrWhiteSpace(message))
            {
                hintText.text = message;
            }
        }

        private void Update()
        {
            EnsureRuntimeBindings();
            RefreshPrimaryActionLabel();
        }

        private void EnsureRuntimeBindings()
        {
            if (runtimeBindingsApplied)
            {
                return;
            }

            if (startButton == null || quitButton == null || cheatToggle == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (startButton == null && button.name == "StartButton")
                    {
                        startButton = button;
                    }
                    else if (quitButton == null && button.name == "QuitButton")
                    {
                        quitButton = button;
                    }
                }

                if (cheatToggle == null)
                {
                    cheatToggle = GetComponentInChildren<Toggle>(true);
                }
            }

            if (startButton == null || quitButton == null || cheatToggle == null)
            {
                return;
            }

            startButton.onClick.RemoveListener(HandlePrimaryActionClicked);
            startButton.onClick.AddListener(HandlePrimaryActionClicked);

            quitButton.onClick.RemoveListener(HandleQuitClicked);
            quitButton.onClick.AddListener(HandleQuitClicked);

            cheatToggle.onValueChanged.RemoveListener(HandleCheatToggleChanged);
            cheatToggle.onValueChanged.AddListener(HandleCheatToggleChanged);

            runtimeBindingsApplied = true;
        }

        private void RefreshPrimaryActionLabel()
        {
            if (startButton == null)
            {
                return;
            }

            Text label = startButton.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            bool paused = GameManager.Instance != null && GameManager.Instance.State.Phase == GamePhase.Paused;
            label.text = paused ? "Resume Match" : "Start Match";
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
