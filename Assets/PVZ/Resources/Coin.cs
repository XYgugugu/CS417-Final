using PVZ3D.NPC;
using PVZ3D.Core;

using UnityEngine;

namespace PVZ3D.Resource
{
    public class Coin : MonoBehaviour
    {
        [Header("Coin Data")]
        public int value = 1;
        [SerializeField] private float pickupRadius = 0.45f;
        public bool IsCollected => collected;

        [HideInInspector] public bool isClaimed = false;
        [HideInInspector] public Trotter claimedByTrotter = null;

        private bool collected;
        private GameManager gameManager;

        private void Awake()
        {
            EnsurePickupSetup();
        }

        private void EnsurePickupSetup()
        {
            SphereCollider pickupCollider = null;
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                colliders[i].isTrigger = true;

                if (colliders[i].gameObject == gameObject && colliders[i] is SphereCollider sphereCollider)
                {
                    pickupCollider = sphereCollider;
                }
            }

            if (pickupCollider == null)
            {
                pickupCollider = gameObject.AddComponent<SphereCollider>();
            }

            pickupCollider.radius = pickupRadius;
            pickupCollider.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.isKinematic = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !IsPlayer(other))
            {
                return;
            }

            Collect();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collected || collision == null || !IsPlayer(collision.collider))
            {
                return;
            }

            Collect();
        }

        public bool Collect()
        {
            if (collected)
            {
                return false;
            }

            collected = true;

            GameManager manager = ResolveGameManager();
            if (manager != null)
            {
                manager.ResourceManager.EarnCoins(value);
            }

            Destroy(gameObject);
            return true;
        }

        private GameManager ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager;
        }

        private static bool IsPlayer(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                return true;
            }

            Rigidbody attachedBody = other.attachedRigidbody;
            if (attachedBody != null && attachedBody.CompareTag("Player"))
            {
                return true;
            }

            Transform current = other.transform;
            while (current != null)
            {
                if (current.CompareTag("Player"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
