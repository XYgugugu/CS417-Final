using System;

namespace PVZ3D.Core
{
    [Serializable]
    public class RunStats
    {
        public bool Won;
        public int WavesCleared;
        public int ZombiesDefeated;
        public int PlantsPlaced;
        public int TotalSunCollected;
        public int TotalCoinsEarned;

        public void Reset()
        {
            Won = false;
            WavesCleared = 0;
            ZombiesDefeated = 0;
            PlantsPlaced = 0;
            TotalSunCollected = 0;
            TotalCoinsEarned = 0;
        }

        public RunStats Clone()
        {
            return new RunStats
            {
                Won = Won,
                WavesCleared = WavesCleared,
                ZombiesDefeated = ZombiesDefeated,
                PlantsPlaced = PlantsPlaced,
                TotalSunCollected = TotalSunCollected,
                TotalCoinsEarned = TotalCoinsEarned
            };
        }
    }
}
