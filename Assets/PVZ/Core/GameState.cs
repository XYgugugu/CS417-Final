using System;

namespace PVZ3D.Core
{
    [Serializable]
    public class GameState
    {
        public GamePhase Phase = GamePhase.Menu;
        public int CurrentWave;
        public int TotalWaves;
        public int BaseHealth = 10;
        public int Sun = 0;
        public int Coins = 0;
        public int PlacedPlants = 0;
    }
}
