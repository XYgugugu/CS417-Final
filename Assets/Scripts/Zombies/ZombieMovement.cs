using PVZ3D.Grid;
using PVZ3D.Plants;
using UnityEngine;

namespace PVZ3D.Zombies
{
    public class ZombieMovement : MonoBehaviour
    {
        [SerializeField] private ZombieBase zombie;
        [SerializeField] private float engageDistance = 0.65f;

        public void Initialize(ZombieBase owner)
        {
            zombie = owner;
        }

        public void Tick(float deltaTime)
        {
            if (zombie == null || zombie.IsDead)
            {
                return;
            }

            PlantBase target = LawnGridManager.Instance != null
                ? LawnGridManager.Instance.GetBlockingPlant(zombie.Lane, zombie.transform.position.x, engageDistance)
                : null;

            if (target != null)
            {
                zombie.SetAttackTarget(target);
                return;
            }

            zombie.ClearAttackTarget();
            zombie.transform.position += Vector3.left * (zombie.MoveSpeed * deltaTime);

            if (zombie.transform.position.x <= zombie.BaseTargetX)
            {
                zombie.ReachBase();
            }
        }
    }
}
