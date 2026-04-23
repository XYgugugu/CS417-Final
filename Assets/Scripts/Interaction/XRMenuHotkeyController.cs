using PVZ3D.Core;
using PVZ3D.UI;
using UnityEngine;
using UnityEngine.XR;

namespace PVZ3D.Interaction
{
    public class XRMenuHotkeyController : MonoBehaviour
    {
        [Header("Menu Hotkey")]
        [SerializeField] private bool enableOnDevice = true;
        [SerializeField] private bool enableInEditor = true;
        [Tooltip("Left controller menu button.")]
        [SerializeField] private bool useLeftMenuButton = true;
        [Tooltip("Fallback when menu button is unavailable on runtime.")]
        [SerializeField] private bool useLeftSecondaryFallback = true;

        private InputDevice leftDevice;
        private bool wasPressedLastFrame;

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Application.isEditor && !enableInEditor)
            {
                return;
            }

            if (!Application.isEditor && !enableOnDevice)
            {
                return;
            }

            if (!leftDevice.isValid)
            {
                leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            }

            bool isPressed = false;
            if (leftDevice.isValid)
            {
                if (useLeftMenuButton && leftDevice.TryGetFeatureValue(CommonUsages.menuButton, out bool menuHeld) && menuHeld)
                {
                    isPressed = true;
                }
                else if (useLeftSecondaryFallback && leftDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryHeld) && secondaryHeld)
                {
                    isPressed = true;
                }
            }

            if (isPressed && !wasPressedLastFrame)
            {
                TriggerMenuRecall();
            }

            wasPressedLastFrame = isPressed;
        }

        private static void TriggerMenuRecall()
        {
            GameManager manager = GameManager.Instance;
            UIManager ui = UIManager.Instance;
            if (manager == null || ui == null)
            {
                return;
            }

            GamePhase phase = manager.State.Phase;
            if (phase == GamePhase.Prep || phase == GamePhase.Battle)
            {
                manager.PauseMatch();
                ui.ShowMainMenuInFront();
                return;
            }

            if (phase == GamePhase.Paused)
            {
                manager.ResumePausedMatch();
                ui.ShowGameplayHud();
                return;
            }

            ui.ShowMainMenuInFront();
        }
    }
}
