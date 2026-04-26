using System.Collections.Generic;
using PVZ3D.Core;
using PVZ3D.Plants;
using UnityEngine;

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
        [SerializeField] private Vector3 origin = Vector3.zero;

        [Header("Visuals")]
        [SerializeField] private Transform gridParent;
        [SerializeField] private Color laneStripeColor = new Color(0.12f, 0.26f, 0.14f);

        private GridCell[,] cells;

        public int Lanes => lanes;
        public int Columns => columns;

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

            cells = new GridCell[lanes, columns];

            for (int lane = 0; lane < lanes; lane++)
            {
                CreateLaneStripe(lane);
                for (int col = 0; col < columns; col++)
                {
                    Vector3 position = GetCellPosition(lane, col);
                    GameObject tile = CreateCellObject(position);
                    tile.name = $"Cell_L{lane}_C{col}";
                    tile.transform.SetParent(gridParent, true);

                    GridCell cell = tile.GetComponent<GridCell>();
                    cell.Initialize(lane, col);
                    cells[lane, col] = cell;
                }
            }
        }

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
            if (cells == null || lane < 0 || lane >= lanes || col < 0 || col >= columns)
            {
                return null;
            }

            return cells[lane, col];
        }

        public GridCell GetFirstEmptyCellInLane(int lane)
        {
            if (cells == null || lane < 0 || lane >= lanes)
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

        private GameObject CreateCellObject(Vector3 worldPosition)
        {
            GameObject obj = new GameObject("GridCell");
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
                RuntimeVisualMaterialUtility.ApplyColor(topRenderer, new Color(0.28f, 0.56f, 0.24f));
            }

            return obj.AddComponent<GridCell>().gameObject;
        }

        private void CreateLaneStripe(int lane)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = $"LaneStripe_{lane}";
            stripe.transform.SetParent(gridParent != null ? gridParent : transform, false);
            stripe.transform.position = new Vector3(GetColumnX(columns / 2), -0.06f, GetLaneZ(lane));
            stripe.transform.localScale = new Vector3(columns * columnSpacing, 0.01f, 1.22f);

            Renderer renderer = stripe.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, laneStripeColor);
            }
        }
    }
}
