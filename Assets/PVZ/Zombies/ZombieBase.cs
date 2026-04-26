using System.Collections.Generic;
using PVZ3D.Plants;
using UnityEngine;

namespace PVZ3D.Zombies
{
    public class ZombieBase : MonoBehaviour
    {
        private static readonly HashSet<ZombieBase> ActiveZombies = new HashSet<ZombieBase>();

        [SerializeField] private int lane;
        [SerializeField] private float currentHealth = 100f;

        public int Lane => lane;
        public bool IsDead { get; private set; }

        private void OnEnable()
        {
            ActiveZombies.Add(this);
        }

        private void OnDisable()
        {
            ActiveZombies.Remove(this);
        }

        public void Configure(int laneIndex, float health)
        {
            lane = laneIndex;
            currentHealth = Mathf.Max(1f, health);
            IsDead = false;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                IsDead = true;
                Destroy(gameObject);
            }
        }

        public static ZombieBase GetFirstAliveInLaneAhead(int laneIndex, float xPosition, float maxDistance)
        {
            ZombieBase best = null;
            float bestDistance = float.PositiveInfinity;

            foreach (ZombieBase zombie in ActiveZombies)
            {
                if (zombie == null || zombie.IsDead || zombie.lane != laneIndex)
                {
                    continue;
                }

                float dx = zombie.transform.position.x - xPosition;
                if (dx <= 0f || dx > maxDistance)
                {
                    continue;
                }

                if (dx < bestDistance)
                {
                    bestDistance = dx;
                    best = zombie;
                }
            }

            return best;
        }
    }
}
