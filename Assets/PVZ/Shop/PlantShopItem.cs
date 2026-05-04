using UnityEngine;
using TMPro;
using PVZ3D.Core;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Shop
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

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip purchaseClip;
        [SerializeField] private AudioClip purchaseFailedClip;
        [SerializeField, Range(0f, 1f)] private float purchaseVolume = 1f;

        [Header("Purchase Effect")]
        [SerializeField] private GameObject purchaseConfettiPrefab;
        [SerializeField] private Vector3 purchaseConfettiOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private float purchaseConfettiLifetime = 3f;

        private XRSimpleInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (interactable != null)
            {
                interactable.selectEntered.AddListener(OnSelected);
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
                interactable.selectEntered.RemoveListener(OnSelected);
            }
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            TryPurchasePlant();
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
                PlayShopSound(purchaseFailedClip);
                return;
            }

            GameObject spawnedPlant = SpawnPlant();
            PlayShopSound(purchaseClip);
            PlayPurchaseConfetti(spawnedPlant);
            RefreshPriceText();
        }

        private GameObject SpawnPlant()
        {
            if (plantPrefab == null)
            {
                Debug.LogWarning("PlantShopItem: plantPrefab is not assigned.");
                return null;
            }

            Transform point = ShopItemUtility.GetAvailableSpawnPoint(spawnPoints, occupiedCheckRadius, occupiedLayer);

            if (point == null)
            {
                Debug.LogWarning("PlantShopItem: no empty spawn point found.");
                return null;
            }

            return Instantiate(plantPrefab, point.position, point.rotation);
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

        private void PlayShopSound(AudioClip clip)
        {
            ShopItemUtility.PlaySound(audioSource, clip, transform.position, purchaseVolume);
        }

        private void PlayPurchaseConfetti(GameObject purchasedItem)
        {
            ShopItemUtility.PlayPurchaseConfetti(
                purchaseConfettiPrefab,
                purchasedItem,
                purchaseConfettiOffset,
                purchaseConfettiLifetime);
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
