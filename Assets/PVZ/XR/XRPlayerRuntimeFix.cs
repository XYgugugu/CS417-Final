using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace PVZ3D.XR
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-32000)]
    public class XRPlayerRuntimeFix : MonoBehaviour
    {
        [SerializeField] private string simulatorObjectName = "XR Interaction Simulator";
        [SerializeField] private string gravityProviderObjectName = "Gravity";
        [SerializeField] private float deviceModeFallbackHeight = 1.65f;
        [SerializeField] private bool disableGravityProvider = true;

        private static readonly List<InputDevice> HeadMountedDevices = new();

        private void Awake()
        {
            ApplyRuntimeFixes();
        }

        private IEnumerator Start()
        {
            yield return null;
            ApplyRuntimeFixes();

            yield return new WaitForSecondsRealtime(0.25f);
            ApplyRuntimeFixes();
        }

        private void ApplyRuntimeFixes()
        {
            if (!Application.isEditor || IsHeadsetActive())
            {
                DisableInteractionSimulator();
            }

            ConfigureXROrigin();

            if (disableGravityProvider)
            {
                DisableGravityProvider();
            }
        }

        private static bool IsHeadsetActive()
        {
            if (XRSettings.isDeviceActive)
            {
                return true;
            }

            HeadMountedDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeadMounted,
                HeadMountedDevices
            );

            for (int i = 0; i < HeadMountedDevices.Count; i++)
            {
                if (HeadMountedDevices[i].isValid)
                {
                    return true;
                }
            }

            return false;
        }

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
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

            if (xrOrigin.CameraFloorOffsetObject != null)
            {
                Transform cameraOffset = xrOrigin.CameraFloorOffsetObject.transform;
                Vector3 localPosition = cameraOffset.localPosition;
                localPosition.y = deviceModeFallbackHeight;
                cameraOffset.localPosition = localPosition;
            }
        }

        private void DisableGravityProvider()
        {
            Transform gravityProvider = FindDeepChild(transform, gravityProviderObjectName);
            if (gravityProvider != null)
            {
                gravityProvider.gameObject.SetActive(false);
            }
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
    }
}
