using System;
using PVZ3D.Grid;
using PVZ3D.Interaction;
using PVZ3D.Plants;
using PVZ3D.Resources;
using PVZ3D.Save;
using PVZ3D.UI;
using PVZ3D.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;
#endif

namespace PVZ3D.Core
{
    public class PVZSceneBootstrap : MonoBehaviour
    {
        private static bool created;
        private float nextAudioListenerCheckTime;
        private int repairSweepBudget = 12;

        private static readonly Vector3 DefaultComfortSpawnPosition = new Vector3(-3.6f, 0f, -2.2f);
        private static readonly Vector3 DefaultComfortSpawnEuler = new Vector3(0f, 72f, 0f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return;
            }

            if (created || UnityEngine.Object.FindFirstObjectByType<PVZSceneBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObj = new GameObject("PVZSceneBootstrap");
            bootstrapObj.AddComponent<PVZSceneBootstrap>();
            created = true;
        }

        private void Awake()
        {
            if (UnityEngine.Object.FindObjectsByType<PVZSceneBootstrap>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            BuildSceneHierarchy();
            EnsureXRFoundation();
#if UNITY_EDITOR
            EnsureEditorSimulationSupport();
#endif
            EnsureSingleAudioListener();
            EnsureManagers();
            RepairAllErrorShaderRenderers();
        }

        private void Update()
        {
            if (!Application.isPlaying || Time.unscaledTime < nextAudioListenerCheckTime)
            {
                return;
            }

            nextAudioListenerCheckTime = Time.unscaledTime + 0.75f;
            EnsureSingleAudioListener();

            if (repairSweepBudget > 0)
            {
                RepairAllErrorShaderRenderers();
                repairSweepBudget--;
            }
        }

        private void BuildSceneHierarchy()
        {
            EnsureNamedObject("Managers");
            EnsureNamedObject("XR Rig");
            EnsureNamedObject("Environment");
            EnsureNamedObject("Grid");
            EnsureNamedObject("Spawners");
            EnsureNamedObject("UI");
            GameObject runtimeRoot = EnsureNamedObject("Runtime");
            EnsureChildObject(runtimeRoot.transform, "Plants");
            EnsureChildObject(runtimeRoot.transform, "Zombies");
            EnsureChildObject(runtimeRoot.transform, "Pickups");
            EnsureNamedObject("Debug");

            CreateGroundIfMissing();
            CreateHorizonGroundIfMissing();
            CreateSafetyFloorIfMissing();
            CreateBaseIfMissing();
            RefreshEnvironmentMaterials();
            EnsureBaseSolidBody();
            EnsureMainCameraPosition();
        }

        private void RefreshEnvironmentMaterials()
        {
            Renderer ground = GameObject.Find("Environment/Ground")?.GetComponent<Renderer>();
            if (ground != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(ground, new Color(0.16f, 0.33f, 0.16f));
            }

            Renderer lawnPad = GameObject.Find("Environment/LawnPad")?.GetComponent<Renderer>();
            if (lawnPad != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(lawnPad, new Color(0.25f, 0.45f, 0.26f));
            }

            Renderer horizon = GameObject.Find("Environment/HorizonGround")?.GetComponent<Renderer>();
            if (horizon != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(horizon, new Color(0.22f, 0.38f, 0.25f));
            }

            Renderer body = GameObject.Find("Environment/HouseBase/Body")?.GetComponent<Renderer>();
            if (body != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(body, new Color(0.47f, 0.32f, 0.19f));
            }

            Renderer roof = GameObject.Find("Environment/HouseBase/Roof")?.GetComponent<Renderer>();
            if (roof != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(roof, new Color(0.36f, 0.18f, 0.16f));
            }

            Renderer door = GameObject.Find("Environment/HouseBase/Door")?.GetComponent<Renderer>();
            if (door != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(door, new Color(0.22f, 0.11f, 0.08f));
            }
        }

        private void EnsureManagers()
        {
            GameObject managersRoot = EnsureNamedObject("Managers");

            EnsureManager<SaveSystem>(managersRoot, "SaveSystem");
            EnsureManager<ResourceManager>(managersRoot, "ResourceManager");
            EnsureManager<LawnGridManager>(EnsureNamedObject("Grid"), "LawnGridManager");
            EnsureManager<SunSpawner>(EnsureNamedObject("Spawners"), "SunSpawner");
            EnsureManager<XRPickupSweepCollector>(managersRoot, "XRPickupSweepCollector");
            EnsureManager<XRMenuHotkeyController>(managersRoot, "XRMenuHotkeyController");
            EnsureManager<PlantPlacementManager>(managersRoot, "PlantPlacementManager");
            EnsureManager<PlantTraySpawner>(managersRoot, "PlantTraySpawner");
            EnsureManager<ZombieSpawner>(EnsureNamedObject("Spawners"), "ZombieSpawner");
            EnsureManager<WaveManager>(managersRoot, "WaveManager");
            EnsureManager<GameManager>(managersRoot, "GameManager");
            EnsureManager<AudioFeedbackManager>(managersRoot, "AudioFeedbackManager");
            EnsureManager<UIManager>(EnsureNamedObject("UI"), "UIManager");

#if UNITY_EDITOR
            Type fallbackType = Type.GetType("PVZ3D.Interaction.VRControllerGameplayFallback, Assembly-CSharp");
            if (fallbackType != null && FindAnyLoadedObject(fallbackType) == null)
            {
                GameObject fallbackObj = new GameObject("EditorGameplayFallback");
                fallbackObj.transform.SetParent(EnsureNamedObject("Debug").transform, false);
                fallbackObj.AddComponent(fallbackType);
            }
#endif
        }

        private void EnsureXRFoundation()
        {
            GameObject xrRoot = EnsureNamedObject("XR Rig");
            bool createdXrOrigin = false;

            Type xrOriginType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
            if (xrOriginType != null && FindAnyLoadedObject(xrOriginType) == null)
            {
                if (TryInstantiateRuntimePrefab("Prefabs/XR Origin (XR Rig)", xrRoot.transform, out _))
                {
                    createdXrOrigin = true;
                    DisableStandaloneMainCameraRuntimeSafe();
                }
            }
#if UNITY_EDITOR
            if (Application.isPlaying && xrOriginType != null && FindAnyLoadedObject(xrOriginType) == null)
            {
                if (TryInstantiateEditorPrefabFromCandidates(
                        new[]
                        {
                            "Assets/Samples/XR Interaction Toolkit/Starter Assets/Prefabs/XR Origin (XR Rig).prefab",
                            "Packages/com.unity.xr.interaction.toolkit/Samples~/Starter Assets/Prefabs/XR Origin (XR Rig).prefab",
                        },
                        xrRoot.transform,
                        out _))
                {
                    createdXrOrigin = true;
                    DisableStandaloneMainCameraRuntimeSafe();
                }
            }
#endif
            if (xrOriginType != null && FindAnyLoadedObject(xrOriginType) == null)
            {
                Component originComponent = xrRoot.AddComponent(xrOriginType);
                createdXrOrigin = true;
                Camera cam = Camera.main;
                if (cam == null)
                {
                    GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                    cam = camObj.GetComponent<Camera>();
                    cam.tag = "MainCamera";
                }

                cam.transform.SetParent(xrRoot.transform, true);
                cam.transform.localPosition = new Vector3(-3.3f, 1.65f, 0f);
                cam.transform.localRotation = Quaternion.Euler(12f, 78f, 0f);

                TrySetProperty(originComponent, "Camera", cam);
                TrySetProperty(originComponent, "CameraFloorOffsetObject", xrRoot);
            }

            if (createdXrOrigin)
            {
                ApplyComfortSpawnTransform(xrRoot.transform);
            }
            else
            {
                ApplyComfortSpawnTransform(xrRoot.transform);
            }

            Type interactionManagerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager, Unity.XR.Interaction.Toolkit");
            if (interactionManagerType != null && FindAnyLoadedObject(interactionManagerType) == null)
            {
                GameObject managerObj = new GameObject("XR Interaction Manager");
                managerObj.transform.SetParent(xrRoot.transform, false);
                managerObj.AddComponent(interactionManagerType);
            }

            Type rayInteractorType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor, Unity.XR.Interaction.Toolkit");
            Type nearFarInteractorType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor, Unity.XR.Interaction.Toolkit");
            bool hasRayInteractor = rayInteractorType != null && FindAnyLoadedObject(rayInteractorType) != null;
            bool hasNearFarInteractor = nearFarInteractorType != null && FindAnyLoadedObject(nearFarInteractorType) != null;
            if (!hasRayInteractor && !hasNearFarInteractor)
            {
                EnsureControllerObject(xrRoot.transform, "Left Controller", true);
                EnsureControllerObject(xrRoot.transform, "Right Controller", false);
            }

            ConfigureUiInteractorSupport(rayInteractorType);
            ConfigureUiInteractorSupport(nearFarInteractorType);
            EnsureComponentOnRoot<XRFlightLocomotionController>(xrRoot);
            EnsureComponentOnRoot<XRControllerTriggerGrabConfigurator>(xrRoot);

            // Keep only XR camera active when possible to avoid duplicate listeners/camera conflict.
            if (xrOriginType != null && FindAnyLoadedObject(xrOriginType) != null)
            {
                DisableStandaloneMainCameraRuntimeSafe();
            }
        }

        private void EnsureControllerObject(Transform parent, string objectName, bool left)
        {
            if (HasDescendantNamed(parent, objectName))
            {
                return;
            }

            GameObject controller = new GameObject(objectName);
            controller.transform.SetParent(parent, false);
            controller.transform.localPosition = new Vector3(-2.8f, 1.3f, left ? -0.25f : 0.25f);

            AddIfTypeExists(controller, "UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor, Unity.XR.Interaction.Toolkit");
            AddIfTypeExists(controller, "UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor, Unity.XR.Interaction.Toolkit");
            AddIfTypeExists(controller, "UnityEngine.XR.Interaction.Toolkit.Visuals.XRInteractorLineVisual, Unity.XR.Interaction.Toolkit");
        }

        private void CreateGroundIfMissing()
        {
            if (GameObject.Find("Environment/Ground") != null)
            {
                return;
            }

            GameObject envRoot = EnsureNamedObject("Environment");
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(envRoot.transform, false);
            ground.transform.position = new Vector3(3f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(1.35f, 1f, 1.05f);

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(0.16f, 0.33f, 0.16f));
            }

            GameObject lawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lawn.name = "LawnPad";
            lawn.transform.SetParent(envRoot.transform, false);
            lawn.transform.position = new Vector3(2.9f, -0.025f, 0f);
            lawn.transform.localScale = new Vector3(13.4f, 0.03f, 8.6f);
            Renderer lawnRenderer = lawn.GetComponent<Renderer>();
            if (lawnRenderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(lawnRenderer, new Color(0.25f, 0.45f, 0.26f));
            }

            Collider lawnCol = lawn.GetComponent<Collider>();
            if (lawnCol != null)
            {
                lawnCol.enabled = false;
            }
        }

