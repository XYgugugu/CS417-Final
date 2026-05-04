using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace PVZ3D.XR
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-32000)]
    public class XRPlayerRuntimeFix : MonoBehaviour
    {
        [SerializeField] private string simulatorObjectName = "XR Interaction Simulator";
        [SerializeField] private float deviceModeFallbackHeight = 1.65f;

        private void Awake()
        {
#if !UNITY_EDITOR
            DisableInteractionSimulator();
            ConfigureXROrigin();
#endif
        }

        private IEnumerator Start()
        {
#if !UNITY_EDITOR
            yield return null;
            ConfigureXROrigin();
#else
            yield break;
#endif
        }

#if !UNITY_EDITOR
        private void DisableInteractionSimulator()
        {
            Transform simulator = FindDeepChild(transform, simulatorObjectName);
            if (simulator != null)
            {
                simulator.gameObject.SetActive(false);
            }
        }

        private void ConfigureXROrigin()
        {
            XROrigin xrOrigin = GetComponentInChildren<XROrigin>(true);
            if (xrOrigin == null) return;

            xrOrigin.CameraYOffset = deviceModeFallbackHeight;
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        }

        private Transform FindDeepChild(Transform root, string childName)
        {
            if (root.name == childName) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindDeepChild(root.GetChild(i), childName);
                if (match != null) return match;
            }

            return null;
        }
#endif
    }
}
