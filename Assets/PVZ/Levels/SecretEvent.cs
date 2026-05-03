using PVZ3D.Core;
using PVZ3D.Zombies;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Levels
{
    public class SecretEvent : MonoBehaviour
    {
        [Header("Optional References")]
        [Tooltip("Optional explicit GameManager reference. If unset, the first active GameManager in the scene is used.")]
        [SerializeField] private GameManager gameManager;

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
            Debug.Log($"Secret Event: Player receives {count} random plant(s).");

            GameManager gm = ResolveGameManager();
            if (gm == null)
            {
                Debug.Log("Secret Event: No GameManager found. Plant reward remains a placeholder.");
                return;
            }

            PlantType[] shopPlantTypes = ResolvePurchasablePlantTypes();
            if (shopPlantTypes.Length == 0)
            {
                Debug.Log("Secret Event: No purchasable PlantShopItem found and no safe fallback plant list available.");
                return;
            }

            int totalSun = 0;
            var pickedTypes = new List<PlantType>();

            for (int i = 0; i < count; i++)
            {
                PlantType chosen = shopPlantTypes[Random.Range(0, shopPlantTypes.Length)];
                pickedTypes.Add(chosen);
                totalSun += gm.PlantsEconomy.GetPlantCost(chosen);
            }

            gm.PlantsEconomy.CollectSun(totalSun);
            Debug.Log($"Secret Event: Granted {totalSun} Sun for random shop plant purchase power: {string.Join(", ", pickedTypes)}.");
        }

        private PlantType[] ResolvePurchasablePlantTypes()
        {
            PlantShopItem[] shopItems = FindObjectsOfType<PlantShopItem>();
            var types = new List<PlantType>();

            foreach (PlantShopItem item in shopItems)
            {
                if (!types.Contains(item.ShopPlantType))
                {
                    types.Add(item.ShopPlantType);
                }
            }

            if (types.Count > 0)
            {
                return types.ToArray();
            }

            return new PlantType[0];
        }

        private void InstantWin()
        {
            Debug.Log("Secret Event: Instant win triggered.");

            GameManager gm = ResolveGameManager();
            if (gm != null)
            {
                gm.WinGame();
                Debug.Log("Secret Event: Connected to existing GameManager.WinGame().");
                return;
            }

            Debug.Log("Secret Event: No GameManager found. Win trigger remains a placeholder.");
        }

        private void SpawnNewZombies(int count)
        {
            Debug.Log($"Secret Event: Spawn {count} new zombie(s).");

            ZombieSpawner spawner = FindObjectOfType<ZombieSpawner>();
            if (spawner != null)
            {
                spawner.SpawnZombies(count);
                Debug.Log("Secret Event: Connected to existing ZombieSpawner.SpawnZombies().");
                return;
            }

            Debug.Log("Secret Event: No ZombieSpawner found. Spawn new zombies remains a placeholder.");
        }

        private void SpawnRandomZombies(int count)
        {
            Debug.Log($"Secret Event: Spawn {count} random zombie(s).");

            ZombieSpawner spawner = FindObjectOfType<ZombieSpawner>();
            if (spawner != null)
            {
                spawner.SpawnZombies(count);
                Debug.Log("Secret Event: Connected to existing ZombieSpawner.SpawnZombies().");
                return;
            }

            Debug.Log("Secret Event: No ZombieSpawner found. Spawn random zombies remains a placeholder.");
        }

        private void RestartOrLose()
        {
            Debug.Log("Secret Event: Restart / lose triggered.");

            GameManager gm = ResolveGameManager();
            if (gm != null && gm.PlayerManager != null)
            {
                gm.PlayerManager.SetHealth(0);
                Debug.Log("Secret Event: Connected to existing PlayerManager and forced player death to trigger game over.");
                return;
            }

            Debug.Log("Secret Event: No GameManager/PlayerManager found. Restart/lose remains a placeholder.");
        }

        private GameManager ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            return gameManager;
        }
    }
}
