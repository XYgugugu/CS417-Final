using System;
using System.IO;
using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// JSON-on-disk persistence for <see cref="GameState"/>. Stored in
    /// <c>Application.persistentDataPath</c> so it survives reinstalls of the
    /// editor / built player.
    ///
    /// Use <see cref="Save"/> and <see cref="Load"/>; never read/write fields
    /// on the file directly from gameplay code.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "pvz_save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave => File.Exists(FilePath);

        public static event Action OnSaved;
        public static event Action<SaveData> OnLoaded;

        /// <summary>Capture the current <see cref="GameState"/> snapshot to disk.</summary>
        public static bool Save()
        {
            try
            {
                var data = GameState.CaptureSnapshot();
                var json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
                OnSaved?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>Read disk and apply to <see cref="GameState"/>. Returns the parsed data, or null if no save / on failure.</summary>
        public static SaveData Load()
        {
            if (!HasSave) return null;
            try
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[SaveSystem] Save file existed but parsed to null. Ignoring.");
                    return null;
                }
                GameState.ApplySnapshot(data);
                OnLoaded?.Invoke(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>Read disk WITHOUT applying to GameState. Useful for the start menu's "Continue" preview.</summary>
        public static SaveData PeekLoad()
        {
            if (!HasSave) return null;
            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] PeekLoad failed: {e.Message}");
                return null;
            }
        }

        /// <summary>Wipe the save file. Used when starting a brand-new game.</summary>
        public static bool Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Delete failed: {e.Message}");
                return false;
            }
        }

        /// <summary>For debugging / showing a "save located at..." in the menu.</summary>
        public static string GetSavePath() => FilePath;
    }
}
