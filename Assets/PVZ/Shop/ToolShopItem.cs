using UnityEngine;
using TMPro;
using PVZ3D.Core;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Shop
{
    public class ToolShopItem : MonoBehaviour
    {
        public enum ToolType
        {
            Trotter,
            Replicator,
            Shovel
        }

        [Header("Game Manager")]
        [SerializeField] private GameManager gameManager;

        [Header("Tool")]
        [SerializeField] private ToolType toolType;
        [SerializeField] private int coinCost = 10;

        [Header("Prefab To Spawn")]
        [SerializeField] private GameObject toolPrefab;

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
            TryPurchaseTool();
        }

        public void TryPurchaseTool()
        {
            if (gameManager == null)
            {
                Debug.LogError("ToolShopItem: GameManager is not assigned.");
                return;
            }

            bool purchaseSucceeded = gameManager.ResourceManager.ExchangeCoin(coinCost);

            if (!purchaseSucceeded)
            {
                Debug.Log("Tool purchase failed: not enough coins.");
                PlayShopSound(purchaseFailedClip);
                return;
            }

            GameObject spawnedTool = SpawnTool();
            PlayShopSound(purchaseClip);
            PlayPurchaseConfetti(spawnedTool);
            RefreshPriceText();
        }

        private GameObject SpawnTool()
        {
            if (toolPrefab == null)
            {
                Debug.LogWarning("ToolShopItem: toolPrefab is not assigned.");
                return null;
            }

            Transform point = ShopItemUtility.GetAvailableSpawnPoint(spawnPoints, occupiedCheckRadius, occupiedLayer);

            if (point == null)
            {
                Debug.LogWarning("ToolShopItem: no empty spawn point found.");
                return null;
            }

            return Instantiate(toolPrefab, point.position, point.rotation);
        }

        private void RefreshPriceText()
        {
            if (priceText == null) return;

            priceText.text = $"{toolType}\n{coinCost} Coins";
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
