using System;
using UnityEngine;

namespace PVZ3D.Core
{
    [Serializable]
    public class PlayerManager
    {
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int hp = 100;

        private Action onPlayerDead;

        public int MaxHP => maxHP;
        public int HP => hp;

        private void Awake()
        {
            hp = maxHP;
        }

        public void Initialize(Action onPlayerDead)
        {
            this.onPlayerDead = onPlayerDead;
        }

        public void SetHealth(int value)
        {
            hp = Mathf.Clamp(value, 0, maxHP);

            if (hp <= 0)
            {
                onPlayerDead?.Invoke();
            }
        }

        public void GainHealth(int value)
        {
            if (value <= 0) return;

            hp = Mathf.Min(maxHP, hp + value);
        }

        public void LoseHealth(int value)
        {
            if (value <= 0) return;

            hp = Mathf.Max(0, hp - value);
            Debug.Log($"HP: {hp}/{maxHP} - Lost {value}.");

            if (hp <= 0)
            {
                onPlayerDead?.Invoke();
            }
        }
    }
}
