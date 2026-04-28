using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RouteManager : MonoBehaviour
{
    public LevelManager levelManager;
    public RouteType routeType;

    public enum RouteType
    {
        Level2A,
        Level2B
    }

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
            Debug.LogWarning("RouteManager needs an XR Simple Interactable on " + gameObject.name);
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
        if (levelManager == null)
        {
            Debug.LogWarning("RouteManager has no LevelManager assigned.");
            return;
        }

        if (routeType == RouteType.Level2A)
        {
            levelManager.ChooseLevel2A();
        }
        else if (routeType == RouteType.Level2B)
        {
            levelManager.ChooseLevel2B();
        }
    }
}