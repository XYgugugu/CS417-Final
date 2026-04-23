using PVZ3D.Core;
using PVZ3D.Plants;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PVZ3D.Interaction
{
    public class VRControllerGameplayFallback : MonoBehaviour
    {
        [Header("Editor Keyboard Fallback")]
        [SerializeField] private bool enableInPlayerBuild;
        [SerializeField] private Key startKey = Key.Enter;
        [SerializeField] private Key restartKey = Key.R;
        [SerializeField] private Key menuKey = Key.Escape;

        private void Update()
        {
            if (!Application.isEditor && !enableInPlayerBuild)
            {
                return;
            }

            GameManager manager = GameManager.Instance;
            if (manager == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard[startKey].wasPressedThisFrame && manager.State.Phase == GamePhase.Menu)
            {
                manager.StartMatch();
                return;
            }

            if (keyboard[restartKey].wasPressedThisFrame && (manager.State.Phase == GamePhase.Win || manager.State.Phase == GamePhase.Lose))
            {
                manager.RestartMatch();
                return;
            }

            if (keyboard[menuKey].wasPressedThisFrame)
            {
                if (manager.State.Phase == GamePhase.Prep || manager.State.Phase == GamePhase.Battle)
                {
                    manager.PauseMatch();
                    UI.UIManager.Instance?.ShowMainMenuInFront();
                }
                else if (manager.State.Phase == GamePhase.Paused)
                {
                    manager.ResumePausedMatch();
                    UI.UIManager.Instance?.ShowGameplayHud();
                }
                else
                {
                    manager.ReturnToMenu();
                }
                return;
            }

            if (manager.State.Phase != GamePhase.Prep && manager.State.Phase != GamePhase.Battle)
            {
                return;
            }

            PlantPlacementManager placement = PlantPlacementManager.Instance;
            if (placement == null)
            {
                return;
            }

            if (keyboard[Key.Digit1].wasPressedThisFrame)
            {
                placement.SelectPlantByIndex(0);
            }
            else if (keyboard[Key.Digit2].wasPressedThisFrame)
            {
                placement.SelectPlantByIndex(1);
            }
            else if (keyboard[Key.Digit3].wasPressedThisFrame)
            {
                placement.SelectPlantByIndex(2);
            }

            if (keyboard[Key.Q].wasPressedThisFrame)
            {
                placement.TryPlaceSelectedInLane(0);
            }
            else if (keyboard[Key.W].wasPressedThisFrame)
            {
                placement.TryPlaceSelectedInLane(1);
            }
            else if (keyboard[Key.E].wasPressedThisFrame)
            {
                placement.TryPlaceSelectedInLane(2);
            }
            else if (keyboard[Key.A].wasPressedThisFrame)
            {
                placement.TryPlaceSelectedInLane(3);
            }
            else if (keyboard[Key.S].wasPressedThisFrame)
            {
                placement.TryPlaceSelectedInLane(4);
            }
        }
    }
}
