using System;

namespace PVZ3D.Core
{
    public enum GamePhase
    {
        Menu,
        Prep,
        Battle,
        Win,
        Lose,
        Paused
    }

    [Serializable]
    public class GameState
    {
        public GamePhase Phase = GamePhase.Menu;
        public int Sun;
        public int Coins;
        public int CurrentWave;
        public int TotalWaves;
        public int BaseHealth;
        public bool CheatMode;
        public int PlacedPlants;
        public int AliveZombies;

        public GameState Clone()
        {
            return (GameState)MemberwiseClone();
        }
    }
}
