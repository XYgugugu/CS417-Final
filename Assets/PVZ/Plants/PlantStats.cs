namespace PVZ3D.Plants
{
    public readonly struct PlantStats
    {
        public PlantStats(int cost, float maxHealth, float cooldown)
        {
            Cost = cost;
            MaxHealth = maxHealth;
            Cooldown = cooldown;
        }

        public int Cost { get; }
        public float MaxHealth { get; }
        public float Cooldown { get; }

        public static PlantStats Get(PlantType type)
        {
            return type switch
            {
                PlantType.Sunflower => new PlantStats(50, 100f, 5f),
                PlantType.Peashooter => new PlantStats(100, 100f, 5f),
                PlantType.WallNut => new PlantStats(50, 800f, 15f),
                _ => new PlantStats(0, 100f, 0f)
            };
        }
    }
}