        private void CreateSafetyFloorIfMissing()
        {
            if (GameObject.Find("Environment/SafetyFloor") != null)
            {
                return;
            }

            GameObject envRoot = EnsureNamedObject("Environment");
            GameObject safetyFloor = new GameObject("SafetyFloor", typeof(BoxCollider));
            safetyFloor.transform.SetParent(envRoot.transform, false);
            safetyFloor.transform.position = new Vector3(2.8f, -0.7f, 0f);
            safetyFloor.transform.localScale = new Vector3(520f, 1f, 520f);

            BoxCollider box = safetyFloor.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.size = Vector3.one;
                box.center = Vector3.zero;
            }
        }

        private void CreateHorizonGroundIfMissing()
        {
            if (GameObject.Find("Environment/HorizonGround") != null)
            {
                return;
            }

            GameObject envRoot = EnsureNamedObject("Environment");
            GameObject horizon = GameObject.CreatePrimitive(PrimitiveType.Plane);
            horizon.name = "HorizonGround";
            horizon.transform.SetParent(envRoot.transform, false);
            horizon.transform.position = new Vector3(2.8f, -0.06f, 0f);
            horizon.transform.localScale = new Vector3(40f, 1f, 40f);

            Renderer horizonRenderer = horizon.GetComponent<Renderer>();
            if (horizonRenderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(horizonRenderer, new Color(0.22f, 0.38f, 0.25f));
            }
        }

