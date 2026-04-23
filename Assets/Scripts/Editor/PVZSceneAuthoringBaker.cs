#if UNITY_EDITOR
using System.IO;
using PVZ3D.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PVZ3D.EditorTools
{
    public static class PVZSceneAuthoringBaker
    {
        private const string MainScenePath = "Assets/Scenes/SampleScene.unity";
        private const string FallbackMaterialPath = "Assets/Materials/PVZ_EditorFallback.mat";
        private static bool sceneOpenHookRegistered;
        private static bool autoBakeInProgress;

        [InitializeOnLoadMethod]
        private static void RegisterSceneOpenHook()
        {
            if (sceneOpenHookRegistered)
            {
                return;
            }

            sceneOpenHookRegistered = true;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            if (Application.isPlaying)
            {
                return;
            }

            // Auto-heal any transient error-shader references after exiting Play Mode.
            RepairErrorMaterialsInOpenScene();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (autoBakeInProgress || Application.isPlaying)
            {
                return;
            }

            if (!scene.IsValid() || scene.path != MainScenePath)
            {
                return;
            }

            RepairErrorMaterialsInOpenScene();

            if (IsMainSceneAlreadyAuthored())
            {
                return;
            }

            // Delay to keep scene loading stable before we touch hierarchy.
            EditorApplication.delayCall += () =>
            {
                if (autoBakeInProgress || Application.isPlaying)
                {
                    return;
                }

                if (SceneManager.GetActiveScene().path != MainScenePath)
                {
                    return;
                }

                autoBakeInProgress = true;
                try
                {
                    Bake(save: true);
                    Debug.Log("PVZ3D: Auto-baked SampleScene into a persistent authored scene.");
                }
                finally
                {
                    autoBakeInProgress = false;
                }
            };
        }

        [MenuItem("PVZ3D/Authoring/Bake Scene Static (Save)")]
        public static void BakeSceneStaticAndSave()
        {
            Bake(save: true);
        }

        [MenuItem("PVZ3D/Authoring/Bake Scene Static (No Save)")]
        public static void BakeSceneStaticNoSave()
        {
            Bake(save: false);
        }

        [MenuItem("PVZ3D/Authoring/Repair Error Materials In Open Scene")]
        public static void RepairErrorMaterialsInOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("PVZ3D: No valid active scene to repair.");
                return;
            }

            Material fallback = GetFallbackMaterial();
            if (fallback == null)
            {
                Debug.LogError("PVZ3D: Could not resolve fallback material from active render pipeline.");
                return;
            }

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int fixedCount = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material shared = renderer.sharedMaterial;
                if (shared == null || IsBadShader(shared.shader))
                {
                    renderer.sharedMaterial = fallback;
                    fixedCount++;
                    EditorUtility.SetDirty(renderer);
                }
            }

            if (fixedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"PVZ3D: Repaired {fixedCount} renderer(s) using fallback material '{fallback.name}'.");
            }
        }

        private static void Bake(bool save)
        {
            Scene opened = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            if (!opened.IsValid())
            {
                Debug.LogError($"PVZ3D: Failed to open scene for baking: {MainScenePath}");
                return;
            }

            PVZSceneBootstrap bootstrap = Object.FindFirstObjectByType<PVZSceneBootstrap>();
            if (bootstrap == null)
            {
                GameObject bootstrapObj = GameObject.Find("PVZSceneBootstrap");
                if (bootstrapObj == null)
                {
                    bootstrapObj = new GameObject("PVZSceneBootstrap");
                }

                bootstrap = bootstrapObj.GetComponent<PVZSceneBootstrap>();
                if (bootstrap == null)
                {
                    bootstrap = bootstrapObj.AddComponent<PVZSceneBootstrap>();
                }
            }

            bootstrap.BakeStaticSceneForAuthoring(save);
            EditorGUIUtility.PingObject(bootstrap.gameObject);
            Debug.Log(save
                ? "PVZ3D: Static scene baked and saved."
                : "PVZ3D: Static scene baked (not saved yet).");
        }

        private static bool IsMainSceneAlreadyAuthored()
        {
            return GameObject.Find("PVZSceneBootstrap") != null
                   && GameObject.Find("Managers") != null
                   && GameObject.Find("XR Rig") != null
                   && GameObject.Find("Environment") != null
                   && GameObject.Find("Grid") != null
                   && GameObject.Find("Spawners") != null
                   && GameObject.Find("UI") != null
                   && GameObject.Find("Runtime") != null;
        }

        private static Material GetFallbackMaterial()
        {
            RenderPipelineAsset activeRp = GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline
                : GraphicsSettings.defaultRenderPipeline;
            if (activeRp != null && activeRp.defaultMaterial != null)
            {
                return activeRp.defaultMaterial;
            }

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
            if (existing != null && existing.shader != null && existing.shader.isSupported)
            {
                return existing;
            }

            string[] shaderCandidates =
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
                "Standard",
            };

            for (int i = 0; i < shaderCandidates.Length; i++)
            {
                Shader shader = Shader.Find(shaderCandidates[i]);
                if (shader != null && shader.isSupported)
                {
                    Material created = new Material(shader)
                    {
                        name = "PVZ_EditorFallback",
                    };

                    string dir = Path.GetDirectoryName(FallbackMaterialPath)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                    {
                        if (dir == "Assets/Materials" && AssetDatabase.IsValidFolder("Assets"))
                        {
                            AssetDatabase.CreateFolder("Assets", "Materials");
                        }
                        else
                        {
                            Directory.CreateDirectory(dir);
                            AssetDatabase.Refresh();
                        }
                    }

                    AssetDatabase.CreateAsset(created, FallbackMaterialPath);
                    AssetDatabase.SaveAssets();
                    return created;
                }
            }

            return null;
        }

        private static bool IsBadShader(Shader shader)
        {
            if (shader == null)
            {
                return true;
            }

            string shaderName = shader.name ?? string.Empty;
            return !shader.isSupported ||
                   shaderName.Contains("Hidden/InternalErrorShader") ||
                   shaderName.Contains("Error");
        }
    }
}
#endif
