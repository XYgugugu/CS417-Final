using UnityEngine;
using PVZ3D.Core;

namespace PVZ3D.Levels
{
    /// <summary>
    /// Manages route portal visibility based on level clear state.
    /// - Hides route portals at start.
    /// - Shows them when GameManager.OnGameEnded fires with didWin=true.
    /// - Deletes HUDCanvas after level clear.
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

            // Subscribe to game end event
            if (gameManager != null)
            {
                gameManager.OnGameEnded += OnGameEnded;
            }
            else
            {
                Debug.LogWarning("LevelClearRouteUnlocker: GameManager not found or assigned.");
            }
        }

        private void OnGameEnded(bool didWin)
        {
            if (!didWin) return;
            if (hasUnlockedPortals) return;

            hasUnlockedPortals = true;

            Debug.Log("LevelClearRouteUnlocker: Level cleared! Showing route portals.");
            ShowRoutePortals();
            DeleteHUDCanvas();
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

        private void DeleteHUDCanvas()
        {
            GameObject hudCanvas = GameObject.Find("HUDCanvas");
            if (hudCanvas != null)
            {
                // Destroy(hudCanvas);
                hudCanvas.SetActive(false);
                Debug.Log("LevelClearRouteUnlocker: HUDCanvas destroyed.");
            }
            else
            {
                Debug.LogWarning("LevelClearRouteUnlocker: HUDCanvas not found in scene.");
            }
        }

        // Testing for unlocking route portals without needing to clear the level
        [ContextMenu("Test Unlock Route Portals")]
        private void TestUnlockRoutePortals()
        {
            OnGameEnded(true);
        }


        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (gameManager != null)
            {
                gameManager.OnGameEnded -= OnGameEnded;
            }
        }
    }
}
