using System;
using UnityEngine;

namespace PVZ3D.Core
{
    public enum PlantType
    {
        SunFlower,
        PeaShooter,
        WallNut
    }

    [Serializable]
    public class PlantsEconomy
    {
        [SerializeField] private int initSun = 100;
        [SerializeField] private int sun = 0;
        [SerializeField] private float sunFlowerCooldownRemain;
        [SerializeField] private float peaShooterCooldownRemain;
        [SerializeField] private float wallNutCooldownRemain;

        public int Sun => sun;

        public void Awake()
        {
            Reset();
        }

        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            sunFlowerCooldownRemain = TickCooldown(sunFlowerCooldownRemain, deltaTime);
            peaShooterCooldownRemain = TickCooldown(peaShooterCooldownRemain, deltaTime);
            wallNutCooldownRemain = TickCooldown(wallNutCooldownRemain, deltaTime);
        }

        public void Reset()
        {
            sun = initSun;
            sunFlowerCooldownRemain = 0f;
            peaShooterCooldownRemain = 0f;
            wallNutCooldownRemain = 0f;
        }

        public void CollectSun(int amount)
        {
            if (amount <= 0) {return;}
            sun += amount;
        }

        public bool ExchangePlant(PlantType plantType)
        {
            if (!IsPlantReady(plantType))
            {
                return false;
            }

            if (!ExchangeSun(GetPlantCost(plantType)))
            {
                return false;
            }

            SetCooldownRemain(plantType, GetPlantCooldownDuration(plantType));
            return true;
        }

        public bool CanExchangePlant(PlantType plantType)
        {
            return IsPlantReady(plantType) && sun >= GetPlantCost(plantType);
        }

        public bool IsPlantReady(PlantType plantType)
        {
            return GetPlantCooldownRemaining(plantType) <= 0f;
        }

        public int GetPlantCost(PlantType plantType)
        {
            return plantType switch
            {
                PlantType.SunFlower => 50,
                PlantType.PeaShooter => 100,
                PlantType.WallNut => 50,
                _ => 0
            };
        }

        public float GetPlantCooldownDuration(PlantType plantType)
        {
            return plantType switch
            {
                PlantType.SunFlower => 5f,
                PlantType.PeaShooter => 5f,
                PlantType.WallNut => 15f,
                _ => 0f
            };
        }

        public float GetPlantCooldownRemaining(PlantType plantType)
        {
            return plantType switch
            {
                PlantType.SunFlower => sunFlowerCooldownRemain,
                PlantType.PeaShooter => peaShooterCooldownRemain,
                PlantType.WallNut => wallNutCooldownRemain,
                _ => 0f
            };
        }

        private bool ExchangeSun(int amount)
        {
            int sunAfterTrade = sun - amount;
            if (sunAfterTrade < 0) {return false;}
            sun = sunAfterTrade;
            return true;
        }

        private void SetCooldownRemain(PlantType plantType, float value)
        {
            value = Mathf.Max(0f, value);
            switch (plantType)
            {
                case PlantType.SunFlower:
                    sunFlowerCooldownRemain = value;
                    break;
                case PlantType.PeaShooter:
                    peaShooterCooldownRemain = value;
                    break;
                case PlantType.WallNut:
                    wallNutCooldownRemain = value;
                    break;
            }
        }

        private static float TickCooldown(float current, float deltaTime)
        {
            return current > 0f ? Mathf.Max(0f, current - deltaTime) : 0f;
        }
    }
}
