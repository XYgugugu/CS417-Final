using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Plants;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PVZ3D.Grid
{
    public class LawnGridManager : MonoBehaviour
    {
        public static LawnGridManager Instance { get; private set; }

        [Header("Grid Layout")]
        [SerializeField] private int lanes = 5;
        [SerializeField] private int columns = 7;
        [SerializeField] private float columnSpacing = 1.6f;
        [SerializeField] private float laneSpacing = 1.35f;
        [SerializeField] private Vector3 origin = new Vector3(0f, 0f, 0f);

        [Header("Visual Prefabs")]
        [SerializeField] private GameObject cellTilePrefab;
        [SerializeField] private Transform gridParent;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Color laneStripeColor = new Color(0.12f, 0.26f, 0.14f);
        [SerializeField] private Color spawnMarkerColor = new Color(0.44f, 0.16f, 0.16f);
        [SerializeField] private Color laneLabelColor = new Color(0.92f, 0.96f, 0.93f);

        [Header("Base")]
        [SerializeField] private float baseXOffset = -1.6f;
        [SerializeField] private float spawnXOffset = 2f;

        private GridCell[,] cells;
        private readonly List<Transform> laneSpawnPoints = new List<Transform>();

        public int Lanes => lanes;
        public int Columns => columns;
        public float BaseX => origin.x + baseXOffset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            BuildGridIfNeeded();
        }

        public void BuildGridIfNeeded()
        {
            if (TryBindExistingLayout())
            {
                return;
            }

            if (cells != null && cells.Length > 0)
            {
                return;
            }

            if (gridParent == null)
            {
                GameObject root = new GameObject("GridCells");
                root.transform.SetParent(transform, false);
                gridParent = root.transform;
            }

            if (spawnParent == null)
            {
                GameObject root = new GameObject("LaneSpawns");
                root.transform.SetParent(transform, false);
                spawnParent = root.transform;
            }

            cells = new GridCell[lanes, columns];
            laneSpawnPoints.Clear();

            for (int lane = 0; lane < lanes; lane++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Vector3 world = GetCellPosition(lane, col);
                    GameObject tile = CreateCellObject(world);
                    tile.name = $"Cell_L{lane}_C{col}";
                    tile.transform.SetParent(gridParent, true);
                    GridCell gridCell = tile.GetComponent<GridCell>();
                    gridCell.Initialize(lane, col);
                    cells[lane, col] = gridCell;
                }

                GameObject spawn = new GameObject($"Spawn_Lane_{lane}");
                spawn.transform.SetParent(spawnParent, false);
                spawn.transform.position = new Vector3(GetColumnX(columns - 1) + spawnXOffset, 0f, GetLaneZ(lane));
                CreateSpawnMarkerVisual(spawn.transform);
                laneSpawnPoints.Add(spawn.transform);
                CreateLaneIndicator(lane);
            }
        }

        public void RebuildGridForAuthoring()
        {
            cells = null;
            laneSpawnPoints.Clear();

            if (gridParent == null)
            {
                Transform existingGridParent = transform.Find("GridCells");
                if (existingGridParent != null)
                {
                    gridParent = existingGridParent;
                }
            }

            if (spawnParent == null)
            {
                Transform existingSpawnParent = transform.Find("LaneSpawns");
                if (existingSpawnParent != null)
                {
                    spawnParent = existingSpawnParent;
                }
            }

            if (gridParent != null)
            {
                ClearChildrenImmediate(gridParent);
            }

            if (spawnParent != null)
            {
                ClearChildrenImmediate(spawnParent);
            }

            BuildGridIfNeeded();
        }

        private bool TryBindExistingLayout()
        {
            if (gridParent == null)
            {
                Transform existingGridParent = transform.Find("GridCells");
                if (existingGridParent != null)
                {
                    gridParent = existingGridParent;
                }
            }

            if (spawnParent == null)
            {
                Transform existingSpawnParent = transform.Find("LaneSpawns");
                if (existingSpawnParent != null)
                {
                    spawnParent = existingSpawnParent;
                }
            }

            if (gridParent == null || spawnParent == null)
            {
                return false;
            }

            GridCell[,] existingCells = new GridCell[lanes, columns];
            for (int lane = 0; lane < lanes; lane++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Transform cellTransform = gridParent.Find($"Cell_L{lane}_C{col}");
                    if (cellTransform == null)
                    {
                        return false;
                    }

                    GridCell cell = cellTransform.GetComponent<GridCell>();
                    if (cell == null)
                    {
                        return false;
                    }

                    existingCells[lane, col] = cell;
                    cell.Initialize(lane, col);
                }
            }

            List<Transform> existingSpawns = new List<Transform>(lanes);
            for (int lane = 0; lane < lanes; lane++)
            {
                Transform spawnTransform = spawnParent.Find($"Spawn_Lane_{lane}");
                if (spawnTransform == null)
                {
                    return false;
                }

                existingSpawns.Add(spawnTransform);
            }

            cells = existingCells;
            laneSpawnPoints.Clear();
            laneSpawnPoints.AddRange(existingSpawns);
            return true;
        }

        private static void ClearChildrenImmediate(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
                else
#endif
                {
                    Object.Destroy(child.gameObject);
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Authoring/Rebuild Grid")]
        private void ContextRebuildGrid()
        {
            if (Application.isPlaying)
            {
                return;
            }

            RebuildGridForAuthoring();
            EditorUtility.SetDirty(gameObject);
        }
#endif

        public Vector3 GetCellPosition(int lane, int col)
        {
            return new Vector3(GetColumnX(col), origin.y, GetLaneZ(lane));
        }

        public float GetLaneZ(int lane)
        {
            float centerOffset = (lanes - 1) * 0.5f;
            return origin.z + (lane - centerOffset) * laneSpacing;
        }

        public float GetColumnX(int column)
        {
            return origin.x + (column * columnSpacing);
        }

        public GridCell GetCell(int lane, int col)
        {
            if (cells == null)
            {
                return null;
            }

            if (lane < 0 || lane >= lanes || col < 0 || col >= columns)
            {
                return null;
            }

            return cells[lane, col];
        }

        public GridCell GetFirstEmptyCellInLane(int lane)
        {
            if (lane < 0 || lane >= lanes || cells == null)
            {
                return null;
            }

            for (int col = 0; col < columns; col++)
            {
                GridCell cell = cells[lane, col];
                if (cell != null && cell.CanPlacePlant())
                {
                    return cell;
                }
            }

            return null;
        }

        public Vector3 GetZombieSpawnPosition(int lane)
        {
            if (lane < 0 || lane >= laneSpawnPoints.Count)
            {
                lane = Mathf.Clamp(lane, 0, laneSpawnPoints.Count - 1);
            }

            if (lane >= 0 && lane < laneSpawnPoints.Count)
            {
                return laneSpawnPoints[lane].position;
            }

            return new Vector3(GetColumnX(columns - 1) + spawnXOffset, 0f, GetLaneZ(0));
        }

        public PlantBase GetBlockingPlant(int lane, float zombieX, float engageDistance)
        {
            if (cells == null || lane < 0 || lane >= lanes)
            {
                return null;
            }

            PlantBase best = null;
            float bestX = float.NegativeInfinity;

            for (int col = 0; col < columns; col++)
            {
                GridCell cell = cells[lane, col];
                if (cell == null || cell.Occupant == null)
                {
                    continue;
                }

                float plantX = cell.transform.position.x;
                if (plantX <= zombieX + 0.1f && zombieX - plantX <= engageDistance)
                {
                    if (plantX > bestX)
                    {
                        bestX = plantX;
                        best = cell.Occupant;
                    }
                }
            }

            return best;
        }

        public Vector3 GetBasePositionForLane(int lane)
        {
            return new Vector3(BaseX, 0f, GetLaneZ(Mathf.Clamp(lane, 0, lanes - 1)));
        }

        private GameObject CreateCellObject(Vector3 worldPosition)
        {
            GameObject obj;
            if (cellTilePrefab != null)
            {
                obj = Instantiate(cellTilePrefab, worldPosition, Quaternion.identity);
            }
            else
            {
                obj = new GameObject("GridCell");
                obj.transform.position = worldPosition;

                GameObject baseTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseTile.transform.SetParent(obj.transform, false);
                baseTile.transform.localPosition = Vector3.zero;
                baseTile.transform.localScale = new Vector3(1.3f, 0.1f, 1.1f);
                Renderer baseRenderer = baseTile.GetComponent<Renderer>();
                if (baseRenderer != null)
                {
                    RuntimeVisualMaterialUtility.ApplyColor(baseRenderer, new Color(0.2f, 0.44f, 0.2f));
                }

                GameObject topTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topTile.transform.SetParent(obj.transform, false);
                topTile.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                topTile.transform.localScale = new Vector3(1.18f, 0.02f, 0.98f);
                Renderer topRenderer = topTile.GetComponent<Renderer>();
                if (topRenderer != null)
                {
                    RuntimeVisualMaterialUtility.ApplyColor(topRenderer, new Color(0.34f, 0.66f, 0.29f));
                }

                Collider baseCollider = baseTile.GetComponent<Collider>();
                if (baseCollider != null)
                {
                    baseCollider.enabled = false;
                }

                Collider topCollider = topTile.GetComponent<Collider>();
                if (topCollider != null)
                {
                    topCollider.enabled = false;
                }
            }

            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<BoxCollider>();
            }

            if (obj.GetComponent<GridCell>() == null)
            {
                obj.AddComponent<GridCell>();
            }

            return obj;
        }

        private void CreateSpawnMarkerVisual(Transform parent)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            marker.transform.localScale = new Vector3(0.24f, 0.05f, 0.24f);
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, spawnMarkerColor);
            }

            Collider col = marker.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        private void CreateLaneIndicator(int lane)
        {
            GameObject laneStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            laneStrip.name = $"Lane_{lane + 1}_Strip";
            laneStrip.transform.SetParent(gridParent, false);
            laneStrip.transform.position = new Vector3(GetColumnX((columns - 1) / 2), -0.03f, GetLaneZ(lane));
            laneStrip.transform.localScale = new Vector3(columns * columnSpacing + 1.4f, 0.02f, 0.06f);
            Renderer stripRenderer = laneStrip.GetComponent<Renderer>();
            if (stripRenderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(stripRenderer, laneStripeColor);
            }

            Collider stripCol = laneStrip.GetComponent<Collider>();
            if (stripCol != null)
            {
                stripCol.enabled = false;
            }

            GameObject labelObj = new GameObject($"Lane_{lane + 1}_Label");
            labelObj.transform.SetParent(gridParent, false);
            labelObj.transform.position = new Vector3(GetColumnX(0) - 0.75f, 0.2f, GetLaneZ(lane));
            TextMesh label = labelObj.AddComponent<TextMesh>();
            label.text = $"L{lane + 1}";
            label.fontSize = 52;
            label.characterSize = 0.045f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = laneLabelColor;
            labelObj.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
        }
    }
}
