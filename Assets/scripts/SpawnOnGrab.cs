using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SpawnOnGrab : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public Transform spawnPoint;
    private XRGrabInteractable grab;

    private Vector3 originalPos;
    private Quaternion originalRot;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void Start()
    {
        originalPos = prefabToSpawn.transform.position;
        originalRot = prefabToSpawn.transform.rotation;
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
            return;
        spawnPoint = prefabToSpawn.transform;
        GameObject newObj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        newObj.tag = this.gameObject.tag;
        Debug.Log("Spawned: " + newObj.name);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // if released into a socket, do nothing
        if (args.interactorObject is XRSocketInteractor)
            return;

        // if released by hand and not caught by socket, go back
        if (!grab.isSelected)
        {
            transform.position = new Vector3(0, -100, 0);
       
            Debug.Log("Returned to original position");
        }
    }
}