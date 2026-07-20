using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class ThirdPersonCameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private PlayerLockOn lockOn;
    [SerializeField] private float distance = 10.6f;
    [SerializeField] private float height = 2.45f;
    [SerializeField] private float sensitivity = 0.16f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 55f;
    [SerializeField] private float lockOnPitch = 17f;
    [SerializeField] private float lockOnTargetHeight = 1.55f;
    [SerializeField] private float smoothTime = 0.05f;

    [Header("Collision")]
    [SerializeField, Min(0.05f)] private float collisionRadius = 0.35f;
    [SerializeField, Min(0f)] private float collisionBuffer = 0.2f;
    [SerializeField, Min(0.05f)] private float minimumDistance = 0.15f;
    [SerializeField] private LayerMask collisionLayers = ~0;

    private float yaw;
    private float pitch = 22f;
    private Vector3 velocity;
    private readonly RaycastHit[] collisionHits = new RaycastHit[12];

    public void SetTarget(Transform value)
    {
        target = value;
        lockOn = value != null ? value.GetComponent<PlayerLockOn>() : null;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (lockOn == null) lockOn = target.GetComponent<PlayerLockOn>();

        if (lockOn != null && lockOn.IsLockedOn)
        {
            Vector3 targetDirection = lockOn.CurrentTarget.position - target.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.001f)
                yaw = Quaternion.LookRotation(targetDirection.normalized).eulerAngles.y;
            pitch = lockOnPitch;
        }
        else
        {
            Vector2 look = ReadLookInput();
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);
        }

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * height;

        float activeDistance = distance;
        if (lockOn != null && lockOn.IsLockedOn)
        {
            float separation = Vector3.Distance(target.position, lockOn.CurrentTarget.position);
            activeDistance += Mathf.Clamp((separation - 2.5f) * 0.28f, 0f, 2.2f);
        }

        Vector3 cameraDirection = -(orbitRotation * Vector3.forward);
        float collisionDistance = GetCollisionDistance(pivot, cameraDirection, activeDistance);
        Vector3 desired = pivot + cameraDirection * collisionDistance;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

        if (lockOn != null && lockOn.IsLockedOn)
        {
            Vector3 playerFocus = target.position + Vector3.up * 1.05f;
            Vector3 enemyFocus = lockOn.CurrentTarget.position + Vector3.up * lockOnTargetHeight;
            Vector3 focus = Vector3.Lerp(playerFocus, enemyFocus, 0.66f);
            Vector3 lookDirection = focus - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
        else
        {
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
