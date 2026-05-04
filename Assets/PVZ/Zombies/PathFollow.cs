using UnityEngine;

namespace PVZ3D.Zombies
{
    public class PathFollow : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 0.5f;
        [SerializeField] private float destination = 2f;

        private const float BobAmount = 0.05f;
        private const float BobSpeed = 5f;

        private Vector3 startLocalPosition;

        private bool reachedDestination;

        public bool IsStopped { get; private set; }

        private void Start()
        {
            startLocalPosition = transform.localPosition;
        }

        private void Update()
        {
            if (reachedDestination || IsStopped) return;

            transform.position += Vector3.forward * moveSpeed * Time.deltaTime;

            float bob = Mathf.Sin(Time.time * BobSpeed) * BobAmount;
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                startLocalPosition.y + bob,
                transform.localPosition.z);

            if (transform.position.z >= destination)
            {
                reachedDestination = true;
                Destroy(gameObject);
            }
        }

        public void StopMoving()
        {
            IsStopped = true;
        }

        public void ResumeMoving()
        {
            IsStopped = false;
        }
    }
}
