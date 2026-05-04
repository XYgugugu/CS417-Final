using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PlantShovelLightChanger : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light targetLight;
    [SerializeField] private Color defaultLightColor = Color.white;

    [Header("Controller Button")]
    [SerializeField] private InputActionReference rightControllerButton;

    [Header("Interactor")]
    [SerializeField] private XRBaseInteractor handInteractor;

    private string _grabbedTag = "";

    void OnEnable()
    {
        rightControllerButton.action.Enable();
        rightControllerButton.action.performed += OnButtonPressed;

        handInteractor.selectEntered.AddListener(OnGrab);
        handInteractor.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        rightControllerButton.action.performed -= OnButtonPressed;

        handInteractor.selectEntered.RemoveListener(OnGrab);
        handInteractor.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        _grabbedTag = args.interactableObject.transform.tag;
        Debug.Log($"[LightChanger] Grabbed object with tag: {_grabbedTag}");
    }

    void OnRelease(SelectExitEventArgs args)
    {
        _grabbedTag = "";
        ResetLight();
        Debug.Log("[LightChanger] Released - Light RESET");
    }

    void OnButtonPressed(InputAction.CallbackContext ctx)
    {
        switch (_grabbedTag)
        {
            case "Plant":
                targetLight.color = Color.green;
                Debug.Log("[LightChanger] Plant - Light GREEN");
                break;
            case "shovel":
                targetLight.color = Color.red;
                Debug.Log("[LightChanger] Shovel - Light RED");
                break;
            case "Replicator":
                targetLight.color = Color.blue;
                Debug.Log("[LightChanger] Kettle - Light BLUE");
                break;
            default:
                Debug.Log("[LightChanger] Button pressed but nothing relevant grabbed");
                break;
        }
    }

    void ResetLight()
    {
        targetLight.color = defaultLightColor;
    }
}