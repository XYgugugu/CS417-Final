using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Core
{
    public class PlantShopItem : MonoBehaviour
    {
        [Header("Game Manager")]
        [SerializeField] private GameManager gameManager;

        [Header("Plant")]
        [SerializeField] private PlantType plantType;

        public PlantType ShopPlantType => plantType;

        [Header("Prefab To Spawn")]
        [SerializeField] private GameObject plantPrefab;

        [Header("Spawn Points Near Shop")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float occupiedCheckRadius = 0.4f;
        [SerializeField] private LayerMask occupiedLayer;

        [Header("Price UI")]
        [SerializeField] private TextMeshProUGUI priceText;

        private XRSimpleInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();

            if (interactable != null)
            {
                interactable.selectEntered.AddListener(_ => TryPurchasePlant());
            }
        }

        private void Start()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            RefreshPriceText();
        }

        private void OnDestroy()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveAllListeners();
            }
        }

        public void TryPurchasePlant()
        {
            if (gameManager == null)
            {
                Debug.LogError("PlantShopItem: GameManager is not assigned.");
                return;
            }

            bool purchaseSucceeded = gameManager.PlantsEconomy.ExchangePlant(plantType);

            if (!purchaseSucceeded)
            {
                Debug.Log("Plant purchase failed: not enough sun or plant is on cooldown.");
                return;
            }

            SpawnPlant();
            RefreshPriceText();
        }

        private void SpawnPlant()
        {
            if (plantPrefab == null)
            {
                Debug.LogWarning("PlantShopItem: plantPrefab is not assigned.");
                return;
            }

            Transform point = GetAvailableSpawnPoint();

            if (point == null)
            {
                Debug.LogWarning("PlantShopItem: no empty spawn point found.");
                return;
            }

            Instantiate(plantPrefab, point.position, point.rotation);
        }

        private Transform GetAvailableSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return null;
            }

            foreach (Transform point in spawnPoints)
            {
                if (point == null) continue;

                bool occupied = Physics.CheckSphere(
                    point.position,
                    occupiedCheckRadius,
                    occupiedLayer
                );

                if (!occupied)
                {
                    return point;
                }
            }

            return null;
        }

        private void RefreshPriceText()
        {
            if (priceText == null || gameManager == null) return;

            int cost = gameManager.PlantsEconomy.GetPlantCost(plantType);
            float cooldown = gameManager.PlantsEconomy.GetPlantCooldownRemaining(plantType);

            if (cooldown > 0f)
            {
                priceText.text = $"{plantType}\n{cost} Sun\nCD: {cooldown:0}s";
            }
            else
            {
                priceText.text = $"{plantType}\n{cost} Sun";
            }
        }

        private void Update()
        {
            RefreshPriceText();
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnPoints == null) return;

            foreach (Transform point in spawnPoints)
            {
                if (point == null) continue;
                Gizmos.DrawWireSphere(point.position, occupiedCheckRadius);
            }
        }
    }
}