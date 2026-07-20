using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerLockOn : MonoBehaviour
{
    [SerializeField] private float searchRadius = 18f;
    [SerializeField] private float rotationSpeed = 14f;
    [SerializeField] private Transform currentTarget;
    [SerializeField] private TinyParryHUD hud;

    private bool lockedOn;

    public bool IsLockedOn => lockedOn && currentTarget != null;
    public Transform CurrentTarget => currentTarget;

    public void Configure(TinyParryHUD hudValue)
    {
        hud = hudValue;
    }

    private void Update()
    {
        if (LockOnPressed())
        {
            if (IsLockedOn)
                ClearTarget();
            else
                AcquireNearestTarget();
        }

        if (!IsLockedOn)
            return;

        CombatantHealth health = currentTarget.GetComponent<CombatantHealth>();
        if (!currentTarget.gameObject.activeInHierarchy || health == null || health.IsDead)
        {
            ClearTarget();
            return;
        }

        Vector3 direction = currentTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > searchRadius * searchRadius)
        {
            ClearTarget();
            return;
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void AcquireNearestTarget()
    {
        CombatantHealth[] candidates =
            FindObjectsByType<CombatantHealth>(FindObjectsSortMode.None);

        Transform best = null;
        float bestDistance = float.MaxValue;

        foreach (CombatantHealth candidate in candidates)
        {
            if (candidate.gameObject == gameObject ||
                candidate.IsDead ||
                !candidate.gameObject.activeInHierarchy)
                continue;

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance <= searchRadius && distance < bestDistance)
            {
                best = candidate.transform;
                bestDistance = distance;
            }
        }

        currentTarget = best;
        lockedOn = currentTarget != null;

        if (hud != null)
            hud.SetLockOnTarget(currentTarget);
    }

    private void ClearTarget()
    {
        currentTarget = null;
        lockedOn = false;

        if (hud != null)
            hud.SetLockOnTarget(null);
    }

    private static bool LockOnPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }
}