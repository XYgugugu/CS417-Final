using PVZ3D.Resource;
using UnityEngine;
using UnityEngine.AI;

namespace PVZ3D.NPC
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Trotter : MonoBehaviour
    {
        [Header("Target")]
        public Transform player;

        [Header("Formation")]
        public float baseFollowDistance = 3.5f;
        public float spacing = 2.0f;
        public float repathInterval = 0.15f;

        [Header("Movement")]
        public float slotSampleRadius = 2.0f;
        public float faceTurnSpeed = 8f;

        [Header("Coin Chase")]
        public float coinSearchRadius = 999f;
        public float consumeDistance = 0.75f;
        public int maxConsume = 10;

        public int consumedCount = 0;

        [Header("Idle")]
        public float minIdleDistance = 3f;
        public float idleMoveSpeed = 6f;
        
        [Header("Body")]
        [SerializeField] private Renderer bodyRenderer;
        [Header("Texture Randomization")]
        [SerializeField] private string textureResourcePath = "Models/MaoMaoGao/textures/BodyTextures";

        private NavMeshAgent agent;
        private float repathTimer;
        private float normalMoveSpeed;
        private float idleAngle;
        private Vector3 idleDestination;

        private int followIndex = 0;
        private int totalFollowers = 1;

        private Coin currentTargetCoin;

        public void SetFollowIndex(int index, int total)
        {
            followIndex = index;
            totalFollowers = Mathf.Max(1, total);
        }

        private void Awake()
        {
            ApplyRandomBodyTexture();

            agent = GetComponent<NavMeshAgent>();
            normalMoveSpeed = agent.speed;
            idleAngle = Random.Range(0f, Mathf.PI * 2f);
            idleDestination = transform.position;
        }

        private void OnEnable()
        {
            NPCFollowManager.Instance?.RegisterTrotter(this);
        }

        private void OnDisable()
        {
            ReleaseCurrentCoin();
            NPCFollowManager.Instance?.UnregisterTrotter(this);
        }

        private void Update()
        {
            if (player == null) return;

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                UpdateBehavior();
            }

            FaceMoveDirection();
        }

        private void UpdateBehavior()
        {
            if (consumedCount >= maxConsume)
            {
                Despawn();
                return;
            }

            if (currentTargetCoin != null)
            {
                if (!IsCoinStillValid(currentTargetCoin))
                {
                    ReleaseCurrentCoin();
                    IdleAroundPlayer();
                    return;
                }

                ChaseCoin(currentTargetCoin);
                return;
            }

            Coin coin = FindBestAvailableCoin();

            if (coin != null)
            {
                ClaimCoin(coin);
                ChaseCoin(coin);
            }
            else
            {
                IdleAroundPlayer();
            }
        }

        private void ChaseCoin(Coin coin)
        {
            agent.speed = normalMoveSpeed;
            agent.SetDestination(coin.transform.position);

            if (Vector3.Distance(transform.position, coin.transform.position) <= consumeDistance)
                ConsumeCurrentCoin();
        }

        private void IdleAroundPlayer()
        {
            agent.speed = idleMoveSpeed;

            if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.25f)
            {
                agent.SetDestination(GetIdlePosition());
            }
        }

        private Coin FindBestAvailableCoin()
        {
            Coin[] coins = FindObjectsByType<Coin>(FindObjectsSortMode.None);

            Coin best = null;
            float bestSqrDist = float.MaxValue;
            float maxSqrDist = coinSearchRadius * coinSearchRadius;

            foreach (Coin coin in coins)
            {
                if (coin == null || coin.IsCollected || coin.isClaimed) continue;

                float sqrDist = (coin.transform.position - transform.position).sqrMagnitude;
                if (sqrDist > maxSqrDist || sqrDist >= bestSqrDist) continue;

                bestSqrDist = sqrDist;
                best = coin;
            }

            return best;
        }

        private void ClaimCoin(Coin coin)
        {
            coin.isClaimed = true;
            coin.claimedByTrotter = this;
            currentTargetCoin = coin;
        }

        private void ReleaseCurrentCoin()
        {
            if (currentTargetCoin != null && currentTargetCoin.claimedByTrotter == this)
            {
                currentTargetCoin.isClaimed = false;
                currentTargetCoin.claimedByTrotter = null;
            }

            currentTargetCoin = null;
            ResetAgentPath();
        }

        private bool IsCoinStillValid(Coin coin)
        {
            return coin != null
                && coin.gameObject.activeInHierarchy
                && !coin.IsCollected
                && coin.claimedByTrotter == this;
        }

        private void ConsumeCurrentCoin()
        {
            if (!IsCoinStillValid(currentTargetCoin)) return;

            Coin coinToDestroy = currentTargetCoin;
            currentTargetCoin = null;
            ResetAgentPath();

            if (coinToDestroy.Collect())
            {
                consumedCount++;
            }

            if (consumedCount >= maxConsume)
            {
                Despawn();
            }
        }

        private Vector3 GetIdlePosition()
        {
            Vector3 playerPos = player.position;
            playerPos.y = transform.position.y;

            float radius = Mathf.Max(minIdleDistance, baseFollowDistance + spacing * totalFollowers);
            idleAngle = Random.Range(0f, Mathf.PI * 2f);

            idleDestination = playerPos + new Vector3(
                Mathf.Cos(idleAngle) * radius,
                0f,
                Mathf.Sin(idleAngle) * radius
            );

            if (NavMesh.SamplePosition(idleDestination, out NavMeshHit hit, slotSampleRadius, NavMesh.AllAreas))
                return hit.position;

            return idleDestination;
        }

        private void FaceMoveDirection()
        {
            Vector3 velocity = agent.velocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude <= 0.05f) return;

            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                faceTurnSpeed * Time.deltaTime
            );
        }

        [ContextMenu("Despawn")]
        public void Despawn()
        {
            ReleaseCurrentCoin();

            Destroy(gameObject);
        }

        private void ResetAgentPath()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }

        private void ApplyRandomBodyTexture()
        {
            if (bodyRenderer == null)
            {
                Transform body = transform.Find("Body");
                if (body != null)
                {
                    bodyRenderer = body.GetComponentInChildren<Renderer>();
                }
            }

            if (bodyRenderer == null)
            {
                Debug.LogWarning($"{name}: No body renderer found.");
                return;
            }

            Texture2D[] textures = Resources.LoadAll<Texture2D>(textureResourcePath);

            if (textures == null || textures.Length == 0)
            {
                Debug.LogWarning($"{name}: No textures found in Resources/{textureResourcePath}.");
                return;
            }

            Texture2D selectedTexture = textures[Random.Range(0, textures.Length)];

            Material[] mats = bodyRenderer.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i].mainTexture = selectedTexture;
            }

            bodyRenderer.materials = mats;
        }
    }
}
