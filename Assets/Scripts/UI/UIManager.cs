using System;
using PVZ3D.Core;
using PVZ3D.Grid;
using PVZ3D.Plants;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PVZ3D.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("World UI Placement")]
        [Tooltip("Auto place panel when it becomes active. Panels stay fixed after placement.")]
        [SerializeField] private bool autoPlacePanelsInFrontOfCamera = true;
        [Tooltip("For standalone headset builds, force initial auto placement on startup.")]
        [SerializeField] private bool forceAutoPlacementOnDevice = true;
        [Tooltip("If enabled, panels continuously follow head. Keep OFF for stable comfort.")]
        [SerializeField] private bool continuouslyTrackPanels = false;
        [Tooltip("Fallback menu position when auto placement is disabled.")]
        [SerializeField] private Vector3 menuPosition = new Vector3(-3.6f, 1.7f, -1.2f);
        [SerializeField] private Vector3 menuEuler = new Vector3(0f, 90f, 0f);
        [Tooltip("Fallback HUD position when auto placement is disabled.")]
        [SerializeField] private Vector3 hudPosition = new Vector3(-0.7f, 1.35f, -4.1f);
        [SerializeField] private Vector3 hudEuler = new Vector3(17f, 0f, 0f);
        [Tooltip("Fallback endgame panel position when auto placement is disabled.")]
        [SerializeField] private Vector3 endPanelPosition = new Vector3(-1.8f, 1.6f, 0f);
        [SerializeField] private Vector3 endPanelEuler = new Vector3(0f, 90f, 0f);
        [Tooltip("Fallback cheat panel position when auto placement is disabled.")]
        [SerializeField] private Vector3 cheatPanelPosition = new Vector3(-2.8f, 1.2f, -1.9f);
        [SerializeField] private Vector3 cheatPanelEuler = new Vector3(0f, 90f, 0f);

        [Header("Menu Placement Safety")]
        [SerializeField] private float menuForwardDistance = 1.95f;
        [SerializeField] private float menuVerticalOffset = 0.75f;
        [SerializeField] private float menuSideOffset;
        [SerializeField] private float menuCollisionRadius = 0.14f;
        [SerializeField] private float menuLiftPerAttempt = 0.35f;
        [SerializeField] private int menuAvoidanceAttempts = 10;
        [SerializeField] private LayerMask menuOccluderMask = ~0;
        [SerializeField] private float menuPanelDepth = 0.04f;
        [SerializeField] private float menuPanelOverlapPadding = 0.03f;

        private MainMenuController mainMenu;
        private HUDController hud;
        private EndGamePanelController endGame;
        private CheatPanelController cheatPanel;
        private Text idlePopupText;
        private float nextAutoPlaceTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            NormalizePlacementConfig();
            EnsureEventSystem();
            if (!TryBindExistingUiHierarchy())
            {
                BuildUIHierarchy();
            }
        }

        private void Start()
        {
            if (!Application.isEditor && forceAutoPlacementOnDevice)
            {
                autoPlacePanelsInFrontOfCamera = true;
            }

            GameEvents.OnGamePhaseChanged += HandlePhaseChanged;
            GameEvents.OnRunEnded += HandleRunEnded;
            GameEvents.OnIdleProgressApplied += HandleIdleProgress;
            GameEvents.OnCheatModeChanged += HandleCheatChanged;

            PlantPlacementManager ppm = PlantPlacementManager.Instance;
            if (ppm != null)
            {
                hud.BindPlantDefinitions(ppm.PlantDefinitions);
            }

            if (GameManager.Instance != null)
            {
                hud.RefreshAllFromState(GameManager.Instance.State);
                mainMenu.SetCheatToggle(GameManager.Instance.CheatModeEnabled);
                HandlePhaseChanged(GameManager.Instance.State.Phase);
                if (GameManager.Instance.LastIdleProgressResult.HasRewards)
                {
                    HandleIdleProgress(GameManager.Instance.LastIdleProgressResult);
                }
            }

            RefreshActivePanelPlacement();
            if (GameManager.Instance == null || GameManager.Instance.State.Phase == GamePhase.Menu)
            {
                ShowMainMenuInFront();
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnGamePhaseChanged -= HandlePhaseChanged;
            GameEvents.OnRunEnded -= HandleRunEnded;
            GameEvents.OnIdleProgressApplied -= HandleIdleProgress;
            GameEvents.OnCheatModeChanged -= HandleCheatChanged;
        }

        private void Update()
        {
            SyncCanvasWorldCamera();
            EnsureMenuVisibilityFailSafe();

            if (!autoPlacePanelsInFrontOfCamera)
            {
                return;
            }

            if (!continuouslyTrackPanels)
            {
                return;
            }

            if (Time.unscaledTime < nextAutoPlaceTime)
            {
                return;
            }

            nextAutoPlaceTime = Time.unscaledTime + 0.5f;
            RefreshActivePanelPlacement();
        }

        private void BuildUIHierarchy()
        {
            GameObject root = new GameObject("UICanvases");
            root.transform.SetParent(transform, false);

            mainMenu = BuildPanel<MainMenuController>("MainMenuCanvas", root.transform, menuPosition, menuEuler, new Vector2(800f, 600f));
            mainMenu.BuildUI();

            int laneCount = LawnGridManager.Instance != null ? LawnGridManager.Instance.Lanes : 5;
            hud = BuildPanel<HUDController>("HUDCanvas", root.transform, hudPosition, hudEuler, new Vector2(1100f, 620f));
            hud.BuildUI(laneCount);

            endGame = BuildPanel<EndGamePanelController>("EndGameCanvas", root.transform, endPanelPosition, endPanelEuler, new Vector2(820f, 620f));
            endGame.BuildUI();

            cheatPanel = BuildPanel<CheatPanelController>("CheatCanvas", root.transform, cheatPanelPosition, cheatPanelEuler, new Vector2(450f, 280f));
            cheatPanel.BuildUI();

            GameObject idleCanvasObj = CreateWorldCanvasObject("IdlePopupCanvas", root.transform, new Vector3(-0.8f, 2.2f, -1.6f), new Vector3(13f, 23f, 0f), new Vector2(700f, 180f));
            idlePopupText = UIFactory.CreateText("IdlePopupText", idleCanvasObj.transform, string.Empty, 28, TextAnchor.MiddleCenter, new Vector2(680f, 160f), Vector2.zero);
            idlePopupText.color = new Color(1f, 0.92f, 0.6f);
            idleCanvasObj.SetActive(false);
        }

        private bool TryBindExistingUiHierarchy()
        {
            Transform root = transform.Find("UICanvases");
            if (root == null)
            {
                return false;
            }

            mainMenu = root.GetComponentInChildren<MainMenuController>(true);
            hud = root.GetComponentInChildren<HUDController>(true);
            endGame = root.GetComponentInChildren<EndGamePanelController>(true);
            cheatPanel = root.GetComponentInChildren<CheatPanelController>(true);

            Transform idleTextTransform = root.Find("IdlePopupCanvas/IdlePopupText");
            idlePopupText = idleTextTransform != null ? idleTextTransform.GetComponent<Text>() : null;

            return mainMenu != null && hud != null && endGame != null && cheatPanel != null;
        }

#if UNITY_EDITOR
        [ContextMenu("Authoring/Bake UI Into Scene")]
        public void BuildEditorUiPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            Transform existing = transform.Find("UICanvases");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            EnsureEventSystem();
            BuildUIHierarchy();

            if (mainMenu != null)
            {
                mainMenu.SetVisible(true);
            }

            if (hud != null)
            {
                hud.SetVisible(false);
            }

            if (endGame != null)
            {
                endGame.SetVisible(false);
            }

            if (cheatPanel != null)
            {
                cheatPanel.SetVisible(false);
            }

            EditorUtility.SetDirty(gameObject);
        }
