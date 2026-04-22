using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class removePlant : MonoBehaviour
{
    public GameObject plant;
    private XRSocketInteractor socket;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }
    void OnEnable()
    {
        socket.selectEntered.AddListener(OnObjectPlaced);
    }
    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnObjectPlaced);
    }
    private void OnObjectPlaced(SelectEnterEventArgs args)
    {
        Debug.Log("selectEnter");
        Debug.Log(args.interactableObject);
        GameObject placed = args.interactableObject.transform.gameObject;
        Debug.Log(placed.name);
        Debug.Log(placed.tag);
        if (placed.CompareTag("plant")) {
            plant = placed;
            Debug.Log("placed");
        }
        if (placed.CompareTag("shovel"))
        {
            plant.SetActive(false);
            plant = null;
        }
    }
}
