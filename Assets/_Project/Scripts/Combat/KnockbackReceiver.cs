using System.Collections;
using UnityEngine;

public sealed class KnockbackReceiver : MonoBehaviour
{
    [SerializeField] private float defaultDuration = 0.18f;

    private CharacterController characterController;
    private Coroutine activeRoutine;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void ApplyKnockback(Vector3 direction, float distance, float duration = -1f)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(
            KnockbackRoutine(
                direction.normalized,
                Mathf.Max(0f, distance),
                duration > 0f ? duration : defaultDuration
            )
        );
    }

    private IEnumerator KnockbackRoutine(
        Vector3 direction,
        float distance,
        float duration)
    {
        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + direction * distance;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 desired = Vector3.Lerp(start, end, eased);
            Vector3 delta = desired - transform.position;

            if (characterController != null && characterController.enabled)
                characterController.Move(delta);
            else
                transform.position += delta;

            yield return null;
        }

        activeRoutine = null;
    }
}