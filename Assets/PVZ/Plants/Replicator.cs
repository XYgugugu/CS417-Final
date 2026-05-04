using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Plants
{
    public class Replicator : MonoBehaviour
    {
        private Collider replicatorCollider;
        private XRGrabInteractable interactable;

        private void Awake()
        {
            replicatorCollider = GetComponent<Collider>();
            interactable = GetComponent<XRGrabInteractable>();
            interactable.selectExited.AddListener(OnSelectExited);
        }

        private void OnDestroy()
        {
            interactable.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            PlantBase plant = FindCollidingPlantedPlant();
            if (plant == null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(plant.GetType().Name);
            if (prefab == null)
            {
                return;
            }

            Instantiate(prefab, transform.position, transform.rotation);
            Destroy(gameObject);
        }

        private PlantBase FindCollidingPlantedPlant()
        {
            Bounds bounds = replicatorCollider.bounds;
            Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation);

            foreach (Collider hit in hits)
            {
                if (!hit.CompareTag("Plant"))
                {
                    continue;
                }

                PlantBase plant = hit.GetComponent<PlantBase>();
                if (plant != null && plant.IsPlaced)
                {
                    return plant;
                }
            }

            return null;
        }
    }
}
