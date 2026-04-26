using PVZ3D.Core;
using UnityEngine;

namespace PVZ3D.Resources
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private int currentSun;
        [SerializeField] private int currentCoins;

        public int CurrentSun => currentSun;
        public int CurrentCoins => currentCoins;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Initialize(int sun, int coins)
        {
            currentSun = Mathf.Max(0, sun);
            currentCoins = Mathf.Max(0, coins);
            GameEvents.RaiseSunChanged(currentSun);
            GameEvents.RaiseCoinsChanged(currentCoins);
        }

        public void AddSun(int amount, bool countsAsCollected = true)
        {
            if (amount <= 0)
            {
                return;
            }

            currentSun += amount;
            if (countsAsCollected)
            {
                GameManager.Instance?.AddCollectedSunStat(amount);
                GameEvents.RaiseResourceCollected("Sun", amount);
            }

            GameEvents.RaiseSunChanged(currentSun);
        }

        public bool SpendSun(int amount)
        {
            if (amount < 0 || !CanAffordSun(amount))
            {
                return false;
            }

            currentSun -= amount;
            GameEvents.RaiseResourceSpent("Sun", amount);
            GameEvents.RaiseSunChanged(currentSun);
            return true;
        }

        public bool CanAffordSun(int amount)
        {
            return amount >= 0 && currentSun >= amount;
        }

        public void AddCoins(int amount, bool countsAsEarned = true)
        {
            if (amount <= 0)
            {
                return;
            }

            currentCoins += amount;
            if (countsAsEarned)
            {
                GameManager.Instance?.AddEarnedCoinsStat(amount);
                GameEvents.RaiseResourceCollected("Coin", amount);
            }

            GameEvents.RaiseCoinsChanged(currentCoins);
        }

        public bool SpendCoins(int amount)
        {
            if (amount < 0 || !CanAffordCoins(amount))
            {
                return false;
            }

            currentCoins -= amount;
            GameEvents.RaiseResourceSpent("Coin", amount);
            GameEvents.RaiseCoinsChanged(currentCoins);
            return true;
        }

        public bool CanAffordCoins(int amount)
        {
            return amount >= 0 && currentCoins >= amount;
        }
    }
}
