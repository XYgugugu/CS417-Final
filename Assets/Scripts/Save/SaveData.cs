using System;
using PVZ3D.Core;

namespace PVZ3D.Save
{
    [Serializable]
    public class SaveData
    {
        public int LastKnownSun = 150;
        public int LastKnownCoins = 0;
        public bool CheatModeEnabled;
        public int BestWaveReached;
        public string LastSessionUtc;
        public RunStats LastRunStats = new RunStats();
        public bool[] UnlockedPlants = new[] { true, true, true };

        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                LastKnownSun = 150,
                LastKnownCoins = 0,
                CheatModeEnabled = false,
                BestWaveReached = 0,
                LastSessionUtc = DateTime.UtcNow.ToString("O"),
                LastRunStats = new RunStats(),
                UnlockedPlants = new[] { true, true, true }
            };
        }
    }
}
