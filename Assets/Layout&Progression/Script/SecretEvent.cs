using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SecretRandomEvent : MonoBehaviour
{
    private bool used = false;
    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelected);
        }
        else
        {
            Debug.LogWarning("SecretRandomEvent needs an XR Simple Interactable on " + gameObject.name);
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelected);
        }
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        TriggerSecret();
    }

    private void TriggerSecret()
    {
        if (used) return;

        used = true;

        int roll = Random.Range(1, 101); // 1 to 100

        if (roll <= 20)
        {
            GiveRandomPlants(3);
        }
        else if (roll <= 60)
        {
            GiveRandomPlants(1);
        }
        else if (roll <= 65)
        {
            InstantWin();
        }
        else if (roll <= 85)
        {
            SpawnNewZombies(2);
        }
        else if (roll <= 95)
        {
            SpawnRandomZombies(3);
        }
        else
        {
            RestartOrLose();
        }

        gameObject.SetActive(false);
    }

    private void GiveRandomPlants(int count)
    {
        Debug.Log("Secret Event: Player receives " + count + " random plant(s).");

        // Placeholder:
        // Later connect to teammate's plant/inventory system.
        // Example future call:
        // PlantManager.Instance.AddRandomPlants(count);
    }

    private void InstantWin()
    {
        Debug.Log("Secret Event: Instant win triggered.");

        // Placeholder:
        // Later connect to teammate's win/level clear system.
        // Example future call:
        // GameManager.Instance.WinLevel();
        // or LevelManager.Instance.CompleteLevel();
    }

    private void SpawnNewZombies(int count)
    {
        Debug.Log("Secret Event: Spawn " + count + " new zombie(s).");

        // Placeholder:
        // Later connect to teammate's zombie/spawner system.
        // Example future call:
        // ZombieSpawner.Instance.SpawnSpecialZombies(count);
    }

    private void SpawnRandomZombies(int count)
    {
        Debug.Log("Secret Event: Spawn " + count + " random zombie(s).");

        // Placeholder:
        // Later connect to teammate's zombie/spawner system.
        // Example future call:
        // ZombieSpawner.Instance.SpawnRandomZombies(count);
    }

    private void RestartOrLose()
    {
        Debug.Log("Secret Event: Restart / lose triggered.");

        // Placeholder:
        // Later connect to teammate's fail/restart system.
        // Example future call:
        // GameManager.Instance.RestartLevel();
        // or GameManager.Instance.LoseLevel();
    }
}