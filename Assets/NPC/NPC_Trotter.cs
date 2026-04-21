using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPC_Trotter : MonoBehaviour
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
    public float coinSearchRadius = 999f;     // effectively whole scene unless you lower it
    public float consumeDistance = 0.75f;
    public int maxConsume = 10;

    [Header("Debug")]
    public int consumedCount = 0;
    public int storedValue = 0;

    private NavMeshAgent agent;
    private float repathTimer;

    private int followIndex = 0;
    private int totalFollowers = 1;

    private Coin currentTargetCoin = null;
    private bool isBusyConsuming = false;

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
            NPCFollowManager.Instance.RegisterTrotter(this);
    }

    private void OnDisable()
    {
        ReleaseCurrentCoin();

        if (NPCFollowManager.Instance != null)
            NPCFollowManager.Instance.UnregisterTrotter(this);
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
        // If max reached, never chase coins again.
        if (consumedCount >= maxConsume)
        {
            currentTargetCoin = null;
            isBusyConsuming = false;
            agent.SetDestination(GetFollowSlotPosition());
            return;
        }

        // If already chasing one coin, stay locked on it until finished/lost.
        if (currentTargetCoin != null)
        {
            if (!IsCoinStillValid(currentTargetCoin))
            {
                ReleaseCurrentCoin();
                agent.SetDestination(GetFollowSlotPosition());
                return;
            }

            agent.SetDestination(currentTargetCoin.transform.position);

            float dist = Vector3.Distance(transform.position, currentTargetCoin.transform.position);
            if (dist <= consumeDistance)
            {
                ConsumeCurrentCoin();
            }

            return;
        }

        // Not currently busy: try to claim a coin.
        Coin coin = FindBestAvailableCoin();
        if (coin != null)
        {
            ClaimCoin(coin);
            agent.SetDestination(coin.transform.position);
            return;
        }

        // No coin available: normal follow.
        agent.SetDestination(GetFollowSlotPosition());
    }

    private Coin FindBestAvailableCoin()
    {
        Coin[] allCoins = FindObjectsByType<Coin>(FindObjectsSortMode.None);

        Coin best = null;
        float bestSqrDist = float.MaxValue;
        float maxSqr = coinSearchRadius * coinSearchRadius;

        for (int i = 0; i < allCoins.Length; i++)
        {
            Coin coin = allCoins[i];
            if (coin == null) continue;
            if (coin.isClaimed) continue;

            float sqrDist = (coin.transform.position - transform.position).sqrMagnitude;
            if (sqrDist > maxSqr) continue;

            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                best = coin;
            }
        }

        return best;
    }

    private void ClaimCoin(Coin coin)
    {
        if (coin == null) return;

        coin.isClaimed = true;
        coin.claimedByTrotter = this;

        currentTargetCoin = coin;
        isBusyConsuming = true;
    }

    private void ReleaseCurrentCoin()
    {
        if (currentTargetCoin != null && currentTargetCoin.claimedByTrotter == this)
        {
            currentTargetCoin.isClaimed = false;
            currentTargetCoin.claimedByTrotter = null;
        }

        currentTargetCoin = null;
        isBusyConsuming = false;
    }

    private bool IsCoinStillValid(Coin coin)
    {
        if (coin == null) return false;
        if (!coin.gameObject.activeInHierarchy) return false;
        if (coin.claimedByTrotter != this) return false;

        return true;
    }

    private void ConsumeCurrentCoin()
    {
        if (currentTargetCoin == null) return;
        if (currentTargetCoin.claimedByTrotter != this) return;

        storedValue += currentTargetCoin.value;
        consumedCount += 1;

        Coin coinToDestroy = currentTargetCoin;

        currentTargetCoin = null;
        isBusyConsuming = false;

        Destroy(coinToDestroy.gameObject);
    }

    private Vector3 GetFollowSlotPosition()
    {
        Vector3 playerPos = player.position;
        playerPos.y = transform.position.y;

        Vector3 forward = player.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 right = new Vector3(forward.z, 0f, -forward.x);

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

        float extraPlayerBuffer = 1.0f;
        float backwardDist = baseFollowDistance + extraPlayerBuffer + row * spacing;

        float lateralOffset;
        if (row == 0)
        {
            lateralOffset = 0f;
        }
        else
        {
            float start = -((row * 2 - 1) * spacing * 0.5f);
            lateralOffset = start + posInRow * spacing;
        }

        Vector3 desired =
            playerPos
            - forward * backwardDist
            + right * lateralOffset;

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