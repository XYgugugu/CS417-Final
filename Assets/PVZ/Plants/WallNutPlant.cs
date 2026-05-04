using UnityEngine;

namespace PVZ3D.Plants
{
    public class WallNutPlant : PlantBase
    {
        protected override void Awake()
        {
            base.Awake();
            InitializePlant(
                800f,
                new Vector3(0f, 0.5f, 0f) * PlantVisualUtility.PrefabScale,
                Vector3.one * PlantVisualUtility.PrefabScale,
                new Color(0.68f, 0.4f, 0.16f, 1f),
                new Color(0.9f, 0.62f, 0.28f, 0.65f),
                PlantVisualKind.WallNut);
        }
    }
}