        private void CreateBaseIfMissing()
        {
            if (GameObject.Find("Environment/HouseBase") != null)
            {
                return;
            }

            GameObject envRoot = EnsureNamedObject("Environment");
            GameObject baseObj = new GameObject("HouseBase");
            baseObj.name = "HouseBase";
            baseObj.transform.SetParent(envRoot.transform, false);
            baseObj.transform.position = new Vector3(-1.9f, 0f, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(baseObj.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            body.transform.localScale = new Vector3(1.2f, 1f, 8.2f);
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(bodyRenderer, new Color(0.47f, 0.32f, 0.19f));
            }

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(baseObj.transform, false);
            roof.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            roof.transform.localScale = new Vector3(1.36f, 0.28f, 8.5f);
            Renderer roofRenderer = roof.GetComponent<Renderer>();
            if (roofRenderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(roofRenderer, new Color(0.36f, 0.18f, 0.16f));
            }

            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(baseObj.transform, false);
            door.transform.localPosition = new Vector3(0.64f, 0.45f, 0f);
            door.transform.localScale = new Vector3(0.08f, 0.72f, 1.2f);
            Renderer doorRenderer = door.GetComponent<Renderer>();
            if (doorRenderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(doorRenderer, new Color(0.22f, 0.11f, 0.08f));
            }
        }

        private void EnsureBaseSolidBody()
        {
            Transform baseRoot = GameObject.Find("Environment/HouseBase")?.transform;
            if (baseRoot == null)
            {
                return;
            }

            Rigidbody rb = baseRoot.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = baseRoot.gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            Transform[] children = baseRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == baseRoot)
                {
                    continue;
                }

                Collider collider = child.GetComponent<Collider>();
                if (collider == null)
                {
                    // Keep primitives solid if collider got stripped in scene edits.
                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        BoxCollider box = child.gameObject.AddComponent<BoxCollider>();
                        box.isTrigger = false;
                    }
                }
                else
                {
                    collider.isTrigger = false;
                    collider.enabled = true;
                }
            }
        }

        private void EnsureMainCameraPosition()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            if (cam.transform.position.z < -8f)
            {
                cam.transform.position = new Vector3(-3.3f, 1.65f, 0f);
                cam.transform.rotation = Quaternion.Euler(12f, 78f, 0f);
            }
        }

        private static GameObject EnsureNamedObject(string name)
        {
            GameObject found = GameObject.Find(name);
            if (found != null)
            {
                return found;
            }

            return new GameObject(name);
        }

        private static GameObject EnsureChildObject(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject childObj = new GameObject(childName);
            childObj.transform.SetParent(parent, false);
            return childObj;
        }

        private static T EnsureManager<T>(GameObject parent, string name) where T : Component
        {
            T existing = UnityEngine.Object.FindFirstObjectByType<T>();
            if (existing != null)
            {
                return existing;
            }

            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            return obj.AddComponent<T>();
        }

        private static T EnsureComponentOnRoot<T>(GameObject root) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            T existing = root.GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }

            return root.AddComponent<T>();
        }

        private static UnityEngine.Object FindAnyLoadedObject(Type type)
        {
            UnityEngine.Object[] found = UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None);
            return found != null && found.Length > 0 ? found[0] : null;
        }

        private static void TrySetProperty(Component target, string propertyName, object value)
        {
            if (target == null || value == null)
            {
                return;
            }

            var property = target.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, value);
            }
        }

        private static void AddIfTypeExists(GameObject obj, string assemblyTypeName)
        {
            Type type = Type.GetType(assemblyTypeName);
            if (type != null && obj.GetComponent(type) == null)
            {
                obj.AddComponent(type);
            }
        }

        private static void ConfigureUiInteractorSupport(Type interactorType)
        {
            if (interactorType == null)
            {
                return;
            }

            UnityEngine.Object[] interactors = UnityEngine.Object.FindObjectsByType(interactorType, FindObjectsSortMode.None);
            for (int i = 0; i < interactors.Length; i++)
            {
                if (interactors[i] is not Component interactor)
                {
                    continue;
                }

                TrySetProperty(interactor, "enableUIInteraction", true);
                TrySetProperty(interactor, "blockUIOnInteractableSelection", false);
                TrySetProperty(interactor, "maxRaycastDistance", 25f);
            }
        }

        private static void DisableStandaloneMainCameraRuntimeSafe()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Transform current = cam.transform;
            while (current != null)
            {
                Type xrOriginType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
                if (xrOriginType != null && current.GetComponent(xrOriginType) != null)
                {
                    return;
                }

                current = current.parent;
            }

            cam.gameObject.SetActive(false);
        }

        private static void ApplyComfortSpawnTransform(Transform xrRoot)
        {
            if (xrRoot == null)
            {
                return;
            }

            xrRoot.position = DefaultComfortSpawnPosition;
            xrRoot.rotation = Quaternion.Euler(DefaultComfortSpawnEuler);
        }

        private static void RepairAllErrorShaderRenderers()
        {
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                Material material = renderer.sharedMaterial;
                Shader shader = material != null ? material.shader : null;
                string shaderName = shader != null ? shader.name : string.Empty;

                if (shader == null ||
                    !shader.isSupported ||
                    shaderName.Contains("Hidden/InternalErrorShader", StringComparison.OrdinalIgnoreCase) ||
                    shaderName.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    RuntimeVisualMaterialUtility.EnsureRendererHasValidMaterial(renderer);
                    Debug.LogWarning($"PVZ3D: Repaired error-shader renderer '{renderer.name}'.");
                }
            }
        }

        private static bool TryInstantiateRuntimePrefab(string resourcesPath, Transform parent, out GameObject instance)
        {
            instance = null;
            GameObject prefab = UnityEngine.Resources.Load<GameObject>(resourcesPath);
            if (prefab == null)
            {
                return false;
            }

            instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            return true;
        }

        private static void EnsureSingleAudioListener()
        {
            AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners == null || listeners.Length <= 1)
            {
                return;
            }

            Type xrOriginType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
            AudioListener preferred = null;

            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener candidate = listeners[i];
                if (candidate == null || !candidate.enabled)
                {
                    continue;
                }

                if (xrOriginType != null)
                {
                    Transform t = candidate.transform;
                    while (t != null)
                    {
                        if (t.GetComponent(xrOriginType) != null)
                        {
                            preferred = candidate;
                            break;
                        }
                        t = t.parent;
                    }
                }

                if (preferred != null)
                {
                    break;
                }
            }

            if (preferred == null)
            {
                preferred = listeners[0];
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener l = listeners[i];
                if (l != null && l != preferred && l.enabled)
                {
                    l.enabled = false;
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Authoring/Bake Static Scene (Save)")]
        private void ContextBakeStaticSceneAndSave()
        {
            if (Application.isPlaying)
            {
                return;
            }

            BakeStaticSceneForAuthoring(saveScene: true);
        }

        [ContextMenu("Authoring/Bake Static Scene (No Save)")]
        private void ContextBakeStaticSceneNoSave()
        {
            if (Application.isPlaying)
            {
                return;
            }

            BakeStaticSceneForAuthoring(saveScene: false);
        }

        public void BakeStaticSceneForAuthoring(bool saveScene)
        {
            BuildSceneHierarchy();
            EnsureXRFoundation();
            EnsureManagers();
            EnsureSingleAudioListener();

            PlantPlacementManager placement = UnityEngine.Object.FindFirstObjectByType<PlantPlacementManager>();
            placement?.EnsureDefinitionsForAuthoring();

            LawnGridManager grid = UnityEngine.Object.FindFirstObjectByType<LawnGridManager>();
            grid?.RebuildGridForAuthoring();

            PlantTraySpawner traySpawner = UnityEngine.Object.FindFirstObjectByType<PlantTraySpawner>();
            traySpawner?.BuildTrayForAuthoring();

            UIManager uiManager = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            uiManager?.BuildEditorUiPreview();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.SetDirty(gameObject);

            if (saveScene)
            {
                EditorSceneManager.SaveOpenScenes();
            }
        }

        private void EnsureEditorSimulationSupport()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Type deviceSimType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit");
            UnityEngine.Object existingDeviceSimulator = deviceSimType != null ? FindAnyLoadedObject(deviceSimType) : null;
            if (existingDeviceSimulator is Component existingDeviceComponent)
            {
                if (TryConfigureDeviceSimulator(existingDeviceComponent))
                {
                    return;
                }

                Destroy(existingDeviceComponent.gameObject);
            }

            GameObject simRoot = EnsureChildObject(EnsureNamedObject("Debug").transform, "XR Simulation");
            if (TryInstantiateEditorPrefabFromCandidates(
                    new[]
                    {
                        "Assets/Samples/XR Interaction Toolkit/XR Device Simulator/XR Device Simulator.prefab",
                        "Packages/com.unity.xr.interaction.toolkit/Samples~/XR Device Simulator/XR Device Simulator.prefab",
                    },
                    simRoot.transform,
                    out GameObject simulatorInstance))
            {
                if (deviceSimType != null)
                {
                    Component simulator = simulatorInstance.GetComponent(deviceSimType);
                    if (simulator != null)
                    {
                        TryConfigureDeviceSimulator(simulator);
                    }
                }

                Debug.Log("PVZ3D: XR Device Simulator enabled for Editor (keyboard/mouse simulation).");
                return;
            }

            if (deviceSimType != null)
            {
                GameObject fallback = new GameObject("XR Device Simulator");
                fallback.transform.SetParent(simRoot.transform, false);
                Component simulator = fallback.AddComponent(deviceSimType);
                if (!TryConfigureDeviceSimulator(simulator))
                {
                    Debug.LogWarning("PVZ3D: XR Device Simulator fallback created but input actions were not found.");
                    return;
                }

                Debug.Log("PVZ3D: XR Device Simulator fallback enabled with keyboard/mouse bindings.");
            }
        }

        private static bool IsDeviceSimulatorConfigured(Component simulator)
        {
            if (simulator == null)
            {
                return false;
            }

            object deviceAsset = GetMemberValue(simulator, "deviceSimulatorActionAsset", "DeviceSimulatorActionAsset", "m_DeviceSimulatorActionAsset");
            object controllerAsset = GetMemberValue(simulator, "controllerActionAsset", "ControllerActionAsset", "m_ControllerActionAsset");
            object handAsset = GetMemberValue(simulator, "handActionAsset", "HandActionAsset", "m_HandActionAsset");

            return deviceAsset != null && controllerAsset != null && handAsset != null;
        }

        private static bool TryConfigureDeviceSimulator(Component simulator)
        {
            if (simulator == null)
            {
                return false;
            }

            if (IsDeviceSimulatorConfigured(simulator))
            {
                return true;
            }

            Type inputActionAssetType = Type.GetType("UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");

            UnityEngine.Object deviceAsset = LoadEditorAssetFromCandidates(
                new[]
                {
                    "Assets/Samples/XR Interaction Toolkit/XR Device Simulator/XR Device Simulator Controls.inputactions",
                    "Packages/com.unity.xr.interaction.toolkit/Samples~/XR Device Simulator/XR Device Simulator Controls.inputactions",
                },
                inputActionAssetType);

            UnityEngine.Object controllerAsset = LoadEditorAssetFromCandidates(
                new[]
                {
                    "Assets/Samples/XR Interaction Toolkit/XR Device Simulator/XR Device Controller Controls.inputactions",
                    "Packages/com.unity.xr.interaction.toolkit/Samples~/XR Device Simulator/XR Device Controller Controls.inputactions",
                },
                inputActionAssetType);

            UnityEngine.Object handAsset = LoadEditorAssetFromCandidates(
                new[]
                {
                    "Assets/Samples/XR Interaction Toolkit/XR Device Simulator/XR Device Hand Controls.inputactions",
                    "Packages/com.unity.xr.interaction.toolkit/Samples~/XR Device Simulator/XR Device Hand Controls.inputactions",
                },
                inputActionAssetType);

            if (deviceAsset == null || controllerAsset == null || handAsset == null)
            {
                return false;
            }

            TrySetMemberValue(simulator, deviceAsset, "deviceSimulatorActionAsset", "DeviceSimulatorActionAsset", "m_DeviceSimulatorActionAsset");
            TrySetMemberValue(simulator, controllerAsset, "controllerActionAsset", "ControllerActionAsset", "m_ControllerActionAsset");
            TrySetMemberValue(simulator, handAsset, "handActionAsset", "HandActionAsset", "m_HandActionAsset");

            Camera cam = Camera.main;
            if (cam != null)
            {
                TrySetMemberValue(simulator, cam.transform, "cameraTransform", "CameraTransform", "m_CameraTransform");
            }

            return IsDeviceSimulatorConfigured(simulator);
        }

        private static UnityEngine.Object LoadEditorAssetFromCandidates(string[] assetPaths, Type assetType)
        {
            for (int i = 0; i < assetPaths.Length; i++)
            {
                UnityEngine.Object loaded = AssetDatabase.LoadAssetAtPath(assetPaths[i], assetType ?? typeof(UnityEngine.Object));
                if (loaded != null)
                {
                    return loaded;
                }
            }

            return null;
        }

        private static object GetMemberValue(object target, params string[] memberNames)
        {
            Type t = target.GetType();
            for (int i = 0; i < memberNames.Length; i++)
            {
                string memberName = memberNames[i];
                PropertyInfo property = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    return property.GetValue(target);
                }

                FieldInfo field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.GetValue(target);
                }
            }

            return null;
        }

        private static bool TrySetMemberValue(object target, object value, params string[] memberNames)
        {
            if (target == null || value == null)
            {
                return false;
            }

            Type t = target.GetType();
            Type valueType = value.GetType();
            for (int i = 0; i < memberNames.Length; i++)
            {
                string memberName = memberNames[i];
                PropertyInfo property = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.CanWrite && property.PropertyType.IsAssignableFrom(valueType))
                {
                    property.SetValue(target, value);
                    return true;
                }

                FieldInfo field = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType.IsAssignableFrom(valueType))
                {
                    field.SetValue(target, value);
                    return true;
                }
            }

            return false;
        }

        private static bool TryInstantiateEditorPrefabFromCandidates(string[] assetPaths, Transform parent, out GameObject instance)
        {
            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (TryInstantiateEditorPrefab(assetPaths[i], parent, out instance))
                {
                    return true;
                }
            }

            instance = null;
            return false;
        }

        private static bool TryInstantiateEditorPrefab(string assetPath, Transform parent, out GameObject instance)
        {
            instance = null;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return false;
            }

            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                return false;
            }

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            return true;
        }

        private static void DisableStandaloneMainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Transform current = cam.transform;
            while (current != null)
            {
                Type xrOriginType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
                if (xrOriginType != null && current.GetComponent(xrOriginType) != null)
                {
                    return;
                }

                current = current.parent;
            }

            cam.gameObject.SetActive(false);
        }

#endif
        private static bool HasDescendantNamed(Transform root, string name)
        {
            if (root == null)
            {
                return false;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
