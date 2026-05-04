using UnityEngine;

namespace PVZ3D.Zombies
{
    public class ZombieDeathSplitEffect : MonoBehaviour
    {
        [SerializeField] private GameObject basicZLower;
        [SerializeField] private GameObject basicZUpper;

        [SerializeField] private float minUpperUpForce = 3f;
        [SerializeField] private float maxUpperUpForce = 6f;
        [SerializeField] private float randomSideForce = 1f;
        [SerializeField] private float randomSpinForce = 5f;

        [SerializeField] private float disappearTime = 3f;

        private void OnDestroy()
        {
            PlayDeathSplit();
        }

        private void PlayDeathSplit()
        {
            GameObject lower = Instantiate(
                basicZLower,
                transform.position,
                transform.rotation
            );

            GameObject upper = Instantiate(
                basicZUpper,
                transform.position,
                transform.rotation
            );

            Rigidbody upperRb = upper.AddComponent<Rigidbody>();
            upperRb.useGravity = true;
            upperRb.isKinematic = false;

            Vector3 force = new Vector3(
                Random.Range(-randomSideForce, randomSideForce),
                Random.Range(minUpperUpForce, maxUpperUpForce),
                Random.Range(-randomSideForce, randomSideForce)
            );

            upperRb.AddForce(force, ForceMode.Impulse);
            upperRb.AddTorque(Random.insideUnitSphere * randomSpinForce, ForceMode.Impulse);

            Destroy(lower);
            Destroy(upper, disappearTime);
        }
    }
}
