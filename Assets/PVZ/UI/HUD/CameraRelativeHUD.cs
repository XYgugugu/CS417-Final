using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// Locks a World Space HUD canvas to the player's camera by re-positioning it
    /// each LateUpdate so it sits at <see cref="distance"/> meters in front of the
    /// camera and faces it. Avoids hard-parenting to the camera transform (which
    /// makes prefab/scene authoring awkward) while still giving the camera-relative
    /// behavior a HUD needs in 3D / VR.
    ///
    /// VR notes:
    /// - <see cref="followSmoothing"/> &gt; 0 introduces a slight lag that is the
    ///   single biggest factor in HUD comfort. 0 = head-locked = nausea.
    /// - <see cref="lockYAxis"/> = true is the standard VR HUD pattern: the canvas
    ///   only follows the player's yaw, never their pitch, and stays at a fixed
    ///   world height. This lets the player tilt their head down past the HUD to
    ///   see the playfield (cards / lawn / zombies) the same way you'd glance
    ///   under a real heads-up display.
    /// - The script is a no-op outside World Space mode, so it's safe to leave
    ///   attached if the canvas render mode changes.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public class CameraRelativeHUD : MonoBehaviour
    {
        [Tooltip("Camera the HUD should track. If null, Camera.main is used (i.e. the XR Origin's tagged Main Camera in VR builds).")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Distance in meters from the camera at which the HUD plane sits. 2.0-3.0m fits comfortably in both Quest (~96° V FOV) and editor Game View (~60° V FOV). 1.5m feels close/intimate but clips on narrow editor cameras.")]
        [SerializeField] private float distance = 2.5f;

        [Tooltip("Local offset (camera space) applied after the forward placement. Negative Y drops the HUD slightly below the gaze line so it doesn't block the playfield. Ignored on the Y axis when lockYAxis is on (use Y here as 'height above the canvas's authored position' instead).")]
        [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.2f, 0f);

        [Tooltip("If true, the HUD billboards toward the camera every frame. With lockYAxis on this becomes yaw-only (the canvas stays vertically upright like a sign in the world).")]
        [SerializeField] private bool faceCamera = true;

        [Tooltip("If > 0, position is smoothed toward the target with this lerp rate (higher = snappier). 0 = hard-lock to camera (causes VR nausea). 4-8 is the comfort sweet spot.")]
        [SerializeField] private float followSmoothing = 6f;

        [Tooltip("VR comfort: when on, the HUD stays at the world Y it had on Awake and only follows the player's X/Z (yaw). The player can look down past the HUD to see the playfield. Recommended for any seated/standing VR HUD.")]
        [SerializeField] private bool lockYAxis = true;

        [Tooltip("Skip following while the canvas render mode isn't World Space. Lets you flip render mode at runtime without disabling the script.")]
        [SerializeField] private bool onlyWhenWorldSpace = true;

        private Canvas _canvas;
        private Transform _tr;
        private float _initialY;

        private void Awake()
        {
            _tr = transform;
            _canvas = GetComponent<Canvas>();
            _initialY = _tr.position.y;
        }

        private void LateUpdate()
        {
            Camera cam = ResolveCamera();
            if (cam == null) return;

            if (onlyWhenWorldSpace && _canvas != null && _canvas.renderMode != RenderMode.WorldSpace)
            {
                return;
            }

            Transform camTr = cam.transform;
            Vector3 camFwd = camTr.forward;
            Vector3 camUp = camTr.up;

            if (lockYAxis)
            {
                // Flatten forward onto world XZ plane so the HUD stays upright
                // and pitch (look up/down) doesn't move it.
                Vector3 flat = new Vector3(camFwd.x, 0f, camFwd.z);
                if (flat.sqrMagnitude < 1e-4f)
                {
                    // Edge case: looking straight up/down -- keep current yaw.
                    flat = new Vector3(_tr.forward.x, 0f, _tr.forward.z);
                    if (flat.sqrMagnitude < 1e-4f) flat = Vector3.forward;
                }
                camFwd = flat.normalized;
                camUp = Vector3.up;
            }

            Vector3 camRight = Vector3.Cross(camUp, camFwd).normalized;

            Vector3 targetPos =
                camTr.position
                + camFwd * (distance + localOffset.z)
                + camRight * localOffset.x
                + camUp * localOffset.y;

            if (lockYAxis)
            {
                // Pin world Y to the canvas's authored height plus the vertical
                // offset, so head pitch doesn't slide the HUD up/down with the gaze.
                targetPos.y = _initialY + localOffset.y;
            }

            if (followSmoothing > 0f)
            {
                _tr.position = Vector3.Lerp(
                    _tr.position,
                    targetPos,
                    1f - Mathf.Exp(-followSmoothing * Time.unscaledDeltaTime));
            }
            else
            {
                _tr.position = targetPos;
            }

            if (faceCamera)
            {
                // Face the camera: canvas's forward (+Z) points away from the
                // camera so its UI side faces it. With lockYAxis, camFwd is
                // already flattened so this becomes yaw-only billboarding.
                _tr.rotation = Quaternion.LookRotation(camFwd, camUp);
            }
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null) return targetCamera;
            targetCamera = Camera.main;
            return targetCamera;
        }
    }
}
