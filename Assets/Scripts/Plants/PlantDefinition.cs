using UnityEngine;

namespace PVZ3D.Plants
{
    public enum PlantArchetype
    {
        Sunflower,
        Peashooter,
        Wallnut
    }

    [CreateAssetMenu(menuName = "PVZ3D/Plant Definition", fileName = "PlantDefinition")]
    public class PlantDefinition : ScriptableObject
    {
        public string PlantId = "plant";
        public string DisplayName = "Plant";
        public PlantArchetype Archetype = PlantArchetype.Sunflower;
        public int SunCost = 50;
        public float MaxHealth = 100f;
        public float AttackDamage = 20f;
        public float AttackRate = 1.25f;
        public float AttackRange = 8f;
        public int SunPerDrop = 25;
        public float SunDropInterval = 7f;
        public GameObject Prefab;
    }
}
