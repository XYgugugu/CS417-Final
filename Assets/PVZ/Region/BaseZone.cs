using UnityEngine;
using PVZ3D.Zombies;
using PVZ3D.Core;

public class BaseZone : MonoBehaviour
{
    private GameManager gm;

    private void Awake()
    {
        gm = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        ZombieBase zombie = ResolveZombie(other);
        if (zombie == null) return;

        TriggerBaseDamage(Mathf.CeilToInt(zombie.AttackDamage));
        Destroy(zombie.gameObject);
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
}
