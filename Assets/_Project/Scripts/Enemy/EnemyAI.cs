using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CombatantHealth))]
public sealed class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private TrailRenderer weaponTrail;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.4f;
    [SerializeField] private float attackRange = 1.9f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Pattern 1 - Overhead Slash")]
    [SerializeField] private int overheadDamage = 15;
    [SerializeField] private float overheadTelegraph = 0.72f;

    [Header("Pattern 2 - Horizontal Slash")]
    [SerializeField] private int horizontalDamage = 12;
    [SerializeField] private float horizontalTelegraph = 0.48f;

    [Header("Pattern 3 - Thrust")]
    [SerializeField] private int thrustDamage = 10;
    [SerializeField] private float thrustTelegraph = 0.32f;

    [Header("Physical Feedback")]
    [SerializeField] private float enemyParryKnockback = 1.25f;
    [SerializeField] private float playerHitKnockback = 0.85f;
    [SerializeField] private float parryStunTime = 1.5f;
    [SerializeField] private float recoveryTime = 0.72f;

    private CombatantHealth selfHealth;
    private CombatantHealth playerHealth;
    private PlayerCombat playerCombat;
    private KnockbackReceiver selfKnockback;
    private KnockbackReceiver playerKnockback;

    private bool busy;
    private bool attackWasParried;
    private float stunnedUntil;
    private int nextPattern;
    private Quaternion weaponIdleRotation;
    private Vector3 weaponIdlePosition;

    public void SetPlayer(Transform value)
    {
        player = value;
        CachePlayerReferences();
    }

    private void Awake()
    {
        selfHealth = GetComponent<CombatantHealth>();
        selfKnockback = GetComponent<KnockbackReceiver>();

        if (selfKnockback == null)
            selfKnockback = gameObject.AddComponent<KnockbackReceiver>();

        if (selfHealth != null)
            selfHealth.Died += OnDied;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();

        CacheWeaponIdlePose();
        SetTrail(false);
    }

    private void Start()
    {
        CachePlayerReferences();
        CacheWeaponIdlePose();
    }

    private void CacheWeaponIdlePose()
    {
        if (weaponPivot == null)
            return;

        weaponIdleRotation = weaponPivot.localRotation;
        weaponIdlePosition = weaponPivot.localPosition;
    }

    private void CachePlayerReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null)
            return;

        playerHealth = player.GetComponent<CombatantHealth>();
        playerCombat = player.GetComponent<PlayerCombat>();
        playerKnockback = player.GetComponent<KnockbackReceiver>();

        if (playerKnockback == null)
            playerKnockback = player.gameObject.AddComponent<KnockbackReceiver>();
    }

    private void Update()
    {
        if (selfHealth == null || selfHealth.IsDead)
            return;

        if (Time.time < stunnedUntil || busy)
            return;

        // KnockbackReceiver is optional for basic AI operation.
        // Missing knockback must never stop movement or attacks.
        if (player == null || playerHealth == null || playerCombat == null)
        {
            CachePlayerReferences();
            return;
        }

        if (playerHealth.IsDead)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        if (distance > attackRange)
        {
            Vector3 direction = toPlayer.normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction, Vector3.up);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            return;
        }

        StartCoroutine(AttackRoutine(nextPattern));
        nextPattern = (nextPattern + 1) % 3;
    }

    private IEnumerator AttackRoutine(int pattern)
    {
        busy = true;
        attackWasParried = false;
        FacePlayerImmediately();

        switch (pattern)
        {
            case 0:
                yield return OverheadSlash();
                break;
            case 1:
                yield return HorizontalSlash();
                break;
            default:
                yield return Thrust();
                break;
        }

        SetTrail(false);
        ResetWeapon();

        if (attackWasParried)
        {
            float remainingStun = Mathf.Max(0f, stunnedUntil - Time.time);
            if (remainingStun > 0f)
                yield return new WaitForSeconds(remainingStun);
        }
        else
        {
            yield return new WaitForSeconds(recoveryTime);
        }

        if (!selfHealth.IsDead)
            SetBodyColor(Color.white);
        busy = false;
    }

    private IEnumerator OverheadSlash()
    {
        SetBodyColor(Color.yellow);

        Quaternion raised = Quaternion.Euler(-120f, 0f, 15f);
        Quaternion strike = Quaternion.Euler(55f, 0f, -10f);

        yield return RotateWeapon(weaponIdleRotation, raised, overheadTelegraph);
        SetTrail(true);
        yield return RotateWeapon(raised, strike, 0.18f);
        ResolveHit(overheadDamage, attackRange + 0.45f);
        yield return new WaitForSeconds(0.08f);
    }

    private IEnumerator HorizontalSlash()
    {
        SetBodyColor(new Color(1f, 0.45f, 0f));

        Quaternion windup = Quaternion.Euler(10f, -120f, 20f);
        Quaternion strike = Quaternion.Euler(10f, 115f, -15f);

        yield return RotateWeapon(weaponIdleRotation, windup, horizontalTelegraph);
        SetTrail(true);
        yield return RotateWeapon(windup, strike, 0.22f);
        ResolveHit(horizontalDamage, attackRange + 0.45f);
        yield return new WaitForSeconds(0.08f);
    }

    private IEnumerator Thrust()
    {
        SetBodyColor(new Color(1f, 0.32f, 0.04f));

        Quaternion aim = Quaternion.Euler(0f, 0f, 90f);
        Vector3 pulledBack =
            weaponIdlePosition + new Vector3(0f, 0f, -0.35f);
        Vector3 thrustForward =
            weaponIdlePosition + new Vector3(0f, 0f, 0.75f);

        yield return RotateWeapon(
            weaponIdleRotation,
            aim,
            thrustTelegraph * 0.55f
        );

        yield return MoveWeapon(
            weaponIdlePosition,
            pulledBack,
            thrustTelegraph * 0.45f
        );

        SetTrail(true);
        yield return MoveWeapon(pulledBack, thrustForward, 0.14f);
        ResolveHit(thrustDamage, attackRange + 0.75f);
        yield return new WaitForSeconds(0.08f);
    }

    private void ResolveHit(int damage, float range)
    {
        if (player == null ||
            playerHealth == null ||
            playerCombat == null ||
            Vector3.Distance(transform.position, player.position) > range)
            return;

        Vector3 enemyAwayDirection =
            (transform.position - player.position).normalized;

        Vector3 playerAwayDirection =
            (player.position - transform.position).normalized;

        if (playerCombat.TryParry())
        {
            if (selfKnockback != null)
            {
                selfKnockback.ApplyKnockback(
                    enemyAwayDirection,
                    enemyParryKnockback,
                    0.16f
                );
            }

            attackWasParried = true;
            stunnedUntil = Time.time + parryStunTime;
            CharacterVisualController selfVisual = GetComponent<CharacterVisualController>();
            if (selfVisual != null) selfVisual.PlayStagger(parryStunTime);
            StartCoroutine(ParryReactionRoutine());
            return;
        }

        playerHealth.TakeDamage(damage);
        CharacterVisualController playerVisual = player.GetComponent<CharacterVisualController>();
        if (playerVisual != null) playerVisual.PlayHit();

        if (playerKnockback != null)
        {
            playerKnockback.ApplyKnockback(
                playerAwayDirection,
                playerHitKnockback,
                0.14f
            );
        }
    }

    private IEnumerator ParryReactionRoutine()
    {
        SetTrail(false);
        SetBodyColor(Color.cyan);

        if (weaponPivot != null)
        {
            Quaternion start = weaponPivot.localRotation;
            Quaternion recoil = Quaternion.Euler(-35f, 0f, -55f);
            yield return RotateWeapon(start, recoil, 0.12f);
        }

        yield return new WaitForSeconds(0.18f);
        ResetWeapon();
    }

    private IEnumerator RotateWeapon(
        Quaternion from,
        Quaternion to,
        float duration)
    {
        if (weaponPivot == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            weaponPivot.localRotation = Quaternion.Slerp(
                from,
                to,
                Mathf.Clamp01(elapsed / duration)
            );
            yield return null;
        }

        weaponPivot.localRotation = to;
    }

    private IEnumerator MoveWeapon(
        Vector3 from,
        Vector3 to,
        float duration)
    {
        if (weaponPivot == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            weaponPivot.localPosition = Vector3.Lerp(
                from,
                to,
                Mathf.Clamp01(elapsed / duration)
            );
            yield return null;
        }

        weaponPivot.localPosition = to;
    }

    private void FacePlayerImmediately()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ResetWeapon()
    {
        if (weaponPivot == null)
            return;

        weaponPivot.localRotation = weaponIdleRotation;
        weaponPivot.localPosition = weaponIdlePosition;
    }

    private void SetTrail(bool enabled)
    {
        if (weaponTrail != null)
            weaponTrail.emitting = enabled;
    }

    private void SetBodyColor(Color color)
    {
        // Combat readability is animation-only. Deliberately no material tinting.
    }

    private void OnDied(CombatantHealth _)
    {
        StopAllCoroutines();
        busy = true;
        SetTrail(false);
        SetBodyColor(Color.gray);
        transform.localScale *= 0.7f;
    }
}
