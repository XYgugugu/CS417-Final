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
        private GameObject coinPrefab;
        public float coinSearchRadius = 999f;
        public float consumeDistance = 0.75f;
        public int maxConsume = 10;

        [Header("Loot")]
        public int consumedCount = 0;
        public int storedValue = 0;
        public float dropHeight = 0.05f;

        [Header("Idle")]
        public float minIdleDistance = 3f;
        public float idleMoveSpeed = 6f;

        private NavMeshAgent agent;
        private float repathTimer;
        private float normalMoveSpeed;
        private float idleAngle;

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
            coinPrefab = Resources.Load<GameObject>("Coin");
            if (coinPrefab == null)
                Debug.LogError("Trotter: Could not load Coin.prefab from Resources.");

            agent = GetComponent<NavMeshAgent>();
            normalMoveSpeed = agent.speed;
            idleAngle = Random.Range(0f, Mathf.PI * 2f);
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
                ReleaseCurrentCoin();
                IdleAroundPlayer();
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
            agent.SetDestination(GetIdlePosition());
        }

        private Coin FindBestAvailableCoin()
        {
            Coin[] coins = FindObjectsByType<Coin>(FindObjectsSortMode.None);

            Coin best = null;
            float bestSqrDist = float.MaxValue;
            float maxSqrDist = coinSearchRadius * coinSearchRadius;

            foreach (Coin coin in coins)
            {
                if (coin == null || coin.isClaimed) continue;

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
        }

        private bool IsCoinStillValid(Coin coin)
        {
            return coin != null
                && coin.gameObject.activeInHierarchy
                && coin.claimedByTrotter == this;
        }

        private void ConsumeCurrentCoin()
        {
            if (!IsCoinStillValid(currentTargetCoin)) return;

            storedValue += currentTargetCoin.value;
            consumedCount++;

            Coin coinToDestroy = currentTargetCoin;
            currentTargetCoin = null;

            Destroy(coinToDestroy.gameObject);
        }

        private Vector3 GetIdlePosition()
        {
            Vector3 playerPos = player.position;
            playerPos.y = transform.position.y;

            float radius = Mathf.Max(minIdleDistance, baseFollowDistance + spacing * totalFollowers);
            idleAngle += repathInterval;

            Vector3 desired = playerPos + new Vector3(
                Mathf.Cos(idleAngle) * radius,
                0f,
                Mathf.Sin(idleAngle) * radius
            );

            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, slotSampleRadius, NavMesh.AllAreas))
                return hit.position;

            return desired;
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

            if (coinPrefab != null && storedValue > 0)
            {
                GameObject spawnedCoinObj = Instantiate(
                    coinPrefab,
                    transform.position + Vector3.up * dropHeight,
                    Quaternion.identity
                );

                Coin spawnedCoin = spawnedCoinObj.GetComponent<Coin>();
                if (spawnedCoin != null)
                    spawnedCoin.value = storedValue;
            }

            Destroy(gameObject);
        }
    }
}