using System.Collections.Generic;
using PVZ3D.Core;
using UnityEngine;

namespace PVZ3D.Resources
{
    public class SunPickup : MonoBehaviour
    {
        private static readonly HashSet<SunPickup> Active = new HashSet<SunPickup>();

        [SerializeField] private int sunAmount = 25;
        [SerializeField] private float lifetime = 12f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 2.8f;
        [SerializeField] private float spinSpeed = 60f;
        [SerializeField] private float visualScaleMultiplier = 1f;

        private Vector3 startPos;
        private float spawnTime;
        private bool collected;

        private void OnEnable()
        {
            EnsureRuntimeParent();
            Active.Add(this);
            startPos = transform.position;
            spawnTime = Time.time;
            EnsureCollider();
            EnsureVisual();
            ApplyAmountVisuals();
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Update()
        {
            transform.position = startPos + Vector3.up * (Mathf.Sin((Time.time - spawnTime) * bobSpeed) * bobAmplitude);
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            if (!collected && Time.time - spawnTime >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        public void Configure(int amount)
        {
            sunAmount = Mathf.Max(1, amount);
            ApplyAmountVisuals();
        }

        public void Collect()
        {
            if (collected)
            {
                return;
            }

            collected = true;
            ResourceManager.Instance?.AddSun(sunAmount, true);
            GameEvents.RaisePurchaseResult(true, "Sun collected");
            SpawnCollectFlash(new Color(1f, 0.96f, 0.62f));
            Destroy(gameObject);
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

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(transform, false);
            core.transform.localScale = new Vector3(0.68f, 0.68f, 0.68f);
            Renderer renderer = core.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(1f, 0.88f, 0.2f));
            }

            Collider col = core.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }

        private void SpawnCollectFlash(Color color)
        {
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.transform.position = transform.position;
            flash.transform.localScale = Vector3.one * (0.35f * visualScaleMultiplier);
            Renderer renderer = flash.GetComponent<Renderer>();
            if (renderer != null)
            {
                RuntimeVisualMaterialUtility.ApplyColor(renderer, color);
            }

            Collider col = flash.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            Destroy(flash, 0.15f);
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

        private void ApplyAmountVisuals()
        {
            float extraScale = Mathf.Clamp01((sunAmount - 25f) / 25f);
            visualScaleMultiplier = 1f + (extraScale * 0.45f);
            transform.localScale = Vector3.one * visualScaleMultiplier;

            SphereCollider col = GetComponent<SphereCollider>();
            if (col != null)
            {
                col.radius = 0.3f * visualScaleMultiplier;
            }

            if (sunAmount >= 50)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null)
                    {
                        RuntimeVisualMaterialUtility.ApplyColor(renderer, new Color(1f, 0.94f, 0.34f));
                    }
                }
            }
        }
    }
}
