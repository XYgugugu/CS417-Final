using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// Designer-tunable defaults for the UI/save layer. Right-click in Project
    /// window: Create → PVZ → Game Settings, then drag onto GameSceneBootstrap.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "PVZ/Game Settings", order = 0)]
    public class GameSettings : ScriptableObject
    {
        [Header("Player")]
        [Tooltip("Starting and maximum HP for a fresh run.")]
        public int playerMaxHealth = 100;

        [Header("Starting Resources")]
        public int startingSun = 50;
        public int startingCoins = 0;

        [Header("Idle Progress")]
        [Tooltip("Sun produced per second while the game is closed.")]
        public float idleSunPerSecond = 0.5f;

        [Tooltip("Coins produced per second while the game is closed.")]
        public float idleCoinsPerSecond = 0.1f;

        [Tooltip("Hard cap on offline duration (seconds) — prevents abuse from clock changes. Default = 12 hours.")]
        public float maxIdleSeconds = 12f * 60f * 60f;

        [Header("Cheat Mode")]
        [Tooltip("Sun granted when entering cheat mode from the start menu.")]
        public int cheatSunBoost = 9999;
        public int cheatCoinBoost = 9999;

        [Header("Scenes")]
        [Tooltip("Build-settings scene name for the start menu.")]
        public string startMenuSceneName = "StartMenu";

        [Tooltip("Build-settings scene name for the gameplay level.")]
        public string gameplaySceneName = "Level1_Farm";

        [Header("Default Plant Loadout")]
        [Tooltip(
            "PlantCardData assets that are auto-unlocked when a player starts a fresh save.\n\n" +
            "TO ADD A PLANT TO THE STARTING LOADOUT:\n" +
            "  1. Create a PlantCardData asset (right-click → Create → PVZ → Plant Card Data).\n" +
            "  2. Drag the new asset into this array.\n" +
            "  3. (Optional) Add a matching HUD slot — see PlantCardSlotUI top-of-class doc.\n\n" +
            "TO REMOVE: just shrink the array / delete the entry.\n\n" +
            "NOTE: Existing saves keep their already-unlocked list; this array only seeds " +
            "FRESH games. Use GameState.UnlockPlant(id) at runtime to unlock a plant for an " +
            "ongoing save.")]
        public PlantCardData[] defaultUnlockedPlants;
    }
}
