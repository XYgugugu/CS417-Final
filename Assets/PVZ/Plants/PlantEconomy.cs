using System.Collections.Generic;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PlantEconomy : MonoBehaviour
    {
        private static PlantEconomy instance;

        [SerializeField] private int startingSun = 100;
        [SerializeField] private int currentSun;

        private readonly Dictionary<PlantType, float> cooldownReadyTimes = new Dictionary<PlantType, float>();

        public static PlantEconomy Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject economyObject = new GameObject("Plant Economy");
                    instance = economyObject.AddComponent<PlantEconomy>();
                }

                return instance;
            }
        }

        public int CurrentSun => currentSun;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            currentSun = startingSun;
        }

        public bool CanPlant(PlantType type, out string reason)
        {
            PlantStats stats = PlantStats.Get(type);
            if (currentSun < stats.Cost)
            {
                reason = $"Need {stats.Cost} sun, current sun is {currentSun}.";
                return false;
            }

            float readyTime = GetReadyTime(type);
            if (Time.time < readyTime)
            {
                reason = $"{type} cooldown: {readyTime - Time.time:0.0}s remaining.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TrySpendForPlant(PlantType type, out string reason)
        {
            if (!CanPlant(type, out reason))
            {
                return false;
            }

            PlantStats stats = PlantStats.Get(type);
            currentSun -= stats.Cost;
            cooldownReadyTimes[type] = Time.time + stats.Cooldown;
            Debug.Log($"Sun: {currentSun} after planting {type}.");
            return true;
        }

        public void AddSun(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentSun += amount;
            Debug.Log($"Sun: {currentSun} (+{amount}).");
        }

        private float GetReadyTime(PlantType type)
        {
            return cooldownReadyTimes.TryGetValue(type, out float readyTime) ? readyTime : 0f;
        }
    }
}
