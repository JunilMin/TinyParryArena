using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class ThirdPersonCameraController : MonoBehaviour
{
    [SerializeField] private bool useRecommendedCombatFraming = true;
    [SerializeField] private Transform target;
    [SerializeField] private PlayerLockOn lockOn;
    [SerializeField] private float distance = 10.6f;
    [SerializeField] private float height = 2.45f;
    [SerializeField] private float sensitivity = 0.16f;
    [SerializeField, Range(0.1f, 1f)] private float freeLookSensitivityMultiplier = 0.8f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 55f;
    [SerializeField] private float lockOnTargetHeight = 1.7f;
    [SerializeField] private float smoothTime = 0.05f;
    [SerializeField, Min(0.01f)] private float freeLookRotationSmoothTime = 0.08f;
    [SerializeField, Min(0.01f)] private float lockOnRotationSmoothTime = 0.07f;

    [Header("Lock-On Framing")]
    [SerializeField, Min(2f)] private float lockOnDistance = 3.7f;
    [SerializeField, Min(0.5f)] private float lockOnPivotHeight = 1.45f;
    [SerializeField, Range(-5f, 35f)] private float lockOnCameraPitch = 18f;
    [SerializeField, Range(-3f, 3f)] private float lockOnShoulderOffset = 0.65f;
    [SerializeField, Range(-20f, 20f)] private float lockOnOrbitAngle;
    [SerializeField, Range(0.4f, 0.8f)] private float lockOnFocusBias = 0.72f;
    [SerializeField, Min(0f)] private float lockOnDistancePerMeter = 0.12f;
    [SerializeField, Min(0f)] private float lockOnMaxDistanceExtension = 0.5f;
    [SerializeField, Min(0f)] private float lockOnDistanceExtensionStart = 4.5f;
    [SerializeField, Min(0.01f)] private float lockOnPositionSmoothTime = 0.1f;
    [SerializeField, Min(0.01f)] private float lockOnFocusSmoothTime = 0.08f;
    [SerializeField, Range(35f, 75f)] private float freeLookFieldOfView = 58f;
    [SerializeField, Range(35f, 75f)] private float lockOnFieldOfView = 52f;
    [SerializeField, Range(35f, 75f)] private float closeLockOnFieldOfView = 48f;
    [SerializeField, Min(0.01f)] private float fieldOfViewSmoothTime = 0.16f;

    [Header("Collision")]
    [SerializeField, Min(0.05f)] private float collisionRadius = 0.35f;
    [SerializeField, Min(0f)] private float collisionBuffer = 0.2f;
    [SerializeField, Min(0.05f)] private float minimumDistance = 0.15f;
    [SerializeField] private LayerMask collisionLayers = ~0;

    private float yaw;
    private float pitch = 22f;
    private float desiredYaw;
    private float desiredPitch = 22f;
    private float yawVelocity;
    private float pitchVelocity;
    private Vector3 velocity;
    private readonly RaycastHit[] collisionHits = new RaycastHit[12];
    private Camera controlledCamera;
    private float fieldOfViewVelocity;
    private Vector3 smoothedLockOnFocus;
    private Vector3 lockOnFocusVelocity;
    private Transform smoothedLockOnTarget;

    public void SetTarget(Transform value)
    {
        target = value;
        lockOn = value != null ? value.GetComponent<PlayerLockOn>() : null;
    }

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        if (!useRecommendedCombatFraming)
            return;

        lockOnTargetHeight = 1.7f;
        lockOnDistance = 3.7f;
        lockOnPivotHeight = 1.45f;
        lockOnCameraPitch = 18f;
        lockOnShoulderOffset = 0.65f;
        lockOnOrbitAngle = 0f;
        lockOnFocusBias = 0.72f;
        lockOnDistancePerMeter = 0.12f;
        lockOnMaxDistanceExtension = 0.5f;
        lockOnDistanceExtensionStart = 4.5f;
        lockOnPositionSmoothTime = 0.1f;
        lockOnFocusSmoothTime = 0.08f;
        lockOnRotationSmoothTime = 0.07f;
        lockOnFieldOfView = 52f;
        closeLockOnFieldOfView = 48f;
    }

    private void Start()
    {
        yaw = desiredYaw = transform.eulerAngles.y;
        pitch = desiredPitch;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (lockOn == null) lockOn = target.GetComponent<PlayerLockOn>();

        bool isLockedOn = lockOn != null && lockOn.IsLockedOn;

        if (controlledCamera != null)
        {
            float targetFieldOfView = freeLookFieldOfView;
            if (isLockedOn)
            {
                float separation = Vector3.Distance(target.position, lockOn.CurrentTarget.position);
                targetFieldOfView = Mathf.Lerp(
                    closeLockOnFieldOfView,
                    lockOnFieldOfView,
                    Mathf.InverseLerp(2.4f, 8f, separation));
            }
            controlledCamera.fieldOfView = Mathf.SmoothDamp(
                controlledCamera.fieldOfView,
                targetFieldOfView,
                ref fieldOfViewVelocity,
                fieldOfViewSmoothTime);
        }

        if (isLockedOn)
        {
            Vector3 targetDirection = lockOn.CurrentTarget.position - target.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.001f)
                desiredYaw = Quaternion.LookRotation(targetDirection.normalized).eulerAngles.y + lockOnOrbitAngle;
            desiredPitch = lockOnCameraPitch;
        }
        else
        {
            Vector2 look = ReadLookInput() * freeLookSensitivityMultiplier;
            desiredYaw += look.x * sensitivity;
            desiredPitch = Mathf.Clamp(desiredPitch - look.y * sensitivity, minPitch, maxPitch);
        }

        float rotationSmoothTime = isLockedOn
            ? lockOnRotationSmoothTime
            : freeLookRotationSmoothTime;
        yaw = Mathf.SmoothDampAngle(yaw, desiredYaw, ref yawVelocity, rotationSmoothTime);
        pitch = Mathf.SmoothDamp(pitch, desiredPitch, ref pitchVelocity, rotationSmoothTime);

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        float activeHeight = isLockedOn ? lockOnPivotHeight : height;
        Vector3 pivot = target.position + Vector3.up * activeHeight;

        float activeDistance = isLockedOn ? lockOnDistance : distance;
        if (isLockedOn)
        {
            float separation = Vector3.Distance(target.position, lockOn.CurrentTarget.position);
            activeDistance += Mathf.Clamp(
                (separation - lockOnDistanceExtensionStart) * lockOnDistancePerMeter,
                0f,
                lockOnMaxDistanceExtension);
        }

        Vector3 cameraDirection = -(orbitRotation * Vector3.forward);
        Vector3 desiredOffset = cameraDirection * activeDistance;
        if (isLockedOn)
        {
            float separation = Vector3.Distance(target.position, lockOn.CurrentTarget.position);
            float closeRangeShoulder = lockOnShoulderOffset + 0.15f;
            float activeShoulder = Mathf.Lerp(
                closeRangeShoulder,
                lockOnShoulderOffset,
                Mathf.InverseLerp(2.2f, 6f, separation));
            desiredOffset += orbitRotation * Vector3.right * activeShoulder;
        }

        float desiredOffsetLength = desiredOffset.magnitude;
        Vector3 collisionDirection = desiredOffset / desiredOffsetLength;
        float collisionDistance = GetCollisionDistance(pivot, collisionDirection, desiredOffsetLength);
        Vector3 desired = pivot + collisionDirection * collisionDistance;
        float positionSmoothTime = isLockedOn ? lockOnPositionSmoothTime : smoothTime;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            positionSmoothTime);

        if (isLockedOn)
        {
            Vector3 playerFocus = target.position + Vector3.up * 1.05f;
            Vector3 enemyFocus = lockOn.CurrentTarget.position + Vector3.up * lockOnTargetHeight;
            Vector3 focus = Vector3.Lerp(playerFocus, enemyFocus, lockOnFocusBias);
            if (smoothedLockOnTarget != lockOn.CurrentTarget)
            {
                smoothedLockOnTarget = lockOn.CurrentTarget;
                smoothedLockOnFocus = focus;
                lockOnFocusVelocity = Vector3.zero;
            }
            else
            {
                smoothedLockOnFocus = Vector3.SmoothDamp(
                    smoothedLockOnFocus,
                    focus,
                    ref lockOnFocusVelocity,
                    lockOnFocusSmoothTime);
            }

            Vector3 lookDirection = smoothedLockOnFocus - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
        else
        {
            smoothedLockOnTarget = null;
            lockOnFocusVelocity = Vector3.zero;
            transform.rotation = orbitRotation;
        }
    }

    private float GetCollisionDistance(Vector3 pivot, Vector3 direction, float desiredDistance)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            pivot,
            collisionRadius,
            direction,
            collisionHits,
            desiredDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = desiredDistance;
        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = collisionHits[i].transform;
            if (hitTransform == null || hitTransform == target || hitTransform.IsChildOf(target))
                continue;

            nearestDistance = Mathf.Min(nearestDistance, collisionHits[i].distance - collisionBuffer);
        }

        return Mathf.Clamp(nearestDistance, minimumDistance, desiredDistance);
    }

    private static Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#endif
    }
}
