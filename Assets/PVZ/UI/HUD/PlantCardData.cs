using UnityEngine;

namespace PVZ3D.UI
{
    [CreateAssetMenu(fileName = "PlantCard_", menuName = "PVZ/Plant Card Data", order = 1)]
    public class PlantCardData : ScriptableObject
    {
        [Tooltip("Stable string ID used by PlantCardSlotUI, e.g. sunflower, pea_shooter, wallnut.")]
        public string plantId;

        [Tooltip("Display name on the card.")]
        public string displayName;

        [Tooltip("Card portrait.")]
        public Sprite icon;

        [Tooltip("Legacy fallback cost. Runtime cost comes from GameManager.PlantsEconomy.")]
        public int sunCost = 100;

        [Tooltip("Legacy fallback cooldown. Runtime cooldown comes from GameManager.PlantsEconomy.")]
        public float cooldownSeconds = 7.5f;

        [Tooltip("If true, this plant is available from the start.")]
        public bool unlockedByDefault = true;
    }
}
