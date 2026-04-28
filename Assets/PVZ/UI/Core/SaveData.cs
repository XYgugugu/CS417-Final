using System;
using System.Collections.Generic;

namespace PVZ3D.UI
{
    /// <summary>
    /// Plain-old serializable container for everything that persists across
    /// play sessions. Serialized by Unity's JsonUtility (no Dictionary support
    /// → cooldowns are stored as parallel lists).
    ///
    /// Bump <see cref="schemaVersion"/> whenever fields change shape.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int schemaVersion = 1;

        // Resources
        public int sun;
        public int coins;

        // Player
        public int health;
        public int maxHealth;

        // Wave / scoring
        public int currentWave;
        public int totalWaves;
        public int zombiesDefeated;
        public int highestWaveReached;

        // Flags
        public bool cheatModeEnabled;

        // Idle progress book-keeping
        public long lastSavedUtcTicks;

        // Plant unlocks
        public List<string> unlockedPlants = new();

        // Plant cooldowns (parallel arrays — JsonUtility can't serialize Dictionary)
        public List<string> plantCooldownIds = new();
        public List<float> plantCooldownRemain = new();
        public List<float> plantCooldownTotal = new();
    }
}
