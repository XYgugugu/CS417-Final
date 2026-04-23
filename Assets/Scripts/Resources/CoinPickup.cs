using System.Collections.Generic;
using PVZ3D.Core;
using UnityEngine;

namespace PVZ3D.Resources
{
    public class CoinPickup : MonoBehaviour
    {
        private static readonly HashSet<CoinPickup> Active = new HashSet<CoinPickup>();

        [SerializeField] private int coinAmount = 1;
        [SerializeField] private float lifetime = 10f;
        [SerializeField] private float autoCollectDistance = 1.2f;
        [SerializeField] private float spinSpeed = 120f;
        [SerializeField] private Color coinColor = new Color(1f, 0.86f, 0.25f);

        private float spawnTime;
        private bool collected;

        private void OnEnable()
        {
            EnsureRuntimeParent();
            Active.Add(this);
            spawnTime = Time.time;
            EnsureCollider();
            EnsureVisual();
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            Camera cam = Camera.main;
            if (cam != null && Vector3.Distance(cam.transform.position, transform.position) <= autoCollectDistance)
            {
                Collect();
            }

            if (!collected && Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!collected && IsPlayerCollector(other))
            {
                Collect();
            }
        }

        public void Configure(int amount)
        {
            coinAmount = Mathf.Max(1, amount);
        }

        public void Collect()
        {
            if (collected)
            {
                return;
            }

            collected = true;
            ResourceManager.Instance?.AddCoins(coinAmount, true);
            GameEvents.RaisePurchaseResult(true, $"+{coinAmount} coin");
            SpawnCollectFlash();
            Destroy(gameObject);
        }

        public static void DestroyAll()
        {
            CoinPickup[] picks = new CoinPickup[Active.Count];
            Active.CopyTo(picks);
            foreach (CoinPickup pickup in picks)
            {
                if (pickup != null)
                {
                    Destroy(pickup.gameObject);
                }
            }
        }

        private void EnsureCollider()
        {
            SphereCollider col = GetComponent<SphereCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<SphereCollider>();
            }

            col.radius = 0.3f;
            col.isTrigger = true;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.isKinematic = true;
        }

        private void EnsureVisual()
        {
            Renderer rootRenderer = GetComponent<Renderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            if (transform.childCount > 0)
            {
                return;
            }

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.28f, 0.05f, 0.28f);
            Renderer renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, coinColor);
            }

            Collider col = body.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        private void SpawnCollectFlash()
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flash.transform.position = transform.position;
            flash.transform.localScale = new Vector3(0.26f, 0.02f, 0.26f);
            Renderer renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(1f, 0.94f, 0.62f));
            }

            Collider col = flash.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            Destroy(flash, 0.12f);
        }

        private static bool IsPlayerCollector(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            return other.GetComponentInParent<Camera>() != null;
        }

        private void EnsureRuntimeParent()
        {
            GameObject root = GameObject.Find("Runtime/Pickups");
            if (root == null)
            {
                GameObject runtime = GameObject.Find("Runtime") ?? new GameObject("Runtime");
                root = new GameObject("Pickups");
                root.transform.SetParent(runtime.transform, false);
            }

            if (transform.parent != root.transform)
            {
                transform.SetParent(root.transform, true);
            }
        }
    }
}
