using PVZ3D.Core;
using PVZ3D.Resources;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class SunflowerPlant : PlantBase
    {
        private float timer;

        private void Update()
        {
            if (IsDead || definition == null)
            {
                return;
            }

            GamePhase phase = GameManager.Instance != null ? GameManager.Instance.State.Phase : GamePhase.Menu;
            if (phase != GamePhase.Battle && phase != GamePhase.Prep)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer >= definition.SunDropInterval)
            {
                timer = 0f;
                SunSpawner.SpawnSunAt(transform.position + Vector3.up * 1.2f, definition.SunPerDrop);
            }
        }
    }
}
