using System;
using System.IO;
using PVZ3D.Core;
using PVZ3D.Resources;
using UnityEngine;

namespace PVZ3D.Save
{
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "pvz3d_save.json";
        [SerializeField] private bool logSaveOperations;

        public SaveData CurrentData { get; private set; }

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentData = LoadOrCreate();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveCurrentState();
            }
        }

        private void OnApplicationQuit()
        {
            SaveCurrentState();
        }

        public SaveData LoadOrCreate()
        {
            if (!File.Exists(SaveFilePath))
            {
                SaveData created = SaveData.CreateDefault();
                Write(created);
                return created;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded == null)
                {
                    throw new Exception("Save file deserialized to null.");
                }

                if (loaded.UnlockedPlants == null || loaded.UnlockedPlants.Length < 3)
                {
                    loaded.UnlockedPlants = new[] { true, true, true };
                }

                if (loaded.LastRunStats == null)
                {
                    loaded.LastRunStats = new RunStats();
                }

                loaded.LastKnownSun = Mathf.Max(0, loaded.LastKnownSun);
                loaded.LastKnownCoins = Mathf.Max(0, loaded.LastKnownCoins);
                loaded.BestWaveReached = Mathf.Max(0, loaded.BestWaveReached);

                if (logSaveOperations)
                {
                    Debug.Log($"SaveSystem: Loaded save from {SaveFilePath}");
                }

                return loaded;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SaveSystem: Save data corrupt or unreadable. Recreating default save. Error: {ex.Message}");
                SaveData fallback = SaveData.CreateDefault();
                Write(fallback);
                return fallback;
            }
        }

        public void SaveCurrentState()
        {
            if (CurrentData == null)
            {
                CurrentData = SaveData.CreateDefault();
            }

            ResourceManager resources = ResourceManager.Instance;
            GameManager manager = GameManager.Instance;

            if (resources != null)
            {
                CurrentData.LastKnownSun = resources.CurrentSun;
                CurrentData.LastKnownCoins = resources.CurrentCoins;
            }

            if (manager != null)
            {
                CurrentData.CheatModeEnabled = manager.CheatModeEnabled;
                CurrentData.BestWaveReached = Mathf.Max(CurrentData.BestWaveReached, manager.State.CurrentWave);
                CurrentData.LastRunStats = manager.CurrentRunStats.Clone();
            }

            CurrentData.LastSessionUtc = DateTime.UtcNow.ToString("O");
            Write(CurrentData);
        }

        public void Write(SaveData data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath) ?? Application.persistentDataPath);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                if (logSaveOperations)
                {
                    Debug.Log($"SaveSystem: Wrote save file to {SaveFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"SaveSystem: Failed to write save file. {ex.Message}");
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Save/Force Save Now")]
        private void ForceSaveNow()
        {
            SaveCurrentState();
        }
#endif
    }
}
