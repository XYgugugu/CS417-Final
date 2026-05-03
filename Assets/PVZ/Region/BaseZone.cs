using UnityEngine;
using PVZ3D.Zombies;
using PVZ3D.Core;

public class BaseZone : MonoBehaviour
{
    [SerializeField] private int fallbackDamageAmount = 0;

    private GameManager gm;

    private void Awake()
    {
        gm = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        ZombieBase zombie = ResolveZombie(other);
        if (zombie == null && !IsZombieTagged(other))
        {
            return;
        }

        int damageAmount = zombie != null
            ? Mathf.CeilToInt(zombie.AttackDamage)
            : fallbackDamageAmount;

        TriggerBaseDamage(damageAmount);

        if (zombie != null)
        {
            Destroy(zombie.gameObject);
        }
        else
        {
            Destroy(GetTaggedZombieRoot(other));
        }
    }

    private void TriggerBaseDamage(int amount)
    {
        if (gm == null)
        {
            gm = FindObjectOfType<GameManager>();
        }

        if (gm == null)
        {
            Debug.LogWarning($"{name}: No GameManager found in scene.");
            return;
        }

        if (gm.PlayerManager == null)
        {
            Debug.LogWarning($"{name}: GameManager has no playerManager assigned.");
            return;
        }

        gm.PlayerManager.LoseHealth(amount);
    }

    [ContextMenu("Test Base Damage")]
    public void TestBaseDamage()
    {
        TriggerBaseDamage(fallbackDamageAmount);
    }

    private static ZombieBase ResolveZombie(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        ZombieBase zombie = other.GetComponentInParent<ZombieBase>();
        if (zombie != null)
        {
            return zombie;
        }

        Rigidbody attachedBody = other.attachedRigidbody;
        return attachedBody != null ? attachedBody.GetComponentInParent<ZombieBase>() : null;
    }

    private static bool IsZombieTagged(Collider other)
    {
        return GetTaggedZombieRoot(other) != null;
    }

    private static GameObject GetTaggedZombieRoot(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        Transform current = other.transform;
        while (current != null)
        {
            if (current.CompareTag("Zombie"))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        Rigidbody attachedBody = other.attachedRigidbody;
        if (attachedBody == null)
        {
            return null;
        }

        current = attachedBody.transform;
        while (current != null)
        {
            if (current.CompareTag("Zombie"))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }
}
