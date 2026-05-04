using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using PVZ3D.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace PVZ3D.Levels
{
    /// <summary>
    /// Route portal that triggers the victory/endgame UI when selected.
    /// Used in randomized portal sets where one portal always leads to the victory screen.
    /// </summary>
    public class VictoryRoutePortal : MonoBehaviour
    {
        [SerializeField] private HUDController hudController;
        private XRSimpleInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();

            if (interactable != null)
            {
                interactable.selectEntered.AddListener(OnSelected);
            }
            else
            {
                Debug.LogWarning("VictoryRoutePortal needs an XR Simple Interactable on " + gameObject.name);
            }

            if (hudController == null)
            {
                hudController = FindObjectOfType<HUDController>();
            }
        }

        private void OnDestroy()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelected);
            }
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            ShowVictory();
        }

        public void ShowVictory()
        {
            if (hudController != null)
            {
                hudController.ShowVictoryUI();
                Debug.Log("VictoryRoutePortal: Victory UI triggered.");
            }
            else
            {
                Debug.LogWarning("VictoryRoutePortal: HUDController not found or assigned.");
            }
        }
    }
}
