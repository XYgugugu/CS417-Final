using UnityEngine;

namespace PVZ3D.Zombies
{
    public class PathFollow : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 0.5f;
        [SerializeField] public float destination = 2f;

        // Fake Walk Motion
        private float bobAmount = 0.05f;
        private float bobSpeed = 5f;
        private Vector3 startLocalPosition;

        private bool reachedDestination = false;
        private bool isAttacking = false;

        void Start()
        {
            startLocalPosition = transform.localPosition;
        }
        void Update()
        {
            if (reachedDestination || isAttacking) return;

            transform.position += Vector3.forward * moveSpeed * Time.deltaTime;

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.localPosition = new Vector3(
            transform.localPosition.x,
            startLocalPosition.y + bob,
            transform.localPosition.z
        );


            if (transform.position.z >= destination)
            {
                reachedDestination = true;
                Destroy(gameObject);
            }
        }

        public void StopMoving()
        {
            isAttacking = true;
        }

        public void ResumeMoving()
        {
            isAttacking = false;
        }

        public bool IsStopped()
        {
            return isAttacking;
        }
    }

}