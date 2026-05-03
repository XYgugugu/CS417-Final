using UnityEngine;

public class tp : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Camera portalCamera;
    public GameObject thePlayer;

    private void OnTriggerEnter(Collider other)
    {
        thePlayer.transform.position += portalCamera.transform.position;
        Debug.Log("teleport");
    }
}
