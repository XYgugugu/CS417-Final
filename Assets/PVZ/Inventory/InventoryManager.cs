using System.Collections.Generic;
using PVZ3D.Plants;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Core
{
    public class InventoryManager : MonoBehaviour
    {
        private const int EmptyQuickSlot = -1;

        [Header("Capacity")]
        [SerializeField] private int columns = 5;
        [SerializeField] private int rows = 3;
        [SerializeField] private int quickSlotCount = 5;

        [Header("Screen Overlay UI References")]
        [SerializeField] private Canvas inventoryCanvas;
        [SerializeField] private Canvas hudCanvasToHide;
        [SerializeField] private RectTransform inventoryPanel;
        [SerializeField] private RectTransform iconDragRoot;

        [SerializeField] private InventorySlotUI[] quickSlotViews;
        [SerializeField] private InventorySlotUI[] gridSlotViews;

        [Header("Icon Prefab")]
        [SerializeField] private InventoryItemIconUI itemIconPrefab;

        [Header("Item Icon Sprites")]
        [SerializeField] private Sprite sunflowerIcon;
        [SerializeField] private Sprite peashooterIcon;
        [SerializeField] private Sprite wallNutIcon;
        [SerializeField] private Sprite replicatorIcon;
        [SerializeField] private Sprite shovelIcon;
        [SerializeField] private Sprite defaultIcon;

        [Header("Panel Layout")]
        [SerializeField] private Vector2 collapsedPanelSize = new Vector2(700f, 180f);
        [SerializeField] private Vector2 expandedPanelSize = new Vector2(700f, 500f);
        [SerializeField] private Vector2 collapsedQuickSlotOffset = new Vector2(0f, 80f);
        [SerializeField] private RectTransform quickSlotTitle;

        [Header("Respawn Settings")]
        [SerializeField] private Transform itemRespawnPoint;
        [SerializeField] private Vector3 respawnOffset = Vector3.zero;

        [Header("Interaction Mode While Inventory Is Open")]
        [SerializeField] private Behaviour[] worldBehavioursToDisable;
        [SerializeField] private GameObject[] worldObjectsToDisable;
        [SerializeField] private Behaviour[] inventoryBehavioursToEnable;
        [SerializeField] private GameObject[] inventoryObjectsToEnable;

        [Header("Controller Navigation")]
        [SerializeField] private float joystickDeadZone = 0.55f;
        [SerializeField] private float navigationRepeatDelay = 0.28f;
        [SerializeField] private float navigationRepeatInterval = 0.16f;

        [Header("Scanning")]
        [SerializeField] private float scanInterval = 0.75f;

        public static InventoryManager Instance { get; private set; }

        private readonly List<InventoryRecord> records = new();
        private readonly Dictionary<int, List<InventoryItemIconUI>> icons = new();
        private readonly List<int> quickSlotAgeOrder = new();
        private readonly List<BehaviourState> cachedBehaviourStates = new();
        private readonly List<GameObjectState> cachedGameObjectStates = new();

        private int[] quickItemIds;
        private int nextId = 1;
        private int selectedItemId = EmptyQuickSlot;

        private bool modalVisible;
        private bool expandedVisible;
        private float nextScanTime;

        private Camera mainCamera;
        private InputAction toggleInventoryAction;
        private InputAction expandInventoryAction;
        private InputAction navigateInventoryAction;
        private InputAction respawnSelectedAction;
        private InputAction placeSelectedInQuickSlotAction;
        private Vector2Int lastNavigationStep;
        private float nextNavigationTime;
        private Vector2[] quickSlotBasePositions;
        private Vector2 quickSlotTitleBasePosition;
        private bool hasQuickSlotTitleBasePosition;

        public bool IsOpen => modalVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            int actualQuickSlotCount = quickSlotViews != null && quickSlotViews.Length > 0
                ? quickSlotViews.Length
                : Mathf.Max(1, quickSlotCount);

            quickItemIds = new int[actualQuickSlotCount];

            for (int i = 0; i < quickItemIds.Length; i++)
            {
                quickItemIds[i] = EmptyQuickSlot;
            }

            ConfigureSlotViews();
            SetModalVisible(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnEnable()
        {
            CreateInputActions();

            toggleInventoryAction.Enable();
            expandInventoryAction.Enable();
            navigateInventoryAction.Enable();
            respawnSelectedAction.Enable();
            placeSelectedInQuickSlotAction.Enable();
        }

        private void OnDisable()
        {
            toggleInventoryAction?.Disable();
            expandInventoryAction?.Disable();
            navigateInventoryAction?.Disable();
            respawnSelectedAction?.Disable();
            placeSelectedInQuickSlotAction?.Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;

            toggleInventoryAction?.Dispose();
            expandInventoryAction?.Dispose();
            navigateInventoryAction?.Dispose();
            respawnSelectedAction?.Dispose();
            placeSelectedInQuickSlotAction?.Dispose();
        }

        private void Update()
        {
            if (toggleInventoryAction != null && toggleInventoryAction.WasPressedThisFrame())
            {
                SetModalVisible(!modalVisible);
            }

            if (modalVisible && expandInventoryAction != null && expandInventoryAction.WasPressedThisFrame())
            {
                expandedVisible = !expandedVisible;
                RefreshModal();
            }

            if (modalVisible)
            {
                HandleInventoryNavigation();
            }

            if (modalVisible && respawnSelectedAction != null && respawnSelectedAction.WasPressedThisFrame())
            {
                RespawnSelectedItem();
            }

            if (modalVisible && placeSelectedInQuickSlotAction != null && placeSelectedInQuickSlotAction.WasPressedThisFrame())
            {
                PlaceSelectedItemInQuickSlot();
            }

            if (Time.unscaledTime >= nextScanTime)
            {
                nextScanTime = Time.unscaledTime + scanInterval;
                AttachCollectiblesInScene();
            }
        }

        public bool TryCollect(GameObject item)
        {
            if (item == null)
            {
                return false;
            }

            if (records.Exists(record => record.StoredObject == item))
            {
                return true;
            }

            Vector2Int itemSize = GetInventorySize(item);

            if (!TryFindFreePosition(itemSize, out Vector2Int position))
            {
                Debug.Log("Inventory is full. Drop or rearrange items before collecting more.");
                return false;
            }

            InventoryRecord record = new InventoryRecord
            {
                Id = nextId++,
                DisplayName = GetDisplayName(item),
                StoredObject = item,
                Position = position,
                Size = itemSize
            };

            records.Add(record);
            AssignQuickSlotOnCollect(record.Id);
            selectedItemId = record.Id;
            StowWorldObject(item);
            RefreshModal();

            return true;
        }

        public void SelectItem(int itemId)
        {
            if (FindRecord(itemId) == null)
            {
                selectedItemId = EmptyQuickSlot;
            }
            else
            {
                selectedItemId = itemId;
            }

            RefreshSelectionVisuals();
        }

        public void MoveItemToSlot(int itemId, InventorySlotUI targetSlot)
        {
            if (targetSlot == null)
            {
                RefreshModal();
                return;
            }

            InventoryRecord record = FindRecord(itemId);

            if (record == null)
            {
                RefreshModal();
                return;
            }

            if (targetSlot.IsQuickSlot)
            {
                AssignQuickSlot(targetSlot.Index, itemId);
                RefreshModal();
                return;
            }

            if (expandedVisible)
            {
                if (CanPlaceAt(record, targetSlot.GridPosition))
                {
                    record.Position = targetSlot.GridPosition;
                }
            }

            RefreshModal();
        }

        public void RespawnSelectedItem()
        {
            if (selectedItemId == EmptyQuickSlot)
            {
                Debug.Log("No inventory item selected.");
                return;
            }

            RespawnItemFromInventory(selectedItemId);
        }

        public void PlaceSelectedItemInQuickSlot()
        {
            if (selectedItemId == EmptyQuickSlot || FindRecord(selectedItemId) == null)
            {
                Debug.Log("No inventory item selected.");
                return;
            }

            AssignQuickSlotOnCollect(selectedItemId);
            RefreshModal();
        }

        public void RespawnItemFromInventory(int itemId)
        {
            InventoryRecord record = FindRecord(itemId);

            if (record == null || record.StoredObject == null)
            {
                RemoveRecord(itemId);
                RefreshModal();
                return;
            }

            GameObject item = record.StoredObject;

            RemoveRecord(itemId);

            if (selectedItemId == itemId)
            {
                selectedItemId = EmptyQuickSlot;
            }

            Vector3 respawnPosition = GetRespawnPosition();
            Quaternion respawnRotation = GetRespawnRotation();

            item.transform.SetParent(null, true);
            item.transform.SetPositionAndRotation(respawnPosition, respawnRotation);
            item.SetActive(true);

            Rigidbody body = item.GetComponent<Rigidbody>();

            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            InventoryCollectible collectible = item.GetComponent<InventoryCollectible>();

            if (collectible == null)
            {
                collectible = item.AddComponent<InventoryCollectible>();
            }

            collectible.EnsureGrabInteractable();

            RefreshModal();
        }

        public void RefreshModal()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.sizeDelta = expandedVisible ? expandedPanelSize : collapsedPanelSize;
            }

            if (gridSlotViews != null)
            {
                foreach (InventorySlotUI slot in gridSlotViews)
                {
                    if (slot != null)
                    {
                        slot.SetVisible(expandedVisible);
                        slot.SetOccupied(IsGridSlotOccupied(slot.GridPosition));
                    }
                }
            }

            if (quickSlotViews != null)
            {
                ApplyQuickSlotLayout();

                for (int i = 0; i < quickSlotViews.Length; i++)
                {
                    bool occupied = i < quickItemIds.Length && quickItemIds[i] != EmptyQuickSlot;

                    if (quickSlotViews[i] != null)
                    {
                        quickSlotViews[i].SetVisible(true);
                        quickSlotViews[i].SetOccupied(occupied);
                    }
                }
            }

            foreach (List<InventoryItemIconUI> itemIcons in icons.Values)
            {
                foreach (InventoryItemIconUI icon in itemIcons)
                {
                    if (icon != null)
                    {
                        Destroy(icon.gameObject);
                    }
                }
            }

            icons.Clear();

            if (!modalVisible)
            {
                return;
            }

            EnsureSelectedItemVisible();

            if (quickSlotViews != null)
            {
                for (int i = 0; i < quickItemIds.Length && i < quickSlotViews.Length; i++)
                {
                    InventoryRecord quickRecord = FindRecord(quickItemIds[i]);

                    if (quickRecord != null &&
                        quickRecord.StoredObject != null &&
                        quickSlotViews[i] != null)
                    {
                        CreateIcon(quickRecord, quickSlotViews[i].transform as RectTransform);
                    }
                }
            }

            foreach (InventoryRecord record in records)
            {
                if (record.StoredObject == null)
                {
                    continue;
                }

                if (expandedVisible)
                {
                    InventorySlotUI gridSlot = FindGridSlot(record.Position);

                    if (gridSlot != null)
                    {
                        CreateIcon(record, gridSlot.transform as RectTransform);
                    }

                    continue;
                }
            }

            RefreshSelectionVisuals();
        }

        private void CreateIcon(InventoryRecord record, RectTransform parentSlot)
        {
            if (parentSlot == null)
            {
                return;
            }

            InventoryItemIconUI icon = itemIconPrefab != null
                ? Instantiate(itemIconPrefab, parentSlot)
                : CreateRuntimeIcon(parentSlot);

            if (icon == null)
            {
                return;
            }

            icon.name = $"Icon - {record.DisplayName}";

            RectTransform iconRect = icon.transform as RectTransform;

            if (iconRect != null)
            {
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = GetIconSize(record.Size);
                iconRect.localScale = Vector3.one;
            }

            Canvas dragCanvas = inventoryCanvas;
            RectTransform dragRoot = iconDragRoot != null ? iconDragRoot : inventoryPanel;

            icon.Initialize(
                record.Id,
                GetIconForItem(record.StoredObject),
                dragCanvas,
                dragRoot
            );

            icon.SetSelected(record.Id == selectedItemId);

            if (!icons.TryGetValue(record.Id, out List<InventoryItemIconUI> itemIcons))
            {
                itemIcons = new List<InventoryItemIconUI>();
                icons[record.Id] = itemIcons;
            }

            itemIcons.Add(icon);
        }

        private void ConfigureSlotViews()
        {
            if (quickSlotViews != null)
            {
                quickSlotBasePositions = new Vector2[quickSlotViews.Length];

                for (int i = 0; i < quickSlotViews.Length; i++)
                {
                    if (quickSlotViews[i] != null)
                    {
                        quickSlotViews[i].ConfigureSlot(true, i, new Vector2Int(i, 0));

                        RectTransform quickRect = quickSlotViews[i].transform as RectTransform;
                        quickSlotBasePositions[i] = quickRect != null
                            ? quickRect.anchoredPosition
                            : Vector2.zero;
                    }
                }
            }

            if (quickSlotTitle != null)
            {
                quickSlotTitleBasePosition = quickSlotTitle.anchoredPosition;
                hasQuickSlotTitleBasePosition = true;
            }

            if (gridSlotViews == null || gridSlotViews.Length == 0)
            {
                return;
            }

            int safeColumns = Mathf.Max(1, columns);
            int visibleRows = Mathf.CeilToInt(gridSlotViews.Length / (float)safeColumns);
            rows = Mathf.Max(rows, visibleRows);

            for (int i = 0; i < gridSlotViews.Length; i++)
            {
                if (gridSlotViews[i] == null)
                {
                    continue;
                }

                Vector2Int position = new Vector2Int(i % safeColumns, i / safeColumns);
                gridSlotViews[i].ConfigureSlot(false, i, position);
            }
        }

        private void ApplyQuickSlotLayout()
        {
            Vector2 offset = expandedVisible ? Vector2.zero : collapsedQuickSlotOffset;

            if (quickSlotViews != null && quickSlotBasePositions != null)
            {
                for (int i = 0; i < quickSlotViews.Length && i < quickSlotBasePositions.Length; i++)
                {
                    if (quickSlotViews[i] == null)
                    {
                        continue;
                    }

                    RectTransform quickRect = quickSlotViews[i].transform as RectTransform;
                    if (quickRect != null)
                    {
                        quickRect.anchoredPosition = quickSlotBasePositions[i] + offset;
                    }
                }
            }

            if (quickSlotTitle != null)
            {
                quickSlotTitle.gameObject.SetActive(true);

                if (hasQuickSlotTitleBasePosition)
                {
                    quickSlotTitle.anchoredPosition = expandedVisible
                        ? quickSlotTitleBasePosition
                        : new Vector2(quickSlotTitleBasePosition.x, 0f);
                }
            }
        }

        private InventoryItemIconUI CreateRuntimeIcon(RectTransform parentSlot)
        {
            GameObject iconObject = new GameObject("Runtime Inventory Icon");
            iconObject.transform.SetParent(parentSlot, false);
            iconObject.AddComponent<Image>();
            iconObject.AddComponent<CanvasGroup>();
            return iconObject.AddComponent<InventoryItemIconUI>();
        }

        private void RefreshSelectionVisuals()
        {
            foreach (KeyValuePair<int, List<InventoryItemIconUI>> pair in icons)
            {
                foreach (InventoryItemIconUI icon in pair.Value)
                {
                    if (icon != null)
                    {
                        icon.SetSelected(pair.Key == selectedItemId);
                    }
                }
            }
        }

        private Vector2 GetIconSize(Vector2Int itemSize)
        {
            float cellSize = 80f;

            return new Vector2(
                Mathf.Max(1, itemSize.x) * cellSize,
                Mathf.Max(1, itemSize.y) * cellSize
            );
        }

        private Sprite GetIconForItem(GameObject item)
        {
            if (item == null)
            {
                return defaultIcon;
            }

            if (item.GetComponent<SunflowerPlant>() != null)
            {
                return sunflowerIcon != null ? sunflowerIcon : defaultIcon;
            }

            if (item.GetComponent<PeashooterPlant>() != null)
            {
                return peashooterIcon != null ? peashooterIcon : defaultIcon;
            }

            if (item.GetComponent<WallNutPlant>() != null)
            {
                return wallNutIcon != null ? wallNutIcon : defaultIcon;
            }

            if (item.GetComponent<Replicator>() != null)
            {
                return replicatorIcon != null ? replicatorIcon : defaultIcon;
            }

            if (item.GetComponent<shovel>() != null || HasTag(item, "Shovel"))
            {
                return shovelIcon != null ? shovelIcon : defaultIcon;
            }

            return defaultIcon;
        }

        private void SetModalVisible(bool visible)
        {
            modalVisible = visible;

            if (!visible)
            {
                expandedVisible = false;
            }

            if (inventoryCanvas != null)
            {
                inventoryCanvas.gameObject.SetActive(visible);
            }

            if (hudCanvasToHide != null)
            {
                hudCanvasToHide.gameObject.SetActive(!visible);
            }

            ApplyInventoryInteractionMode(visible);
            RefreshModal();
        }

        private void CreateInputActions()
        {
            if (toggleInventoryAction == null)
            {
                toggleInventoryAction = new InputAction("Toggle Inventory", InputActionType.Button);
                toggleInventoryAction.AddBinding("<Keyboard>/i");
                toggleInventoryAction.AddBinding("<XRController>{LeftHand}/primaryButton");
            }

            if (expandInventoryAction == null)
            {
                expandInventoryAction = new InputAction("Expand Inventory", InputActionType.Button);
                expandInventoryAction.AddBinding("<Keyboard>/tab");
                expandInventoryAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
            }

            if (navigateInventoryAction == null)
            {
                navigateInventoryAction = new InputAction("Navigate Inventory", InputActionType.Value);
                navigateInventoryAction.AddBinding("<XRController>{LeftHand}/thumbstick");
                navigateInventoryAction.AddBinding("<XRController>{LeftHand}/primary2DAxis");
                navigateInventoryAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
                navigateInventoryAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
            }

            if (respawnSelectedAction == null)
            {
                respawnSelectedAction = new InputAction("Place Selected Inventory Item In World", InputActionType.Button);
                respawnSelectedAction.AddBinding("<Keyboard>/enter");
                respawnSelectedAction.AddBinding("<Keyboard>/space");
                respawnSelectedAction.AddBinding("<XRController>{RightHand}/primaryButton");
            }

            if (placeSelectedInQuickSlotAction == null)
            {
                placeSelectedInQuickSlotAction = new InputAction("Place Selected Inventory Item In Quick Slot", InputActionType.Button);
                placeSelectedInQuickSlotAction.AddBinding("<Keyboard>/q");
                placeSelectedInQuickSlotAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            }
        }

        private void HandleInventoryNavigation()
        {
            if (navigateInventoryAction == null)
            {
                return;
            }

            Vector2 input = navigateInventoryAction.ReadValue<Vector2>();
            Vector2Int step = GetNavigationStep(input);

            if (step == Vector2Int.zero)
            {
                lastNavigationStep = Vector2Int.zero;
                nextNavigationTime = 0f;
                return;
            }

            bool newDirection = step != lastNavigationStep;
            if (!newDirection && Time.unscaledTime < nextNavigationTime)
            {
                return;
            }

            MoveSelection(step);
            lastNavigationStep = step;
            nextNavigationTime = Time.unscaledTime + (newDirection ? navigationRepeatDelay : navigationRepeatInterval);
        }

        private Vector2Int GetNavigationStep(Vector2 input)
        {
            if (input.sqrMagnitude < joystickDeadZone * joystickDeadZone)
            {
                return Vector2Int.zero;
            }

            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
            {
                return input.x > 0f ? Vector2Int.right : Vector2Int.left;
            }

            return input.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        private void MoveSelection(Vector2Int step)
        {
            List<int> visibleItemIds = GetVisibleItemIdsForNavigation();

            if (visibleItemIds.Count == 0)
            {
                selectedItemId = EmptyQuickSlot;
                RefreshSelectionVisuals();
                return;
            }

            int currentIndex = visibleItemIds.IndexOf(selectedItemId);
            if (currentIndex < 0)
            {
                selectedItemId = visibleItemIds[0];
                RefreshSelectionVisuals();
                return;
            }

            int delta = 0;
            if (expandedVisible && step.y != 0)
            {
                delta = step.y > 0 ? -columns : columns;
            }
            else if (step.x != 0)
            {
                delta = step.x > 0 ? 1 : -1;
            }
            else if (step.y != 0)
            {
                delta = step.y > 0 ? -1 : 1;
            }

            if (delta == 0)
            {
                return;
            }

            int nextIndex = WrapIndex(currentIndex + delta, visibleItemIds.Count);
            selectedItemId = visibleItemIds[nextIndex];
            RefreshSelectionVisuals();
        }

        private void EnsureSelectedItemVisible()
        {
            List<int> visibleItemIds = GetVisibleItemIdsForNavigation();

            if (visibleItemIds.Count == 0)
            {
                selectedItemId = EmptyQuickSlot;
                return;
            }

            if (!visibleItemIds.Contains(selectedItemId))
            {
                selectedItemId = visibleItemIds[0];
            }
        }

        private List<int> GetVisibleItemIdsForNavigation()
        {
            List<int> visibleItemIds = new List<int>();

            if (expandedVisible)
            {
                List<InventoryRecord> sortedRecords = new List<InventoryRecord>(records);
                sortedRecords.Sort((left, right) =>
                {
                    int yCompare = left.Position.y.CompareTo(right.Position.y);
                    return yCompare != 0 ? yCompare : left.Position.x.CompareTo(right.Position.x);
                });

                foreach (InventoryRecord record in sortedRecords)
                {
                    if (record.StoredObject != null)
                    {
                        visibleItemIds.Add(record.Id);
                    }
                }

                return visibleItemIds;
            }

            for (int i = 0; i < quickItemIds.Length; i++)
            {
                int itemId = quickItemIds[i];
                if (itemId != EmptyQuickSlot && FindRecord(itemId) != null)
                {
                    visibleItemIds.Add(itemId);
                }
            }

            return visibleItemIds;
        }

        private int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            while (index < 0)
            {
                index += count;
            }

            return index % count;
        }

        private void ApplyInventoryInteractionMode(bool inventoryOpen)
        {
            if (inventoryOpen)
            {
                cachedBehaviourStates.Clear();
                cachedGameObjectStates.Clear();

                CacheAndSetBehaviours(worldBehavioursToDisable, false);
                CacheAndSetGameObjects(worldObjectsToDisable, false);
                CacheAndSetBehaviours(inventoryBehavioursToEnable, true);
                CacheAndSetGameObjects(inventoryObjectsToEnable, true);
                return;
            }

            RestoreInteractionModeStates();
        }

        private void CacheAndSetBehaviours(Behaviour[] behaviours, bool enabled)
        {
            if (behaviours == null)
            {
                return;
            }

            foreach (Behaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                cachedBehaviourStates.Add(new BehaviourState
                {
                    Behaviour = behaviour,
                    WasEnabled = behaviour.enabled
                });

                behaviour.enabled = enabled;
            }
        }

        private void CacheAndSetGameObjects(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            foreach (GameObject target in objects)
            {
                if (target == null)
                {
                    continue;
                }

                cachedGameObjectStates.Add(new GameObjectState
                {
                    GameObject = target,
                    WasActive = target.activeSelf
                });

                target.SetActive(active);
            }
        }

        private void RestoreInteractionModeStates()
        {
            foreach (BehaviourState state in cachedBehaviourStates)
            {
                if (state.Behaviour != null)
                {
                    state.Behaviour.enabled = state.WasEnabled;
                }
            }

            foreach (GameObjectState state in cachedGameObjectStates)
            {
                if (state.GameObject != null)
                {
                    state.GameObject.SetActive(state.WasActive);
                }
            }

            cachedBehaviourStates.Clear();
            cachedGameObjectStates.Clear();
        }

        private Vector3 GetRespawnPosition()
        {
            if (itemRespawnPoint != null)
            {
                return itemRespawnPoint.position + itemRespawnPoint.TransformDirection(respawnOffset);
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                return mainCamera.transform.position + mainCamera.transform.forward * 1f;
            }

            return transform.position + Vector3.forward;
        }

        private Quaternion GetRespawnRotation()
        {
            if (itemRespawnPoint != null)
            {
                return itemRespawnPoint.rotation;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                return Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);
            }

            return Quaternion.identity;
        }

        private void AttachCollectiblesInScene()
        {
            foreach (PlantBase plant in FindObjectsByType<PlantBase>(FindObjectsSortMode.None))
            {
                RegisterCollectible(plant.gameObject);
            }

            foreach (Replicator replicator in FindObjectsByType<Replicator>(FindObjectsSortMode.None))
            {
                RegisterCollectible(replicator.gameObject);
            }

            foreach (shovel shovelTool in FindObjectsByType<shovel>(FindObjectsSortMode.None))
            {
                RegisterCollectible(shovelTool.gameObject);
            }

            foreach (XRGrabInteractable grab in FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                if (IsToolCandidate(grab.gameObject))
                {
                    RegisterCollectible(grab.gameObject);
                }
            }
        }

        private void RegisterCollectible(GameObject item)
        {
            if (item == null || item.GetComponent<InventoryCollectible>() != null)
            {
                return;
            }

            item.AddComponent<InventoryCollectible>();
        }

        private void StowWorldObject(GameObject item)
        {
            Rigidbody body = item.GetComponent<Rigidbody>();

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            item.transform.SetParent(transform, true);
            item.SetActive(false);
        }

        public void EndIconDrag(int itemId, bool droppedOnSlot)
        {
            if (droppedOnSlot)
            {
                RefreshModal();
                return;
            }

            if (modalVisible)
            {
                RespawnItemFromInventory(itemId);
                return;
            }

            RefreshModal();
        }

        private void RemoveRecord(int itemId)
        {
            records.RemoveAll(record => record.Id == itemId);
            quickSlotAgeOrder.Remove(itemId);

            for (int i = 0; i < quickItemIds.Length; i++)
            {
                if (quickItemIds[i] == itemId)
                {
                    quickItemIds[i] = EmptyQuickSlot;
                }
            }
        }

        private void AssignQuickSlotOnCollect(int itemId)
        {
            if (GetQuickSlotIndex(itemId) >= 0)
            {
                MarkQuickSlotNewest(itemId);
                return;
            }

            for (int i = 0; i < quickItemIds.Length; i++)
            {
                if (quickItemIds[i] == EmptyQuickSlot)
                {
                    quickItemIds[i] = itemId;
                    MarkQuickSlotNewest(itemId);
                    return;
                }
            }

            int oldestItemId = GetOldestQuickSlotItemId();
            int oldestSlotIndex = GetQuickSlotIndex(oldestItemId);

            if (oldestSlotIndex >= 0)
            {
                quickItemIds[oldestSlotIndex] = itemId;
                quickSlotAgeOrder.Remove(oldestItemId);
                MarkQuickSlotNewest(itemId);
            }
        }

        private void AssignQuickSlot(int slotIndex, int itemId)
        {
            if (slotIndex < 0 || slotIndex >= quickItemIds.Length)
            {
                return;
            }

            for (int i = 0; i < quickItemIds.Length; i++)
            {
                if (quickItemIds[i] == itemId)
                {
                    quickItemIds[i] = EmptyQuickSlot;
                }
            }

            if (quickItemIds[slotIndex] != EmptyQuickSlot)
            {
                quickSlotAgeOrder.Remove(quickItemIds[slotIndex]);
            }

            quickItemIds[slotIndex] = itemId;
            MarkQuickSlotNewest(itemId);
        }

        private int GetOldestQuickSlotItemId()
        {
            for (int i = 0; i < quickSlotAgeOrder.Count; i++)
            {
                int itemId = quickSlotAgeOrder[i];
                if (GetQuickSlotIndex(itemId) >= 0)
                {
                    return itemId;
                }
            }

            for (int i = 0; i < quickItemIds.Length; i++)
            {
                if (quickItemIds[i] != EmptyQuickSlot)
                {
                    return quickItemIds[i];
                }
            }

            return EmptyQuickSlot;
        }

        private void MarkQuickSlotNewest(int itemId)
        {
            if (itemId == EmptyQuickSlot)
            {
                return;
            }

            quickSlotAgeOrder.Remove(itemId);
            quickSlotAgeOrder.Add(itemId);
        }

        private int GetQuickSlotIndex(int itemId)
        {
            for (int i = 0; i < quickItemIds.Length; i++)
            {
                if (quickItemIds[i] == itemId)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool TryFindFreePosition(Vector2Int itemSize, out Vector2Int position)
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);

                    if (CanPlaceAt(null, candidate, itemSize))
                    {
                        position = candidate;
                        return true;
                    }
                }
            }

            position = Vector2Int.zero;
            return false;
        }

        private bool CanPlaceAt(InventoryRecord movingRecord, Vector2Int position)
        {
            Vector2Int size = movingRecord != null ? movingRecord.Size : Vector2Int.one;
            return CanPlaceAt(movingRecord, position, size);
        }

        private bool CanPlaceAt(InventoryRecord movingRecord, Vector2Int position, Vector2Int size)
        {
            if (position.x < 0 ||
                position.y < 0 ||
                position.x + size.x > columns ||
                position.y + size.y > rows)
            {
                return false;
            }

            RectInt movingRect = new RectInt(position, size);

            foreach (InventoryRecord record in records)
            {
                if (movingRecord != null && record.Id == movingRecord.Id)
                {
                    continue;
                }

                RectInt occupied = new RectInt(record.Position, record.Size);

                if (movingRect.Overlaps(occupied))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsGridSlotOccupied(Vector2Int gridPosition)
        {
            foreach (InventoryRecord record in records)
            {
                RectInt occupied = new RectInt(record.Position, record.Size);

                if (occupied.Contains(gridPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private InventorySlotUI FindGridSlot(Vector2Int position)
        {
            if (gridSlotViews == null)
            {
                return null;
            }

            foreach (InventorySlotUI slot in gridSlotViews)
            {
                if (slot != null && slot.GridPosition == position)
                {
                    return slot;
                }
            }

            return null;
        }

        private InventoryRecord FindRecord(int itemId)
        {
            return records.Find(record => record.Id == itemId);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            mainCamera = Camera.main;
            nextScanTime = 0f;
        }

        private static string GetDisplayName(GameObject item)
        {
            string displayName = item.name.Replace("(Clone)", string.Empty).Trim();

            if (item.GetComponent<SunflowerPlant>() != null)
            {
                return "Sunflower";
            }

            if (item.GetComponent<PeashooterPlant>() != null)
            {
                return "Peashooter";
            }

            if (item.GetComponent<WallNutPlant>() != null)
            {
                return "Wall-Nut";
            }

            if (item.GetComponent<Replicator>() != null)
            {
                return "Replicator";
            }

            if (item.GetComponent<shovel>() != null || HasTag(item, "Shovel"))
            {
                return "Shovel";
            }

            return string.IsNullOrWhiteSpace(displayName) ? "Item" : displayName;
        }

        private static Vector2Int GetInventorySize(GameObject item)
        {
            return Vector2Int.one;
        }

        private static bool IsToolCandidate(GameObject item)
        {
            if (item == null)
            {
                return false;
            }

            string lowerName = item.name.ToLowerInvariant();

            return item.GetComponent<Replicator>() != null ||
                   item.GetComponent<shovel>() != null ||
                   HasTag(item, "Shovel") ||
                   lowerName.Contains("replicator") ||
                   lowerName.Contains("shovel");
        }

        private static bool HasTag(GameObject item, string tagName)
        {
            return item != null && item.tag == tagName;
        }

        private class InventoryRecord
        {
            public int Id;
            public string DisplayName;
            public GameObject StoredObject;
            public Vector2Int Position;
            public Vector2Int Size;
        }

        private struct BehaviourState
        {
            public Behaviour Behaviour;
            public bool WasEnabled;
        }

        private struct GameObjectState
        {
            public GameObject GameObject;
            public bool WasActive;
        }
    }
}
