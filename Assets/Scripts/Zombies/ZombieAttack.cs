using PVZ3D.Plants;
using UnityEngine;

namespace PVZ3D.Zombies
{
    public class ZombieAttack : MonoBehaviour
    {
        [SerializeField] private ZombieBase zombie;
        [SerializeField] private PlantBase target;

        private float attackTimer;

        public void Initialize(ZombieBase owner)
        {
            zombie = owner;
        }

        public void SetTarget(PlantBase plant)
        {
            target = plant;
        }

        public void ClearTarget()
        {
            target = null;
            attackTimer = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (zombie == null || zombie.IsDead || target == null)
            {
                return;
            }

            if (target.IsDead)
            {
                ClearTarget();
                return;
            }

            attackTimer += deltaTime;
            if (attackTimer >= zombie.AttackInterval)
            {
                attackTimer = 0f;
                target.TakeDamage(zombie.AttackDamage);
            }
        }
    }
}
