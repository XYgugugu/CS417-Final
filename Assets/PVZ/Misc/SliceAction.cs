using System.Collections.Generic;
using System.Reflection;
using PVZ3D.Zombies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PVZ3D.Misc
{
    public class SliceAction : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionProperty triggerAction;
        [SerializeField, Range(0.1f, 1f)] private float triggerPressThreshold = 0.5f;

        [Header("Slice Detection")]
        [SerializeField] private Vector3 slicePointLocalOffset = new Vector3(0f, 0f, 0.75f);
        [SerializeField] private float minimumSliceLength = 0.35f;
        [SerializeField] private float minimumSampleDistance = 0.01f;
        [SerializeField, Range(0.5f, 1f)] private float minimumStraightness = 0.9f;
        [SerializeField] private float maximumLineDeviation = 0.08f;

        [Header("Zombie Hit")]
        [SerializeField] private float zombieHitDistance = 2f;
        [SerializeField] private float zombieHitRadius = 0.35f;
        [SerializeField] private LayerMask zombieHitMask = ~0;

        private readonly List<Vector3> samples = new List<Vector3>(64);
        private static readonly MethodInfo ZombieDieMethod = typeof(ZombieBase).GetMethod(
            "Die",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private InputAction fallbackTriggerAction;
        private bool isSlicing;
        private bool blockedUntilRelease;
        private Vector3 sliceStartPosition;
        private Vector3 lastSamplePosition;
        private float slicePathLength;

        private void OnEnable()
        {
            InputAction action = GetTriggerAction();
            if (action != null && !action.enabled)
            {
                action.Enable();
            }
        }

        private void OnDisable()
        {
            if (fallbackTriggerAction != null)
            {
                fallbackTriggerAction.Disable();
            }

            ResetSlice();
        }

        private void OnDestroy()
        {
            fallbackTriggerAction?.Dispose();
            fallbackTriggerAction = null;
        }

        private void Update()
        {
            bool triggerHeld = IsTriggerHeld();
            if (!triggerHeld)
            {
                blockedUntilRelease = false;
                ResetSlice();
                return;
            }

            if (blockedUntilRelease)
            {
                return;
            }

            if (!isSlicing)
            {
                BeginSlice();
                return;
            }

            TrackSlice();
        }

        private void BeginSlice()
        {
            isSlicing = true;
            slicePathLength = 0f;
            sliceStartPosition = GetSliceSamplePosition();
            lastSamplePosition = sliceStartPosition;

            samples.Clear();
            samples.Add(sliceStartPosition);
        }

        private void TrackSlice()
        {
            Vector3 currentPosition = GetSliceSamplePosition();
            float sampleDistance = Vector3.Distance(lastSamplePosition, currentPosition);
            if (sampleDistance < minimumSampleDistance)
            {
                return;
            }

            slicePathLength += sampleDistance;
            lastSamplePosition = currentPosition;
            samples.Add(currentPosition);

            if (IsStraightSlice(currentPosition, out float sliceLength))
            {
                Debug.Log($"{name} slice action length: {sliceLength:0.00}m", this);
                TryKillZombieInFront();
                blockedUntilRelease = true;
                ResetSlice();
            }
        }

        private bool IsStraightSlice(Vector3 currentPosition, out float sliceLength)
        {
            Vector3 sliceVector = currentPosition - sliceStartPosition;
            sliceLength = sliceVector.magnitude;

            if (sliceLength < minimumSliceLength || slicePathLength < minimumSliceLength)
            {
                return false;
            }

            float straightness = sliceLength / Mathf.Max(slicePathLength, 0.0001f);
            if (straightness < minimumStraightness)
            {
                return false;
            }

            return GetMaximumDeviationFromLine(sliceStartPosition, currentPosition) <= maximumLineDeviation;
        }

        private float GetMaximumDeviationFromLine(Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 line = lineEnd - lineStart;
            float lineLengthSquared = line.sqrMagnitude;
            if (lineLengthSquared <= 0.0001f)
            {
                return 0f;
            }

            float maximumDeviation = 0f;
            for (int i = 1; i < samples.Count - 1; i++)
            {
                Vector3 sampleOffset = samples[i] - lineStart;
                float projectedAmount = Mathf.Clamp01(Vector3.Dot(sampleOffset, line) / lineLengthSquared);
                Vector3 closestPoint = lineStart + line * projectedAmount;
                maximumDeviation = Mathf.Max(maximumDeviation, Vector3.Distance(samples[i], closestPoint));
            }

            return maximumDeviation;
        }

        private Vector3 GetSliceSamplePosition()
        {
            return transform.TransformPoint(slicePointLocalOffset);
        }

        private void TryKillZombieInFront()
        {
            ZombieBase zombie = FindZombieInFront();
            if (zombie == null)
            {
                return;
            }

            if (ZombieDieMethod != null)
            {
                ZombieDieMethod.Invoke(zombie, null);
                Debug.Log($"{name} sliced zombie: {zombie.name}", this);
            }
        }

        private ZombieBase FindZombieInFront()
        {
            Vector3 origin = GetSliceSamplePosition();
            Vector3 direction = transform.forward;

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                zombieHitRadius,
                direction,
                zombieHitDistance,
                zombieHitMask,
                QueryTriggerInteraction.Collide);

            ZombieBase closestZombie = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                ZombieBase zombie = hitCollider.GetComponentInParent<ZombieBase>();
                if (zombie == null)
                {
                    Rigidbody attachedBody = hitCollider.attachedRigidbody;
                    zombie = attachedBody != null ? attachedBody.GetComponentInParent<ZombieBase>() : null;
                }

                if (zombie == null || hits[i].distance >= closestDistance)
                {
                    continue;
                }

                closestZombie = zombie;
                closestDistance = hits[i].distance;
            }

            return closestZombie;
        }

        private void ResetSlice()
        {
            isSlicing = false;
            slicePathLength = 0f;
            samples.Clear();
        }

        private bool IsTriggerHeld()
        {
            InputAction action = GetTriggerAction();
            if (action == null)
            {
                return false;
            }

            return action.ReadValue<float>() >= triggerPressThreshold || action.IsPressed();
        }

        private InputAction GetTriggerAction()
        {
            InputAction configuredAction = triggerAction.action;
            if (configuredAction != null && configuredAction.bindings.Count > 0)
            {
                return configuredAction;
            }

            if (fallbackTriggerAction == null)
            {
                fallbackTriggerAction = new InputAction(
                    $"{name} Trigger Slice",
                    InputActionType.Value,
                    ResolveFallbackTriggerBinding(),
                    expectedControlType: "Axis");
            }

            return fallbackTriggerAction;
        }

        private string ResolveFallbackTriggerBinding()
        {
            string objectName = name.ToLowerInvariant();
            if (objectName.Contains("left"))
            {
                return "<XRController>{LeftHand}/{Trigger}";
            }

            if (objectName.Contains("right"))
            {
                return "<XRController>{RightHand}/{Trigger}";
            }

            return "<XRController>/{Trigger}";
        }
    }
}
