using TMPro;
using PVZ3D.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PVZ3D.UI
{
    /// <summary>
    /// Drives the Win / Lose overlay. Shown by <see cref="HUDController"/> when
    /// the active <see cref="GameManager"/> ends the game.
    /// </summary>
    public class WinLoseUI : MonoBehaviour
    {
        [Tooltip("Optional explicit GameManager. If unset, the first GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

        [Header("Title / Subtitle")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;

        [Header("Win Copy")]
        [SerializeField] private string winTitle = "VICTORY!";
        [SerializeField] private Color winTitleColor = new(1f, 0.85f, 0.2f);

        [Header("Lose Copy")]
        [SerializeField] private string loseTitle = "THE ZOMBIES ATE YOUR BRAINS";
        [SerializeField] private Color loseTitleColor = new(0.85f, 0.2f, 0.2f);

        [Header("Subtitle Format")]
        [Tooltip("Args: 0=current wave, 1=total waves, 2=zombie kills, 3=score")]
        [SerializeField] private string subtitleFormat = "Score: {3}";

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private string mainMenuSceneName = "Entry";
        [SerializeField] private bool enableXRInteraction = true;
        [SerializeField] private bool enableMouseInteraction = true;

        [Header("XR Panel Placement")]
        [SerializeField] private bool stabilizeCanvasForXR = true;
        [SerializeField] private bool detachCanvasFromParent = true;
        [SerializeField] private float xrPanelDistance = 2f;
        [SerializeField] private Vector3 xrPanelOffset = new Vector3(0f, -0.15f, 0f);
        [SerializeField] private bool yawOnlyFacing = true;

        private XRSimpleInteractable restartXRInteractable;
        private XRSimpleInteractable mainMenuXRInteractable;
        private XRSimpleInteractable quitXRInteractable;
        private bool commandTriggered;

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            commandTriggered = false;
            BindButtons();
            StabilizeCanvasForXR();
            EnsureUIInteraction();

            // Refresh content the moment the overlay appears.
            Refresh();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void Refresh()
        {
            gameManager = ResolveGameManager();
            bool didWin = gameManager != null && gameManager.DidWin;
            int score = gameManager != null ? gameManager.Score : 0;
            int currentWave = gameManager != null ? gameManager.CurrentWave : 0;
            int totalWaves = gameManager != null ? gameManager.TotalWaves : 0;
            int kills = gameManager != null ? gameManager.ZombieKills : 0;

            if (titleLabel != null)
            {
                titleLabel.text = didWin ? winTitle : loseTitle;
                titleLabel.color = didWin ? winTitleColor : loseTitleColor;
            }
            if (subtitleLabel != null)
            {
                subtitleLabel.text = string.Format(subtitleFormat, currentWave, totalWaves, kills, score);
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

        private void EnsureUIInteraction()
        {
            if (!enableXRInteraction && !enableMouseInteraction)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Camera targetCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (targetCamera != null)
            {
                canvas.worldCamera = targetCamera;
            }

            if (enableXRInteraction)
            {
                TrackedDeviceGraphicRaycaster trackedRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                if (trackedRaycaster == null)
                {
                    trackedRaycaster = canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                }

                trackedRaycaster.enabled = true;
                trackedRaycaster.ignoreReversedGraphics = false;
                EnsureXRInputModule();
                EnableXRInteractorUIInteraction();
            }

            GraphicRaycaster[] mouseRaycasters = canvas.GetComponents<GraphicRaycaster>();
            if (enableMouseInteraction && mouseRaycasters.Length == 0)
            {
                mouseRaycasters = new[] { canvas.gameObject.AddComponent<GraphicRaycaster>() };
            }

            for (int i = 0; i < mouseRaycasters.Length; i++)
            {
                if (mouseRaycasters[i] != null)
                {
                    mouseRaycasters[i].enabled = enableMouseInteraction;
                    mouseRaycasters[i].ignoreReversedGraphics = false;
                }
            }
        }

        private void EnsureXRInputModule()
        {
            EventSystem eventSystem = EventSystem.current != null
                ? EventSystem.current
                : FindObjectOfType<EventSystem>();

            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }

            XRUIInputModule xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
            if (xrInputModule == null)
            {
                xrInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }

            xrInputModule.enabled = true;
            xrInputModule.enableXRInput = true;
            xrInputModule.enableMouseInput = enableMouseInteraction;

            BaseInputModule[] inputModules = eventSystem.GetComponents<BaseInputModule>();
            for (int i = 0; i < inputModules.Length; i++)
            {
                if (inputModules[i] != null && inputModules[i] != xrInputModule)
                {
                    inputModules[i].enabled = false;
                }
            }
        }

        private void EnableXRInteractorUIInteraction()
        {
            foreach (XRRayInteractor interactor in FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None))
            {
                interactor.enableUIInteraction = true;
            }

            foreach (XRPokeInteractor interactor in FindObjectsByType<XRPokeInteractor>(FindObjectsSortMode.None))
            {
                interactor.enableUIInteraction = true;
            }

            foreach (NearFarInteractor interactor in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                interactor.enableUIInteraction = true;
            }
        }

        private XRSimpleInteractable EnsureXRButtonInteractable(Button button)
        {
            if (!enableXRInteraction || button == null)
            {
                return null;
            }

            BoxCollider boxCollider = button.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = button.gameObject.AddComponent<BoxCollider>();
            }

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Rect rect = rectTransform.rect;
                float width = Mathf.Max(1f, rect.width);
                float height = Mathf.Max(1f, rect.height);

                boxCollider.center = new Vector3(rect.center.x, rect.center.y, 0f);
                boxCollider.size = new Vector3(width, height, Mathf.Max(20f, Mathf.Min(width, height) * 0.1f));
            }

            boxCollider.isTrigger = false;

            XRSimpleInteractable interactable = button.GetComponent<XRSimpleInteractable>();
            if (interactable == null)
            {
                interactable = button.gameObject.AddComponent<XRSimpleInteractable>();
            }

            if (!interactable.colliders.Contains(boxCollider))
            {
                interactable.colliders.Add(boxCollider);
            }

            return interactable;
        }

        private void StabilizeCanvasForXR()
        {
            if (!stabilizeCanvasForXR)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.renderMode = RenderMode.WorldSpace;

            CameraRelativeHUD cameraRelativeHud = canvas.GetComponent<CameraRelativeHUD>();
            if (cameraRelativeHud != null)
            {
                cameraRelativeHud.enabled = false;
            }

            if (detachCanvasFromParent && canvas.transform.parent != null)
            {
                canvas.transform.SetParent(null, true);
            }

            Camera targetCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (targetCamera == null)
            {
                return;
            }

            Transform cameraTransform = targetCamera.transform;
            Vector3 forward = cameraTransform.forward;
            Vector3 up = cameraTransform.up;

            if (yawOnlyFacing)
            {
                Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
                if (flatForward.sqrMagnitude < 1e-4f)
                {
                    flatForward = new Vector3(canvas.transform.forward.x, 0f, canvas.transform.forward.z);
                    if (flatForward.sqrMagnitude < 1e-4f)
                    {
                        flatForward = Vector3.forward;
                    }
                }

                forward = flatForward.normalized;
                up = Vector3.up;
            }

            Vector3 right = Vector3.Cross(up, forward).normalized;
            canvas.transform.position =
                cameraTransform.position
                + forward * (xrPanelDistance + xrPanelOffset.z)
                + right * xrPanelOffset.x
                + up * xrPanelOffset.y;
            canvas.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        // ============================================================
        //  Buttons
        // ============================================================

        private void BindButtons()
        {
            UnbindButtons();

            if (restartButton != null) restartButton.onClick.AddListener(HandleRestart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(HandleMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);

            restartXRInteractable = EnsureXRButtonInteractable(restartButton);
            mainMenuXRInteractable = EnsureXRButtonInteractable(mainMenuButton);
            quitXRInteractable = EnsureXRButtonInteractable(quitButton);

            if (restartXRInteractable != null) restartXRInteractable.selectEntered.AddListener(HandleRestartSelected);
            if (mainMenuXRInteractable != null) mainMenuXRInteractable.selectEntered.AddListener(HandleMainMenuSelected);
            if (quitXRInteractable != null) quitXRInteractable.selectEntered.AddListener(HandleQuitSelected);
        }

        private void UnbindButtons()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(HandleRestart);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(HandleMainMenu);
            if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);

            if (restartXRInteractable != null) restartXRInteractable.selectEntered.RemoveListener(HandleRestartSelected);
            if (mainMenuXRInteractable != null) mainMenuXRInteractable.selectEntered.RemoveListener(HandleMainMenuSelected);
            if (quitXRInteractable != null) quitXRInteractable.selectEntered.RemoveListener(HandleQuitSelected);
        }

        private void HandleRestartSelected(SelectEnterEventArgs args)
        {
            if (restartButton != null && restartButton.interactable)
            {
                HandleRestart();
            }
        }

        private void HandleMainMenuSelected(SelectEnterEventArgs args)
        {
            if (mainMenuButton != null && mainMenuButton.interactable)
            {
                HandleMainMenu();
            }
        }

        private void HandleQuitSelected(SelectEnterEventArgs args)
        {
            if (quitButton != null && quitButton.interactable)
            {
                HandleQuit();
            }
        }

        public void HandleRestart()
        {
            if (!TryStartCommand()) return;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void HandleMainMenu()
        {
            if (!TryStartCommand()) return;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public void HandleQuit()
        {
            if (!TryStartCommand()) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool TryStartCommand()
        {
            if (commandTriggered)
            {
                return false;
            }

            commandTriggered = true;
            return true;
        }
    }
}
