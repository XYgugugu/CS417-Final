using PVZ3D.Resources;
using UnityEngine;
using UnityEngine.XR;

namespace PVZ3D.Interaction
{
    public class XRPickupSweepCollector : MonoBehaviour
    {
        [Header("Sweep Collect")]
        [SerializeField] private bool enableSunSweepCollect = true;
        [SerializeField] private bool enableCoinSweepCollect = true;
        [SerializeField] private float maxRayDistance = 18f;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private bool requireButtonHold;

        [Header("Button Mapping")]
        [Tooltip("Use side Grip button (recommended on Quest).")]
        [SerializeField] private bool useGripButton = true;
        [Tooltip("Fallback button: A/X.")]
        [SerializeField] private bool usePrimaryButtonFallback = true;
        [Tooltip("Fallback button: B/Y.")]
        [SerializeField] private bool useSecondaryButtonFallback = true;

        private InputDevice leftDevice;
        private InputDevice rightDevice;
        private Transform leftRayOrigin;
        private Transform rightRayOrigin;
        private float nextRefreshTime;

        private void Awake()
        {
            // User-facing default: sweep-to-collect without holding any button.
            requireButtonHold = false;
            enableSunSweepCollect = true;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + 1f;
                RefreshDevicesAndRayOrigins();
            }

            bool leftHeld = !requireButtonHold || IsCollectButtonHeld(ref leftDevice, XRNode.LeftHand);
            bool rightHeld = !requireButtonHold || IsCollectButtonHeld(ref rightDevice, XRNode.RightHand);

            if (leftHeld)
            {
                TryCollectFromHand(ref leftDevice, XRNode.LeftHand, leftRayOrigin);
            }

            if (rightHeld)
            {
                TryCollectFromHand(ref rightDevice, XRNode.RightHand, rightRayOrigin);
            }
        }

        private void RefreshDevicesAndRayOrigins()
        {
            if (!leftDevice.isValid)
            {
                leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            }

            if (!rightDevice.isValid)
            {
                rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }

            if (leftRayOrigin == null || rightRayOrigin == null)
            {
                Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    if (t == null)
                    {
                        continue;
                    }

                    string n = t.name;
                    if (leftRayOrigin == null && n.Contains("Left Controller"))
                    {
                        leftRayOrigin = t;
                    }
                    else if (rightRayOrigin == null && n.Contains("Right Controller"))
                    {
                        rightRayOrigin = t;
                    }

                    if (leftRayOrigin != null && rightRayOrigin != null)
                    {
                        break;
                    }
                }
            }
        }

        private bool IsCollectButtonHeld(ref InputDevice device, XRNode node)
        {
            if (!device.isValid)
            {
                device = InputDevices.GetDeviceAtXRNode(node);
            }

            if (!device.isValid)
            {
                return false;
            }

            if (useGripButton && device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripHeld) && gripHeld)
            {
                return true;
            }

            if (usePrimaryButtonFallback && device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryHeld) && primaryHeld)
            {
                return true;
            }

            if (useSecondaryButtonFallback && device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryHeld) && secondaryHeld)
            {
                return true;
            }

            return false;
        }

        private void TryCollectFromHand(ref InputDevice device, XRNode node, Transform fallbackOrigin)
        {
            if (!device.isValid)
            {
                device = InputDevices.GetDeviceAtXRNode(node);
            }

            Vector3 origin;
            Vector3 dir;

            Vector3 devicePos = Vector3.zero;
            Quaternion deviceRot = Quaternion.identity;
            bool hasPose = device.isValid &&
                           device.TryGetFeatureValue(CommonUsages.devicePosition, out devicePos) &&
                           device.TryGetFeatureValue(CommonUsages.deviceRotation, out deviceRot);

            if (hasPose)
            {
                origin = devicePos;
                dir = deviceRot * Vector3.forward;
            }
            else if (fallbackOrigin != null)
            {
                origin = fallbackOrigin.position;
                dir = fallbackOrigin.forward;
            }
            else
            {
                return;
            }

            Ray ray = new Ray(origin, dir);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastMask, QueryTriggerInteraction.Collide))
            {
                return;
            }

            if (enableSunSweepCollect)
            {
                SunPickup sun = hit.collider.GetComponentInParent<SunPickup>();
                if (sun != null)
                {
                    sun.Collect();
                    return;
                }
            }

            if (enableCoinSweepCollect)
            {
                CoinPickup coin = hit.collider.GetComponentInParent<CoinPickup>();
                if (coin != null)
                {
                    coin.Collect();
                }
            }
        }
    }
}
