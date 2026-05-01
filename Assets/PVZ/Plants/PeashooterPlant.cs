using System.Collections;
using UnityEngine;

namespace PVZ3D.Plants
{
    public class PeashooterPlant : MonoBehaviour
    {
        [SerializeField] private float fireInterval = 1f;
        [SerializeField] private Vector3 muzzleOffset = new Vector3(0.45f, 0.65f, 0f);
        [SerializeField] private float repeaterSecondShotDelay = 0.5f;
        [SerializeField] private bool isRepeater;

        private float nextFireTime;
        private Coroutine firingRoutine;
        private GameObject upgradeHalo;

        public bool CanUpgradeToRepeater => !isRepeater;

        private void OnEnable()
        {
            nextFireTime = Time.time + fireInterval;
        }

        private void Update()
        {
            if (Time.time < nextFireTime)
            {
                return;
            }

            nextFireTime = Time.time + fireInterval;
            if (firingRoutine == null)
            {
                firingRoutine = StartCoroutine(FireBurst());
            }
        }

        private IEnumerator FireBurst()
        {
            FirePea();

            if (isRepeater)
            {
                yield return new WaitForSeconds(repeaterSecondShotDelay);
                FirePea();
            }

            firingRoutine = null;
        }

        private void FirePea()
        {
            PeaProjectile.Create(transform.position + muzzleOffset, Vector3.right);
        }

        public bool TryUpgradeToRepeater()
        {
            if (!CanUpgradeToRepeater)
            {
                return false;
            }

            isRepeater = true;
            ApplyRepeaterVisual();
            return true;
        }

        private void ApplyRepeaterVisual()
        {
            transform.localScale = Vector3.one * 1.18f;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material material = renderer.material;
                material.color = Color.Lerp(material.color, new Color(0.05f, 1f, 0.25f), 0.55f);
            }

            upgradeHalo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upgradeHalo.name = "Repeater Upgrade Ring";
            upgradeHalo.transform.SetParent(transform, false);
            upgradeHalo.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            upgradeHalo.transform.localScale = new Vector3(0.55f, 0.015f, 0.55f);

            Renderer haloRenderer = upgradeHalo.GetComponent<Renderer>();
            if (haloRenderer != null)
            {
                haloRenderer.material.color = new Color(1f, 0.88f, 0.12f);
            }

            Collider haloCollider = upgradeHalo.GetComponent<Collider>();
            if (haloCollider != null)
            {
                Destroy(haloCollider);
            }
        }
    }
}
