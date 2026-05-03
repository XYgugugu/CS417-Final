using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

public class PlantSocket : XRSocketInteractor
{
    public GameObject plant;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (args.interactableObject.transform.CompareTag("plant"))
        {
            plant = args.interactableObject.transform.gameObject;
            plant.SetActive(true); // make sure it's visible
            Debug.Log("Plant set: " + plant.name);
        }
    }
}