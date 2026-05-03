using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PVZ3D.Core
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

        private XRSimpleInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();

            if (interactable != null)
            {
                interactable.selectEntered.AddListener(_ => TryPurchaseTool());
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
                return;
            }

            SpawnTool();
            RefreshPriceText();
        }

        private void SpawnTool()
        {
            if (toolPrefab == null)
            {
                Debug.LogWarning("ToolShopItem: toolPrefab is not assigned.");
                return;
            }

            Transform point = GetAvailableSpawnPoint();

            if (point == null)
            {
                Debug.LogWarning("ToolShopItem: no empty spawn point found.");
                return;
            }

            Instantiate(toolPrefab, point.position, point.rotation);
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
            if (priceText == null) return;

            priceText.text = $"{toolType}\n{coinCost} Coins";
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