#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Grid;
using PVZ3D.Plants;
using PVZ3D.Resources;
using PVZ3D.UI;
using PVZ3D.Waves;
using PVZ3D.Zombies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PVZ3D.EditorTools
{
    public static class PVZBatchPlaymodeSmoke
    {
        private static readonly List<string> Failures = new List<string>();
        private static readonly List<string> RuntimeErrors = new List<string>();

        private static bool running;
        private static bool requestedStop;
        private static int step;
        private static double stepStartTime;
        private static int exitCode;

        [MenuItem("PVZ3D/QA/Run Batch Playmode Smoke")]
        public static void Run()
        {
            if (running)
            {
                Debug.LogWarning("PVZ3D Smoke: already running.");
                return;
            }

            running = true;
            requestedStop = false;
            exitCode = 1;
            step = 0;
            Failures.Clear();
            RuntimeErrors.Clear();

            string scenePath = "Assets/Scenes/SampleScene.unity";
            Scene opened = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!opened.IsValid())
            {
                Failures.Add($"Failed to open scene: {scenePath}");
                FinishAndExit(1);
                return;
            }

            Application.logMessageReceived += OnLogMessage;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;

            Debug.Log("PVZ3D Smoke: entering Play Mode...");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!running)
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                step = 0;
                stepStartTime = EditorApplication.timeSinceStartup;
                Debug.Log("PVZ3D Smoke: Play Mode entered.");
            }
            else if (state == PlayModeStateChange.EnteredEditMode && requestedStop)
            {
                FinishAndExit(exitCode);
            }
        }

        private static void OnEditorUpdate()
        {
            if (!running || !EditorApplication.isPlaying)
            {
                return;
            }

            try
            {
                TickStateMachine();
            }
            catch (Exception ex)
            {
                Failures.Add($"Unhandled exception in smoke test: {ex}");
                RequestStop(1);
            }
        }

        private static void TickStateMachine()
        {
            if (TimedOut(20))
            {
                Failures.Add($"Step {step} timeout.");
                RequestStop(1);
                return;
            }

            GameManager gm = GameManager.Instance;
            ResourceManager rm = ResourceManager.Instance;
            PlantPlacementManager ppm = PlantPlacementManager.Instance;
            LawnGridManager grid = LawnGridManager.Instance;
            SunSpawner sunSpawner = SunSpawner.Instance;
            ZombieSpawner zombieSpawner = ZombieSpawner.Instance;
            UIManager ui = UIManager.Instance;

            switch (step)
            {
                case 0:
                    if (gm == null || rm == null || ppm == null || grid == null || sunSpawner == null || zombieSpawner == null || ui == null)
                    {
                        return;
                    }
                    PassStep("Managers ready.");
                    break;

                case 1:
                    if (gm.State.Phase != GamePhase.Menu)
                    {
                        Failures.Add($"Expected Menu phase, got {gm.State.Phase}.");
                    }
                    MainMenuController menu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
                    if (menu == null || !menu.gameObject.activeInHierarchy)
                    {
                        Failures.Add("Main menu not visible in menu phase.");
                    }
                    PassStep("Menu visible.");
                    break;

                case 2:
                    gm.StartMatch();
                    PassStep("StartMatch invoked.");
                    break;

                case 3:
                    if (gm.State.Phase != GamePhase.Prep && gm.State.Phase != GamePhase.Battle)
                    {
                        return;
                    }
                    PassStep($"Match phase after start: {gm.State.Phase}.");
                    break;

                case 4:
                    ppm.SelectPlantByIndex(0);
                    bool placedLane = ppm.TryPlaceSelectedInLane(0);
                    if (!placedLane)
                    {
                        Failures.Add("Failed to place first plant in lane 0.");
                    }

                    GridCell firstCell = grid.GetCell(0, 0);
                    bool placedDuplicate = ppm.TryPlaceSelectedAt(firstCell);
                    if (placedDuplicate)
                    {
                        Failures.Add("Occupied cell allowed duplicate placement.");
                    }
                    PassStep("Plant placement checks done.");
                    break;

                case 5:
                    int sunBefore = rm.CurrentSun;
                    Vector3 sunPos = grid.GetCellPosition(0, 0) + Vector3.up * 1.2f;
                    SunPickup sun = SunSpawner.SpawnSunAt(sunPos, 25);
                    if (sun == null)
                    {
                        Failures.Add("Failed to spawn sun pickup.");
                        PassStep("Sun spawn failed.");
                        break;
                    }

                    sun.Collect();
                    if (rm.CurrentSun <= sunBefore)
                    {
                        Failures.Add("Sun collect did not increase sun resource.");
                    }
                    PassStep("Sun collection check done.");
                    break;

                case 6:
                    int aliveBefore = gm.State.AliveZombies;
                    ZombieBase zombie = zombieSpawner.SpawnZombie(0, false);
                    if (zombie == null)
                    {
                        Failures.Add("Failed to spawn zombie.");
                        PassStep("Zombie spawn failed.");
                        break;
                    }

                    zombie.TakeDamage(9999f);
                    if (gm.State.AliveZombies > aliveBefore)
                    {
                        Failures.Add("Zombie alive count did not return after kill.");
                    }
                    PassStep("Zombie spawn/kill check done.");
                    break;

                case 7:
                    gm.PauseMatch();
                    if (gm.State.Phase != GamePhase.Paused)
                    {
                        Failures.Add("PauseMatch did not set phase to Paused.");
                    }

                    if (Mathf.Abs(Time.timeScale) > 0.001f)
                    {
                        Failures.Add("PauseMatch did not set Time.timeScale to 0.");
                    }

                    gm.ResumePausedMatch();
                    if (gm.State.Phase != GamePhase.Prep && gm.State.Phase != GamePhase.Battle)
                    {
                        Failures.Add($"ResumePausedMatch returned wrong phase: {gm.State.Phase}");
                    }

                    if (Mathf.Abs(Time.timeScale - 1f) > 0.001f)
                    {
                        Failures.Add("ResumePausedMatch did not restore Time.timeScale to 1.");
                    }
                    PassStep("Pause/resume checks done.");
                    break;

                case 8:
                    gm.TriggerWin();
                    if (gm.State.Phase != GamePhase.Win)
                    {
                        Failures.Add("TriggerWin did not enter Win phase.");
                    }
                    PassStep("Win path check done.");
                    break;

                case 9:
                    gm.RestartMatch();
                    if (gm.State.Phase != GamePhase.Prep && gm.State.Phase != GamePhase.Battle)
                    {
                        Failures.Add("RestartMatch did not restart to prep/battle.");
                    }
                    if (gm.State.BaseHealth != gm.BaseMaxHealth)
                    {
                        Failures.Add("RestartMatch did not reset base health.");
                    }
                    PassStep("Restart check done.");
                    break;

                case 10:
                    gm.TriggerLose();
                    if (gm.State.Phase != GamePhase.Lose)
                    {
                        Failures.Add("TriggerLose did not enter Lose phase.");
                    }
                    PassStep("Lose path check done.");
                    break;

                case 11:
                    gm.ReturnToMenu();
                    if (gm.State.Phase != GamePhase.Menu)
                    {
                        Failures.Add("ReturnToMenu did not enter Menu phase.");
                    }
                    PassStep("Return menu check done.");
                    break;

                case 12:
                    if (RuntimeErrors.Count > 0)
                    {
                        Failures.Add($"Runtime had {RuntimeErrors.Count} error/exception logs.");
                    }

                    RequestStop(Failures.Count == 0 ? 0 : 1);
                    break;
            }
        }

        private static void PassStep(string message)
        {
            Debug.Log($"PVZ3D Smoke: Step {step} ok - {message}");
            step++;
            stepStartTime = EditorApplication.timeSinceStartup;
        }

        private static bool TimedOut(double seconds)
        {
            return EditorApplication.timeSinceStartup - stepStartTime > seconds;
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!running)
            {
                return;
            }

            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
            {
                RuntimeErrors.Add($"{type}: {condition}\n{stackTrace}");
            }
        }

        private static void RequestStop(int code)
        {
            if (requestedStop)
            {
                return;
            }

            exitCode = code;
            requestedStop = true;
            Debug.Log($"PVZ3D Smoke: stopping play mode. exitCode={code}");
            EditorApplication.isPlaying = false;
        }

        private static void FinishAndExit(int code)
        {
            Application.logMessageReceived -= OnLogMessage;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            running = false;

            if (Failures.Count > 0)
            {
                Debug.LogError("PVZ3D Smoke: FAILURES:\n- " + string.Join("\n- ", Failures));
            }
            else
            {
                Debug.Log("PVZ3D Smoke: all checks passed.");
            }

            if (RuntimeErrors.Count > 0)
            {
                Debug.LogError("PVZ3D Smoke: runtime errors captured:\n- " + string.Join("\n- ", RuntimeErrors));
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
#endif
