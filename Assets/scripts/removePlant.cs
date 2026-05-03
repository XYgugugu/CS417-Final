using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SocketReplacement : MonoBehaviour
{
    public XRSocketInteractor plantSocket; // drag paired plant socket here
    public GameObject shovel;

    public void MovePlant()
    {
        if (plantSocket.hasSelection)
        {
            IXRSelectInteractable plant = plantSocket.GetOldestInteractableSelected();
            GameObject plantObj = plant.transform.gameObject;

            // release from socket first
            plantSocket.interactionManager.SelectExit(plantSocket, plant);

            // then disable
            plantObj.SetActive(false);
            shovel.SetActive(false);
        }
    }
}