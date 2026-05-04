using UnityEngine;
using TMPro;
using System.Collections.Generic;
using PVZ3D.Core;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using PVZ3D.UI;

namespace PVZ3D.Levels
{
    /// <summary>
    /// Manages randomized route portal behavior after level clear.
    /// - Hides all portals at start.
    /// - On level win, randomizes which portal triggers which action.
    /// - Actions: Scene 1, Scene 2, or Victory UI.
    /// - Optionally updates portal labels to show the randomized action.
    /// </summary>
    public class RandomizedRoutePortalSet : MonoBehaviour
    {
        [Header("Portal Setup")]
        [SerializeField] private GameObject[] routePortals; // Exactly 3 portals
        [SerializeField] private string[] targetSceneNames; // Exactly 2 scene names

        [Header("Optional Labels")]
        [SerializeField] private TextMeshProUGUI[] portalLabels; // Optional: 3 labels matching portal order

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private HUDController hudController;
        [SerializeField] private GameObject gameplayHudRoot; // The gameplay HUD to hide on win

        private bool hasRandomizedPortals = false;

        // Internal action representation
        private enum PortalAction { Scene1, Scene2, Victory }
        private PortalAction[] portalActions = new PortalAction[3]; // Which action each portal has

        private void Start()
        {
            // Validate setup
            if (routePortals == null || routePortals.Length != 3)
            {
                Debug.LogError("RandomizedRoutePortalSet: Expected exactly 3 route portals.");
                return;
            }

            if (targetSceneNames == null || targetSceneNames.Length != 2)
            {
                Debug.LogError("RandomizedRoutePortalSet: Expected exactly 2 target scene names.");
                return;
            }

            // Hide all portals at start
            HideAllPortals();

            // Resolve GameManager if not assigned
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            // Resolve HUDController if not assigned
            if (hudController == null)
            {
                hudController = FindObjectOfType<HUDController>();
            }

            // Subscribe to game end event
            if (gameManager != null)
            {
                gameManager.OnGameEnded += OnGameEnded;
            }
            else
            {
                Debug.LogWarning("RandomizedRoutePortalSet: GameManager not found or assigned.");
            }
        }

        private void OnGameEnded(bool didWin)
        {
            if (!didWin) return;
            if (hasRandomizedPortals) return;

            hasRandomizedPortals = true;

            // Hide gameplay HUD on win
            if (gameplayHudRoot != null)
            {
                gameplayHudRoot.SetActive(false);
                Debug.Log("RandomizedRoutePortalSet: Gameplay HUD hidden.");
            }

            Debug.Log("RandomizedRoutePortalSet: Level cleared! Randomizing route portals.");
            RandomizePortals();
            ShowAllPortals();
        }

        private void RandomizePortals()
        {
            // Create list of actions and shuffle
            List<PortalAction> actions = new List<PortalAction>
            {
                PortalAction.Scene1,
                PortalAction.Scene2,
                PortalAction.Victory
            };

            // Fisher-Yates shuffle
            for (int i = actions.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (actions[i], actions[randomIndex]) = (actions[randomIndex], actions[i]);
            }

            // Assign randomized actions to portals
            for (int i = 0; i < 3; i++)
            {
                portalActions[i] = actions[i];
                ConfigurePortal(i, actions[i]);
            }

            Debug.Log($"RandomizedRoutePortalSet: Portal assignments: [0]={portalActions[0]}, [1]={portalActions[1]}, [2]={portalActions[2]}");
        }

        private void ConfigurePortal(int portalIndex, PortalAction action)
        {
            GameObject portal = routePortals[portalIndex];
            if (portal == null) return;

            // Remove existing XRSimpleInteractable listeners
            XRSimpleInteractable interactable = portal.GetComponent<XRSimpleInteractable>();
            if (interactable != null)
            {
                interactable.selectEntered.RemoveAllListeners();
            }

            // Add listener for the new action
            if (interactable != null)
            {
                int index = portalIndex; // Capture for closure
                interactable.selectEntered.AddListener((args) => ExecutePortalAction(index));
            }

            // Update label if available
            if (portalLabels != null && portalLabels.Length > portalIndex && portalLabels[portalIndex] != null)
            {
                string labelText = GetActionLabel(action);
                portalLabels[portalIndex].text = labelText;
            }
        }

        private void ExecutePortalAction(int portalIndex)
        {
            if (portalIndex < 0 || portalIndex >= 3) return;

            PortalAction action = portalActions[portalIndex];

            switch (action)
            {
                case PortalAction.Scene1:
                    LoadScene(targetSceneNames[0], portalIndex);
                    break;
                case PortalAction.Scene2:
                    LoadScene(targetSceneNames[1], portalIndex);
                    break;
                case PortalAction.Victory:
                    ShowVictory(portalIndex);
                    break;
            }
        }

        private void LoadScene(string sceneName, int portalIndex)
        {
            Debug.Log($"RandomizedRoutePortalSet: Portal {portalIndex} loading scene '{sceneName}'.");

            // Try using LevelTransition if available
            LevelTransition levelTransition = FindObjectOfType<LevelTransition>();
            if (levelTransition != null)
            {
                levelTransition.FadeAndLoadScene(sceneName);
            }
            else
            {
                // Fallback to direct scene load
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
        }

        private void ShowVictory(int portalIndex)
        {
            Debug.Log($"RandomizedRoutePortalSet: Portal {portalIndex} triggering victory UI.");

            if (hudController != null)
            {
                hudController.ShowVictoryUI();
            }
            else
            {
                Debug.LogWarning("RandomizedRoutePortalSet: HUDController not found. Victory UI not shown.");
            }
        }

        private string GetActionLabel(PortalAction action)
        {
            switch (action)
            {
                case PortalAction.Scene1:
                    return targetSceneNames[0];
                case PortalAction.Scene2:
                    return targetSceneNames[1];
                case PortalAction.Victory:
                    return "Victory Screen";
                default:
                    return "Unknown";
            }
        }

        private void HideAllPortals()
        {
            foreach (GameObject portal in routePortals)
            {
                if (portal != null)
                {
                    portal.SetActive(false);
                }
            }

            Debug.Log("RandomizedRoutePortalSet: All portals hidden at start.");
        }

        private void ShowAllPortals()
        {
            foreach (GameObject portal in routePortals)
            {
                if (portal != null)
                {
                    portal.SetActive(true);
                }
            }

            Debug.Log("RandomizedRoutePortalSet: All portals shown after level clear.");
        }

        // [ContextMenu("Test Show Randomized Portals")]
        // private void TestShowRandomizedPortals()
        // {
        //     if (hasRandomizedPortals) return;

        //     hasRandomizedPortals = true;

        //     Debug.Log("RandomizedRoutePortalSet: Test clear triggered.");
        //     RandomizePortals();
        //     ShowAllPortals();
        // }

        [ContextMenu("Test Show Randomized Portals")]
        private void TestShowRandomizedPortals()
        {
            OnGameEnded(true);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnGameEnded -= OnGameEnded;
            }
        }
    }
}
