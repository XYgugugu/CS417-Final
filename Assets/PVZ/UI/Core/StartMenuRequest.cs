using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// Tiny PlayerPrefs-backed "intent" channel between the Start Menu scene and
    /// the Gameplay scene. The menu sets a flag, the gameplay scene reads-and-clears
    /// it on Start. Survives a SceneManager.LoadScene boundary without needing a
    /// DontDestroyOnLoad singleton.
    /// </summary>
    public static class StartMenuRequest
    {
        private const string FreshGameKey = "PVZ_FreshGameRequested";
        private const string CheatModeKey = "PVZ_CheatModeRequested";

        public static void RequestFreshGame()
        {
            PlayerPrefs.SetInt(FreshGameKey, 1);
            PlayerPrefs.Save();
        }

        public static bool ConsumeFreshGameRequest()
        {
            if (PlayerPrefs.GetInt(FreshGameKey, 0) != 1) return false;
            PlayerPrefs.DeleteKey(FreshGameKey);
            PlayerPrefs.Save();
            return true;
        }

        public static void SetCheatMode(bool enabled)
        {
            PlayerPrefs.SetInt(CheatModeKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool ConsumeCheatMode()
        {
            var enabled = PlayerPrefs.GetInt(CheatModeKey, 0) == 1;
            // Cheat mode persists across runs intentionally — don't delete the key.
            return enabled;
        }
    }
}
