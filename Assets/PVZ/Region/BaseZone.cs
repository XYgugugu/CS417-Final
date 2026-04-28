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
        ZombieBase zombie = other.GetComponentInParent<ZombieBase>();
        if (zombie == null) return;

        TriggerBaseDamage(20);

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

    [ContextMenu("Test Base Damage")]
    public void TestBaseDamage()
    {
        TriggerBaseDamage(20);
    }

}