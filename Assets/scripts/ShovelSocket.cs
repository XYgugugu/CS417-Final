using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

public class ShovelSocket : XRSocketInteractor
{
    public XRSocketInteractor plantSocket; // drag plant socket here in Inspector

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (args.interactableObject.transform.CompareTag("Shovel"))
        {
            if (plantSocket.hasSelection)
            {
                GameObject plant = plantSocket.GetOldestInteractableSelected().transform.gameObject;
                plant.SetActive(false);
                Debug.Log("Plant removed: " + plant.name);
            }
        }
    }
}