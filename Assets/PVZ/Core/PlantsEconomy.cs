using System;
using UnityEngine;

namespace PVZ3D.Core
{
    [Serializable]
    public class PlantsEconomy
    {
        [SerializeField] private int initSun = 100;
        [SerializeField] private int sun = 0;

        public int Sun => sun;

        public void Awake()
        {
            Reset();
        }

        public void Reset()
        {
            sun = initSun;
        }

        public void collectSun(int amount)
        {
            if (amount <= 0) {return;}
            sun += amount;
        }

        public bool exchangeSun(int amount)
        {
            int sunAfterTrade = sun - amount;
            if (sunAfterTrade < 0) {return false;}
            sun = sunAfterTrade;
            return true;
        }
    }
}