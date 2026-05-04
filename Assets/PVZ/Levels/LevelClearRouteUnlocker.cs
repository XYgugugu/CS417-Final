using UnityEngine;
using PVZ3D.Core;

namespace PVZ3D.Levels
{
    /// <summary>
    /// Manages route portal visibility based on level clear state.
    /// - Hides route portals at start.
    /// - Shows them when GameManager.OnLevelCleared fires.
    /// </summary>
    public class LevelClearRouteUnlocker : MonoBehaviour
    {
        [SerializeField] private GameObject[] routePortals;
        [SerializeField] private GameManager gameManager;

        private bool hasUnlockedPortals = false;

        private void Start()
        {
            // Hide route portals at start
            HideRoutePortals();

            // Resolve GameManager if not assigned
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            // Subscribe to level-clear event
            if (gameManager != null)
            {
                gameManager.OnLevelCleared += OnLevelCleared;
            }
            else
            {
                Debug.LogWarning("LevelClearRouteUnlocker: GameManager not found or assigned.");
            }
        }

        private void OnLevelCleared()
        {
            if (hasUnlockedPortals) return;

            hasUnlockedPortals = true;

            Debug.Log("LevelClearRouteUnlocker: Level cleared! Showing route portals.");
            ShowRoutePortals();
        }

        private void HideRoutePortals()
        {
            if (routePortals == null || routePortals.Length == 0)
            {
                Debug.LogWarning("LevelClearRouteUnlocker: No route portals assigned.");
                return;
            }

            foreach (GameObject portal in routePortals)
            {
                if (portal != null)
                {
                    portal.SetActive(false);
                }
            }

            Debug.Log("LevelClearRouteUnlocker: Route portals hidden at start.");
        }

        private void ShowRoutePortals()
        {
            if (routePortals == null || routePortals.Length == 0) return;

            foreach (GameObject portal in routePortals)
            {
                if (portal != null)
                {
                    portal.SetActive(true);
                }
            }

            Debug.Log($"LevelClearRouteUnlocker: Showing {routePortals.Length} route portal(s).");
        }

        // Testing for unlocking route portals without needing to clear the level
        [ContextMenu("Test Unlock Route Portals")]
        private void TestUnlockRoutePortals()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            if (gameManager != null)
            {
                gameManager.ClearLevel();
            }
            else
            {
                OnLevelCleared();
            }
        }


        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (gameManager != null)
            {
                gameManager.OnLevelCleared -= OnLevelCleared;
            }
        }
    }
}
