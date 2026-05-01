using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PVZ3D.Plants
{
    public class PlantSceneBootstrap : MonoBehaviour
    {
        private static bool subscribedToSceneLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapActiveScene()
        {
            if (!subscribedToSceneLoaded)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                subscribedToSceneLoaded = true;
            }

            TryBootstrap(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBootstrap(scene);
        }

        private static void TryBootstrap(Scene scene)
        {
            if (scene.name != "Farm" || FindFirstObjectByType<PlantSceneBootstrap>() != null)
            {
                return;
            }

            GameObject root = new GameObject("Plant Scene Bootstrap");
            root.AddComponent<PlantSceneBootstrap>();
        }

        private IEnumerator Start()
        {
            yield return null;
            _ = PlantEconomy.Instance;
            SetupPlantingCells();
            SpawnPickupPlants();
        }

        private static void SetupPlantingCells()
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (!IsMudCell(t.name))
                {
                    continue;
                }

                if (t.GetComponent<PlantingCell>() == null)
                {
                    t.gameObject.AddComponent<PlantingCell>();
                }
            }
        }

        private static void SpawnPickupPlants()
        {
            Vector3 center = CalculatePlantFieldCenter(out bool foundCells, out float minX);
            Vector3 shelfCenter = foundCells
                ? new Vector3(minX - 1.4f, center.y + 0.25f, center.z)
                : new Vector3(-2.5f, 0.45f, 0f);

            PlantVisualFactory.CreatePickup(PlantType.Sunflower, shelfCenter + new Vector3(0f, 0f, -0.75f));
            PlantVisualFactory.CreatePickup(PlantType.Peashooter, shelfCenter);
            PlantVisualFactory.CreatePickup(PlantType.WallNut, shelfCenter + new Vector3(0f, 0f, 0.75f));
        }

        private static Vector3 CalculatePlantFieldCenter(out bool foundCells, out float minX)
        {
            PlantingCell[] cells = FindObjectsByType<PlantingCell>(FindObjectsSortMode.None);
            if (cells.Length == 0)
            {
                foundCells = false;
                minX = 0f;
                return Vector3.zero;
            }

            foundCells = true;
            Vector3 sum = Vector3.zero;
            minX = float.PositiveInfinity;
            for (int i = 0; i < cells.Length; i++)
            {
                Vector3 position = cells[i].transform.position;
                sum += position;
                minX = Mathf.Min(minX, position.x);
            }

            return sum / cells.Length;
        }

        private static bool IsMudCell(string objectName)
        {
            return objectName.StartsWith("Mud_");
        }
    }
}
