using PVZ3D.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PVZ3D.UI
{
    public class EndGamePanelController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text resultText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        private bool runtimeBindingsApplied;

        public void BuildUI()
        {
            RectTransform root = EnsureRectTransform();
            root.sizeDelta = new Vector2(820f, 620f);

            GameObject panel = UIFactory.CreatePanel("EndGamePanel", transform, root.sizeDelta, new Color(0.06f, 0.08f, 0.1f, 0.94f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchoredPosition = Vector2.zero;

            UIFactory.CreateText("FinalLabel", panel.transform, "Run Complete", 24, TextAnchor.MiddleCenter, new Vector2(760f, 44f), new Vector2(0f, 262f));
            resultText = UIFactory.CreateText("ResultText", panel.transform, "Victory", 54, TextAnchor.MiddleCenter, new Vector2(760f, 90f), new Vector2(0f, 215f));
            summaryText = UIFactory.CreateText("SummaryText", panel.transform, string.Empty, 24, TextAnchor.UpperLeft, new Vector2(740f, 320f), new Vector2(0f, 20f));

            restartButton = UIFactory.CreateButton("RestartButton", panel.transform, "Restart Match", new Vector2(300f, 78f), new Vector2(-170f, -220f));
            menuButton = UIFactory.CreateButton("MenuButton", panel.transform, "Return To Menu", new Vector2(300f, 78f), new Vector2(170f, -220f), new Color(0.17f, 0.31f, 0.45f, 0.95f));

            EnsureRuntimeBindings();

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            SetVisible(false);
        }

        private void Update()
        {
            EnsureRuntimeBindings();
        }

        private void HandleRestartClicked()
        {
            GameManager.Instance?.RestartMatch();
        }

        private void HandleMenuClicked()
        {
            GameManager.Instance?.ReturnToMenu();
        }

        private void EnsureRuntimeBindings()
        {
            if (runtimeBindingsApplied)
            {
                return;
            }

            if (restartButton == null || menuButton == null)
            {
                Button[] buttons = GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null)
                    {
                        continue;
                    }

                    if (restartButton == null && button.name == "RestartButton")
                    {
                        restartButton = button;
                    }
                    else if (menuButton == null && button.name == "MenuButton")
                    {
                        menuButton = button;
                    }
                }
            }

            if (restartButton == null || menuButton == null)
            {
                return;
            }

            restartButton.onClick.RemoveListener(HandleRestartClicked);
            restartButton.onClick.AddListener(HandleRestartClicked);

            menuButton.onClick.RemoveListener(HandleMenuClicked);
            menuButton.onClick.AddListener(HandleMenuClicked);

            runtimeBindingsApplied = true;
        }

        public void ShowResult(bool won, RunStats stats)
        {
            if (stats == null)
            {
                stats = new RunStats();
            }

            resultText.text = won ? "Victory" : "Defeat";
            resultText.color = won ? new Color(0.62f, 1f, 0.62f) : new Color(1f, 0.5f, 0.5f);

            summaryText.text =
                $"Waves Cleared   : {stats.WavesCleared}\n" +
                $"Zombies Defeated: {stats.ZombiesDefeated}\n" +
                $"Plants Placed   : {stats.PlantsPlaced}\n" +
                $"Sun Collected   : {stats.TotalSunCollected}\n" +
                $"Coins Earned    : {stats.TotalCoinsEarned}\n\n" +
                $"Final Result    : {(won ? "Victory" : "Defeat")}";

            SetVisible(true);
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
