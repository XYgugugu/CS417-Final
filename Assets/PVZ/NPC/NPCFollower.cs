using PVZ3D.Resource;

using UnityEngine;
using UnityEngine.AI;

namespace PVZ3D.NPC
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCFollower : MonoBehaviour
    {
        [Header("Target")]
        public Transform player;

        [Header("Formation")]
        public float baseFollowDistance = 2.0f;
        public float spacing = 1.5f;
        public float repathInterval = 0.15f;

        [Header("Movement")]
        public float slotSampleRadius = 2.0f;
        public float faceTurnSpeed = 8f;

        private NavMeshAgent agent;
        private float repathTimer;

        private int followIndex = 0;
        private int totalFollowers = 1;

        public void SetFollowIndex(int index, int total)
        {
            followIndex = index;
            totalFollowers = Mathf.Max(1, total);
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            if (NPCFollowManager.Instance != null)
                NPCFollowManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (NPCFollowManager.Instance != null)
                NPCFollowManager.Instance.Unregister(this);
        }

        private void Update()
        {
            if (player == null) return;

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;

                Vector3 targetPos = GetSlotPosition();
                agent.SetDestination(targetPos);
            }

            FaceMoveDirection();
        }

        private Vector3 GetSlotPosition()
        {
            Vector3 playerPos = player.position;

            // Use player's facing on XZ plane
            Vector3 forward = player.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            // Arrange followers in rows behind the player:
            // row 0: center
            // row 1: left/right
            // row 2: left/center/right, etc.
            int row = 0;
            int posInRow = 0;
            int remaining = followIndex;

            while (true)
            {
                int rowCount = (row == 0) ? 1 : (row * 2);
                if (remaining < rowCount)
                {
                    posInRow = remaining;
                    break;
                }

                remaining -= rowCount;
                row++;
            }

            float backwardDist = baseFollowDistance + row * spacing;

            float lateralOffset;
            if (row == 0)
            {
                lateralOffset = 0f;
            }
            else
            {
                // Example row 1: -0.75, +0.75
                // row 2: -2.25, -0.75, +0.75, +2.25
                float start = -((row * 2 - 1) * spacing * 0.5f);
                lateralOffset = start + posInRow * spacing;
            }

            Vector3 desired =
                playerPos
                - forward * backwardDist
                + right * lateralOffset;

            // Snap desired point to nearest NavMesh point
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, slotSampleRadius, NavMesh.AllAreas))
                return hit.position;

            return desired;
        }

        private void FaceMoveDirection()
        {
            Vector3 velocity = agent.velocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 0.05f)
            {
                Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    faceTurnSpeed * Time.deltaTime
                );
            }
        }
    }
}