using UnityEngine;
using UnityEngine.XR;

namespace PVZ3D.Interaction
{
    public class XRFlightLocomotionController : MonoBehaviour
    {
        [Header("Enable")]
        [SerializeField] private bool enableOnDevice = true;
        [SerializeField] private bool enableInEditor = true;

        [Header("Left Stick Move (No Flight)")]
        [SerializeField] private float moveSpeed = 2.2f;

        [Header("Right Stick Look")]
        [SerializeField] private float yawSpeed = 105f;
        [SerializeField] private bool comfortMode = true;
        [SerializeField] private bool enablePitchFromRightStick = false;
        [SerializeField] private float pitchSpeed = 55f;
        [SerializeField] private float deadZone = 0.03f;
        [SerializeField] private float stickSmoothing = 14f;
        [SerializeField] private float maxPitchAngle = 16f;

        private InputDevice leftDevice;
        private InputDevice rightDevice;
        private Camera headCamera;
        private Transform movementRoot;
        private Transform lookPivot;
        private bool locomotionProvidersDisabled;
        private Vector2 filteredLeftStick;
        private Vector2 filteredRightStick;
        private float pitchAngle;

        private void Start()
        {
            ApplyComfortDefaults();
            DisableConflictingLocomotionProviders();
            RefreshCamera();
            RefreshDevices(force: true);
            ResolveLookTargets();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Application.isEditor && !enableInEditor)
            {
                return;
            }

            if (!Application.isEditor && !enableOnDevice)
            {
                return;
            }

            if (headCamera == null)
            {
                RefreshCamera();
            }

            if (!locomotionProvidersDisabled)
            {
                DisableConflictingLocomotionProviders();
            }

            if (movementRoot == null || lookPivot == null)
            {
                ResolveLookTargets();
            }

            RefreshDevices(force: false);

            Vector2 leftStick = ReadStick(ref leftDevice, XRNode.LeftHand);
            Vector2 rightStick = ReadStick(ref rightDevice, XRNode.RightHand);

            if (comfortMode)
            {
                // Comfort mode: keep right stick on yaw only. Vertical look is a common nausea trigger.
                rightStick.y = 0f;
                enablePitchFromRightStick = false;
            }

            leftStick = ApplyDeadZone(leftStick, deadZone);
            rightStick = ApplyDeadZone(rightStick, deadZone);

            float smoothing = Mathf.Max(1f, stickSmoothing);
            filteredLeftStick = Vector2.Lerp(filteredLeftStick, leftStick, Time.deltaTime * smoothing);
            filteredRightStick = Vector2.Lerp(filteredRightStick, rightStick, Time.deltaTime * smoothing);

            Transform root = movementRoot != null ? movementRoot : transform;

            Vector3 forward = root.forward;
            if (headCamera != null)
            {
                forward = Vector3.ProjectOnPlane(headCamera.transform.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.001f)
                {
                    forward = Vector3.ProjectOnPlane(root.forward, Vector3.up);
                }
            }
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 planarMove = (forward * filteredLeftStick.y + right * filteredLeftStick.x) * (moveSpeed * Time.deltaTime);
            root.position += planarMove;

            float yaw = filteredRightStick.x * yawSpeed * Time.deltaTime;
            if (Mathf.Abs(yaw) > 0.001f)
            {
                root.Rotate(Vector3.up, yaw, Space.World);
            }

            if (lookPivot != null)
            {
                if (enablePitchFromRightStick)
                {
                    pitchAngle -= filteredRightStick.y * pitchSpeed * Time.deltaTime;
                    pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
                }
                else
                {
                    pitchAngle = Mathf.Lerp(pitchAngle, 0f, Time.deltaTime * 8f);
                }

                lookPivot.localRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
            }
        }

        private void OnValidate()
        {
            ApplyComfortDefaults();
        }

        private void ApplyComfortDefaults()
        {
            if (!comfortMode)
            {
                return;
            }

            enablePitchFromRightStick = false;
            pitchSpeed = Mathf.Clamp(pitchSpeed, 0f, 55f);
            maxPitchAngle = Mathf.Clamp(maxPitchAngle, 0f, 16f);
        }

        private void RefreshCamera()
        {
            headCamera = Camera.main;
        }

        private void RefreshDevices(bool force)
        {
            if (force || !leftDevice.isValid)
            {
                leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            }

            if (force || !rightDevice.isValid)
            {
                rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            }
        }

        private static Vector2 ReadStick(ref InputDevice device, XRNode node)
        {
            if (!device.isValid)
            {
                device = InputDevices.GetDeviceAtXRNode(node);
            }

            if (device.isValid && device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 value))
            {
                return value;
            }

            if (device.isValid && device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 altValue))
            {
                return altValue;
            }

            return Vector2.zero;
        }

        private static Vector2 ApplyDeadZone(Vector2 value, float zone)
        {
            if (value.sqrMagnitude <= zone * zone)
            {
                return Vector2.zero;
            }

            return value;
        }

        private void DisableConflictingLocomotionProviders()
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this)
                {
                    continue;
                }

                System.Type t = behaviour.GetType();
                string ns = t.Namespace ?? string.Empty;
                string name = t.Name;
                if (!ns.Contains("XR.Interaction.Toolkit"))
                {
                    continue;
                }

                if (name.Contains("TurnProvider") ||
                    name.Contains("MoveProvider") ||
                    name.Contains("TeleportationProvider") ||
                    name.Contains("SnapTurn") ||
                    name.Contains("ContinuousTurn") ||
                    name.Contains("ContinuousMove") ||
                    name.Contains("LocomotionProvider"))
                {
                    behaviour.enabled = false;
                }
            }

            locomotionProvidersDisabled = true;
        }

        private void ResolveLookTargets()
        {
            movementRoot = transform;
            lookPivot = null;

            System.Type xrOriginType = System.Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
            if (xrOriginType == null)
            {
                if (headCamera != null && headCamera.transform.parent != null)
                {
                    lookPivot = headCamera.transform.parent;
                }
                return;
            }

            Component[] allComponents = GetComponentsInChildren<Component>(true);
            for (int i = 0; i < allComponents.Length; i++)
            {
                Component c = allComponents[i];
                if (c != null && xrOriginType.IsAssignableFrom(c.GetType()))
                {
                    movementRoot = c.transform;
                    break;
                }
            }

            if (headCamera != null && headCamera.transform.parent != null)
            {
                lookPivot = headCamera.transform.parent;
            }
        }
    }
}
