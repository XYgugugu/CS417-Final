using System;
using UnityEngine;

namespace PVZ3D.Core
{
    [Serializable]
    public class ResourceManager
    {
        [SerializeField] private int initCoins = 0;
        [SerializeField] private int coins;

        public int Coins => coins;

        public void Reset()
        {
            coins = initCoins;
        }

        public void EarnCoin()
        {
            EarnCoins(1);
        }

        public void EarnCoins(int amount)
        {
            if (amount <= 0) return;
            coins += amount;
        }

        public bool ExchangeCoin(int amount)
        {
            int coinsAfterTrade = coins - amount;
            if (coinsAfterTrade < 0)
            {
                return false;
            }

            coins = coinsAfterTrade;
            return true;
        }
    }
}