#endif

        private T BuildPanel<T>(string canvasName, Transform parent, Vector3 worldPos, Vector3 worldEuler, Vector2 size) where T : Component
        {
            GameObject canvasObj = CreateWorldCanvasObject(canvasName, parent, worldPos, worldEuler, size);
            T controller = canvasObj.AddComponent<T>();
            return controller;
        }

        private GameObject CreateWorldCanvasObject(string name, Transform parent, Vector3 worldPos, Vector3 worldEuler, Vector2 size)
        {
            GameObject canvasObj = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(parent, false);
            canvasObj.transform.position = worldPos;
            canvasObj.transform.eulerAngles = worldEuler;
            canvasObj.transform.localScale = Vector3.one * 0.002f;

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 10;

            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            AddTrackedDeviceRaycasterIfAvailable(canvasObj);
            return canvasObj;
        }

        private void EnsureEventSystem()
        {
            EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
                existing = esObj.GetComponent<EventSystem>();
            }

            // Prevent accidental submit/navigation from auto-triggering menu buttons on headset startup.
            existing.sendNavigationEvents = false;

            // Standalone module can steal EventSystem ownership and block XR UI module.
            StandaloneInputModule standalone = existing.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(standalone);
                }
                else
                {
                    DestroyImmediate(standalone);
                }
            }

            Type xrUiModuleType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
            if (xrUiModuleType != null)
            {
                Component xrUiModule = existing.GetComponent(xrUiModuleType);
                if (xrUiModule == null)
                {
                    xrUiModule = existing.gameObject.AddComponent(xrUiModuleType);
                }

                InputSystemUIInputModule inputSystemModule = existing.GetComponent<InputSystemUIInputModule>();
                if (inputSystemModule != null)
                {
                    inputSystemModule.enabled = false;
                }

                if (xrUiModule != null)
                {
                    TrySetBoolProperty(xrUiModule, "enableMouseInput", true);
                    TrySetBoolProperty(xrUiModule, "enableXRInput", true);
                    xrUiModule.GetType().GetProperty("enabled")?.SetValue(xrUiModule, true);
                }
            }
            else
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
        }

        private void SyncCanvasWorldCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].renderMode == RenderMode.WorldSpace && canvases[i].worldCamera != cam)
                {
                    canvases[i].worldCamera = cam;
                }
            }
        }

        private static void TrySetBoolProperty(Component component, string propertyName, bool value)
        {
            if (component == null)
            {
                return;
            }

            var property = component.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
            {
                property.SetValue(component, value);
            }
        }

        private static void AddTrackedDeviceRaycasterIfAvailable(GameObject canvasObject)
        {
            Type trackedRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (trackedRaycasterType == null)
            {
                return;
            }

            if (canvasObject.GetComponent(trackedRaycasterType) == null)
            {
                canvasObject.AddComponent(trackedRaycasterType);
            }
        }

        private void NormalizePlacementConfig()
        {
            // Keep menu comfortably above/away from the house table even if scene-saved values are stale.
            menuForwardDistance = Mathf.Max(menuForwardDistance, 1.8f);
            menuVerticalOffset = Mathf.Max(menuVerticalOffset, 0.65f);
            menuCollisionRadius = Mathf.Max(menuCollisionRadius, 0.1f);
            menuLiftPerAttempt = Mathf.Max(menuLiftPerAttempt, 0.28f);
            menuAvoidanceAttempts = Mathf.Max(menuAvoidanceAttempts, 8);
            menuPanelDepth = Mathf.Max(menuPanelDepth, 0.02f);
            menuPanelOverlapPadding = Mathf.Max(menuPanelOverlapPadding, 0.01f);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            bool menu = phase == GamePhase.Menu || phase == GamePhase.Paused;
            bool playing = phase == GamePhase.Prep || phase == GamePhase.Battle;

            mainMenu.SetVisible(menu);
            hud.SetVisible(playing);

            if (phase != GamePhase.Win && phase != GamePhase.Lose)
            {
                endGame.SetVisible(false);
            }

            if (phase == GamePhase.Menu)
            {
                mainMenu?.SetGuideText("点击 Start Match 开始。扣 Trigger 抓桌面 Seed，拖到格子松开种植。手柄射线扫到 Sun 自动收集。Y 键可随时调菜单。");
            }
            else if (phase == GamePhase.Paused)
            {
                mainMenu?.SetGuideText("已暂停：点击 Resume Match 回到本局，不会丢失进度。");
            }
            else if (phase == GamePhase.Prep)
            {
                hud?.SetGuideText("准备阶段：扣 Trigger 抓 Seed 拖到目标格子。手柄射线扫到 Sun 即收集。");
            }
            else if (phase == GamePhase.Battle)
            {
                hud?.SetGuideText("战斗阶段：持续挥线收 Sun，优先补防危险车道；右摇杆可摇头观察。");
            }
            else if (phase == GamePhase.Win)
            {
                hud?.SetGuideText("胜利！可在结算面板重开或返回菜单。");
            }
            else if (phase == GamePhase.Lose)
            {
                hud?.SetGuideText("失败：尝试更早铺 Sunflower 并分路补防。");
            }

            RefreshActivePanelPlacement();
        }

        private void HandleRunEnded(RunStats stats, bool won)
        {
            endGame.ShowResult(won, stats);
            hud.SetVisible(false);
            mainMenu.SetVisible(false);
            RefreshActivePanelPlacement();
        }

        private void HandleIdleProgress(IdleProgressResult result)
        {
            if (idlePopupText == null || !result.HasRewards)
            {
                return;
            }

            idlePopupText.transform.parent.gameObject.SetActive(true);
            idlePopupText.text = $"Welcome back! Offline gains: +{result.AwardedSun} Sun, +{result.AwardedCoins} Coins";
            CancelInvoke(nameof(HideIdlePopup));
            Invoke(nameof(HideIdlePopup), 6f);
        }

        private void HideIdlePopup()
        {
            if (idlePopupText != null)
            {
                idlePopupText.transform.parent.gameObject.SetActive(false);
            }
        }

        private void HandleCheatChanged(bool enabled)
        {
            mainMenu?.SetCheatToggle(enabled);
            cheatPanel?.SetCheatState(enabled);
            hud?.RefreshAffordability();
        }

        private void RefreshActivePanelPlacement()
        {
            if (!autoPlacePanelsInFrontOfCamera)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            GamePhase phase = GameManager.Instance != null ? GameManager.Instance.State.Phase : GamePhase.Menu;
            if ((phase == GamePhase.Menu || phase == GamePhase.Paused) && mainMenu != null)
            {
                PlaceMenuPanelSmart(mainMenu.transform, cam);
            }
            else if ((phase == GamePhase.Prep || phase == GamePhase.Battle) && hud != null)
            {
                PlacePanelInFront(hud.transform, cam, 1.45f, -0.33f, 0.55f);
            }
            else if ((phase == GamePhase.Win || phase == GamePhase.Lose) && endGame != null)
            {
                PlacePanelInFront(endGame.transform, cam, 1.75f, -0.02f, 0f);
            }

            if (cheatPanel != null)
            {
                PlacePanelInFront(cheatPanel.transform, cam, 1.35f, -0.35f, 0.55f);
            }
        }

        private void PlaceMenuPanelSmart(Transform panel, Camera cam)
        {
            if (panel == null || cam == null)
            {
                return;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = cam.transform.forward;
            }
            flatForward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
            Vector3 targetPosition = cam.transform.position +
                                     (flatForward * menuForwardDistance) +
                                     (right * menuSideOffset);
            targetPosition.y = cam.transform.position.y + menuVerticalOffset;

            Vector3 panelHalfExtents = GetPanelHalfExtentsWorld(panel);
            float sideNudge = 0.16f;
            for (int attempt = 0; attempt < Mathf.Max(1, menuAvoidanceAttempts); attempt++)
            {
                Quaternion candidateRotation = ComputePanelFacingRotation(targetPosition, cam.transform.position);
                bool occluded = IsOccludedFromCamera(cam.transform.position, targetPosition, panel);
                bool overlapping = IsPanelOverlapping(targetPosition, candidateRotation, panelHalfExtents, panel);
                if (!occluded && !overlapping)
                {
                    break;
                }

                targetPosition += Vector3.up * menuLiftPerAttempt;
                targetPosition += (attempt % 2 == 0 ? right : -right) * sideNudge;
                sideNudge += 0.08f;
            }

            panel.position = targetPosition;
            panel.rotation = ComputePanelFacingRotation(targetPosition, cam.transform.position);
        }

        private bool IsOccludedFromCamera(Vector3 cameraPos, Vector3 targetPos, Transform panelRoot)
        {
            Vector3 path = targetPos - cameraPos;
            float distance = path.magnitude;
            if (distance < 0.05f)
            {
                return false;
            }

            if (!Physics.SphereCast(cameraPos, menuCollisionRadius, path.normalized, out RaycastHit hit, distance, menuOccluderMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.transform == null)
            {
                return false;
            }

            if (panelRoot != null && hit.transform.IsChildOf(panelRoot))
            {
                return false;
            }

            return true;
        }

        private Quaternion ComputePanelFacingRotation(Vector3 panelPosition, Vector3 cameraPosition)
        {
            Vector3 toCamera = cameraPosition - panelPosition;
            if (toCamera.sqrMagnitude <= 0.0001f)
            {
                return Quaternion.identity;
            }

            // World-space Canvas visible face is opposite its forward vector.
            return Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
        }

        private Vector3 GetPanelHalfExtentsWorld(Transform panel)
        {
            RectTransform rectTransform = panel as RectTransform;
            if (rectTransform != null)
            {
                Vector3 worldScale = rectTransform.lossyScale;
                float halfWidth = Mathf.Abs(rectTransform.rect.width * worldScale.x) * 0.5f;
                float halfHeight = Mathf.Abs(rectTransform.rect.height * worldScale.y) * 0.5f;
                return new Vector3(Mathf.Max(0.05f, halfWidth), Mathf.Max(0.05f, halfHeight), Mathf.Max(0.01f, menuPanelDepth));
            }

            return new Vector3(0.6f, 0.45f, Mathf.Max(0.01f, menuPanelDepth));
        }

        private bool IsPanelOverlapping(Vector3 panelCenter, Quaternion panelRotation, Vector3 halfExtents, Transform panelRoot)
        {
            Vector3 paddedHalfExtents = halfExtents + Vector3.one * Mathf.Max(0f, menuPanelOverlapPadding);
            Collider[] hits = Physics.OverlapBox(panelCenter, paddedHalfExtents, panelRotation, menuOccluderMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                Transform hitTransform = hit.transform;
                if (panelRoot != null && hitTransform != null && hitTransform.IsChildOf(panelRoot))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void PlacePanelInFront(Transform panel, Camera cam, float forwardDistance, float verticalOffset, float horizontalOffset)
        {
            if (panel == null || cam == null)
            {
                return;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = cam.transform.forward;
            }
            flatForward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
            Vector3 targetPosition = cam.transform.position + (flatForward * forwardDistance) + (right * horizontalOffset);
            targetPosition.y = cam.transform.position.y + verticalOffset;

            panel.position = targetPosition;
            Vector3 toCamera = cam.transform.position - targetPosition;
            if (toCamera.sqrMagnitude > 0.0001f)
            {
                // World-space Canvas visible face is opposite its forward vector.
                panel.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
            }
        }

        public void ShowMainMenuInFront()
        {
            if (mainMenu == null)
            {
                return;
            }

            mainMenu.SetVisible(true);
            hud?.SetVisible(false);
            endGame?.SetVisible(false);
            RefreshActivePanelPlacement();
        }

        public void ShowGameplayHud()
        {
            if (hud == null || GameManager.Instance == null)
            {
                return;
            }

            GamePhase phase = GameManager.Instance.State.Phase;
            bool showHud = phase == GamePhase.Prep || phase == GamePhase.Battle;
            hud.SetVisible(showHud);
            if (mainMenu != null && mainMenu.gameObject.activeSelf && phase != GamePhase.Menu && phase != GamePhase.Paused)
            {
                mainMenu.SetVisible(false);
            }
            RefreshActivePanelPlacement();
        }

        private void EnsureMenuVisibilityFailSafe()
        {
            if (GameManager.Instance == null || mainMenu == null)
            {
                return;
            }

            GamePhase phase = GameManager.Instance.State.Phase;
            if ((phase == GamePhase.Menu || phase == GamePhase.Paused) && !mainMenu.gameObject.activeSelf)
            {
                mainMenu.SetVisible(true);
                RefreshActivePanelPlacement();
            }
        }
    }
}
