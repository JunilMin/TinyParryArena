using System.Collections;
using UnityEngine;
using TinyParryArena.BossFramework;

[RequireComponent(typeof(CombatantHealth))]
public sealed class BossAI : MonoBehaviour
{
    [Header("Boss Definition")]
    [SerializeField] private BossData bossData;
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Renderer bodyRenderer;

    [Header("Difficulty")]
    [SerializeField, Min(1)] private int stageLevel = 1;
    [SerializeField] private int patternCount = 3;
    [SerializeField] private float royalParryDuration = 0.62f;
    [SerializeField] private float moveSpeed = 1.75f;
    [SerializeField] private float attackRange = 3.0f;
    [SerializeField] private float perfectParryWindow = 0.22f;
    [SerializeField] private float parryStunDuration = 2.0f;
    [SerializeField] private float turnSpeed = 13f;

    [Header("Posture")]
    [SerializeField] private int maxPosture = 100;
    [SerializeField] private int postureDamagePerParry = 35;
    [SerializeField] private float postureBreakDuration = 3.0f;

    [Header("Knockback")]
    [SerializeField] private float bossParryKnockback = 0.75f;
    [SerializeField] private float playerHitKnockback = 1.1f;

    [Header("Fear")]
    [SerializeField, Range(1f, 100f)] private float maxFear = 100f;

    [Tooltip("Stage 1에서 Fear를 가득 채우는 데 필요한 성공 패링 수")]
    [SerializeField, Min(1)] private int stageOneRequiredParries = 4;

    [Tooltip("Stage 10 이상에서 Fear를 가득 채우는 데 필요한 성공 패링 수")]
    [SerializeField, Min(1)] private int stageTenRequiredParries = 10;

    [Tooltip("Stage 1에서 플레이어가 피격됐을 때 감소하는 Fear")]
    [SerializeField, Min(0f)] private float stageOneHitFearLoss = 8f;

    [Tooltip("Stage 10 이상에서 플레이어가 피격됐을 때 감소하는 Fear")]
    [SerializeField, Min(0f)] private float stageTenHitFearLoss = 15f;

    [Tooltip("Stage 1에서 패링하지 않아도 Fear 감소가 시작되지 않는 시간")]
    [SerializeField, Min(0f)] private float stageOneDecayDelay = 5f;

    [Tooltip("Stage 10 이상에서 Fear 감소가 시작되는 시간")]
    [SerializeField, Min(0f)] private float stageTenDecayDelay = 3f;

    [SerializeField, Min(0f)] private float fearDecayPerSecond = 1f;

    private CombatantHealth selfHealth;
    private CombatantHealth playerHealth;
    private PlayerCombat playerCombat;
    private KnockbackReceiver selfKnockback;
    private KnockbackReceiver playerKnockback;
    private CharacterVisualController visual;
    private bool busy;
    private bool stunned;
    private int patternIndex;
    private int currentPosture;
    private bool lastStrikeParried;
    private bool bossParryActive;
    private Coroutine stunRoutine;
    private Color baseColor = Color.white;
    private float currentFear;
    private float lastSuccessfulParryTime = -999f;
    private BossState currentState = BossState.Idle;

    public BossData Data => bossData;
    public BossState CurrentState => currentState;
    public bool IsStunned => stunned;
    public int CurrentPosture => currentPosture;
    public int MaxPosture => maxPosture;
    public int StageLevel => stageLevel;
    public float CurrentFear => currentFear;
    public float MaxFear => maxFear;
    public float FearNormalized => maxFear <= 0f ? 0f : Mathf.Clamp01(currentFear / maxFear);
    public bool IsFearBroken => currentFear >= maxFear;
    public bool CanReceiveFinalStrike =>
        IsFearBroken &&
        selfHealth != null &&
        !selfHealth.IsDead;

    public int RequiredParriesForFullFear => GetRequiredParriesForStage();
    public float FearGainPerParry => maxFear / Mathf.Max(1, GetRequiredParriesForStage());

    public void SetPlayer(Transform value)
    {
        player = value;
        CachePlayerReferences();
    }

    public void ConfigureDifficulty(
        int level,
        int availablePatterns,
        float speed,
        float range,
        float parryTiming,
        int posture,
        int parryPostureDamage)
    {
        stageLevel = Mathf.Max(1, level);
        patternCount = Mathf.Clamp(availablePatterns, 3, 8);
        moveSpeed = Mathf.Max(0.5f, speed);
        attackRange = Mathf.Max(1.5f, range);
        perfectParryWindow = Mathf.Clamp(parryTiming, 0.07f, 0.30f);
        maxPosture = Mathf.Max(1, posture);
        postureDamagePerParry = Mathf.Max(1, parryPostureDamage);
        parryStunDuration = CalculateParryStunDuration(stageLevel);
        currentPosture = maxPosture;
    }

    private void Awake()
    {
        selfHealth = GetComponent<CombatantHealth>();
        selfKnockback = GetComponent<KnockbackReceiver>();
        visual = GetComponent<CharacterVisualController>();

        if (selfKnockback == null)
            selfKnockback = gameObject.AddComponent<KnockbackReceiver>();

        if (selfHealth != null)
            selfHealth.Died += OnDied;

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<Renderer>();


        currentPosture = maxPosture;
        currentFear = 0f;
        currentState = BossState.Idle;
    }

    private void Start() => CachePlayerReferences();

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

        if (currentState == BossState.Idle)
            currentState = BossState.Detect;

        UpdateFearDecay();

        if (player == null || playerHealth == null || playerCombat == null)
        {
            CachePlayerReferences();
            return;
        }

        if (playerHealth.IsDead)
            return;

        if (!stunned && !busy)
            currentState = BossState.Combat;

        Vector3 delta = player.position - transform.position;
        delta.y = 0f;
        FacePlayer(delta, busy ? turnSpeed * 1.35f : turnSpeed);

        if (busy || stunned)
            return;

        float distance = delta.magnitude;
        float preferredRange = attackRange * 0.88f;

        if (distance > attackRange)
        {
            float phaseBonus = selfHealth.CurrentHealth <= selfHealth.MaxHealth / 2 ? 1.16f : 1f;
            transform.position += delta.normalized * moveSpeed * phaseBonus * Time.deltaTime;
            return;
        }

        // Prevent the boss from standing directly inside the player.
        if (distance < preferredRange * 0.56f)
        {
            transform.position -= delta.normalized * moveSpeed * 0.55f * Time.deltaTime;
            return;
        }

        int selected = patternIndex++ % Mathf.Max(3, patternCount);
        StartCoroutine(RunPattern(selected));
    }

    private void FacePlayer(Vector3 flatDelta, float speed)
    {
        if (flatDelta.sqrMagnitude <= 0.0001f)
            return;

        Quaternion target = Quaternion.LookRotation(flatDelta.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            target,
            speed * 90f * Time.deltaTime);
    }

    private IEnumerator RunPattern(int pattern)
    {
        busy = true;
        currentState = BossState.Attack;
        lastStrikeParried = false;

        switch (pattern)
        {
            case 0: // readable overhead
                yield return Strike(0, 18 + stageLevel * 2, 0.72f, 0.22f, attackRange + 0.35f, Color.yellow);
                break;
            case 1: // horizontal sweep
                yield return Strike(1, 14 + stageLevel * 2, 0.48f, 0.18f, attackRange + 0.65f, new Color(1f, 0.45f, 0f));
                break;
            case 2: // thrust
                yield return Strike(2, 22 + stageLevel * 2, 0.82f, 0.24f, attackRange + 1.0f, new Color(1f, 0.12f, 0.02f));
                break;
            case 3: // delayed overhead: stage 2+
                yield return Strike(0, 22 + stageLevel * 2, 1.05f, 0.16f, attackRange + 0.45f, new Color(1f, 0.75f, 0.15f));
                break;
            case 4: // double slash: stage 2+
                yield return Strike(1, 13 + stageLevel, 0.42f, 0.15f, attackRange + 0.55f, new Color(1f, 0.3f, 0.1f));
                if (!lastStrikeParried && !selfHealth.IsDead)
                {
                    yield return new WaitForSeconds(0.16f);
                    yield return Strike(0, 18 + stageLevel, 0.24f, 0.12f, attackRange + 0.45f, new Color(1f, 0.18f, 0.08f));
                }
                break;
            case 5: // feint into thrust: stage 3
                SetBodyColor(new Color(0.85f, 0.25f, 1f));
                if (visual != null) visual.PlayTelegraph(0, 0.35f, 1.45f);
                yield return new WaitForSeconds(0.35f);
                SetBodyColor(baseColor);
                yield return new WaitForSeconds(0.22f);
                yield return Strike(2, 27, 0.30f, 0.10f, attackRange + 1.15f, new Color(1f, 0.55f, 0.05f));
                break;
            case 6: // three-hit royal combo: stage 3
                yield return Strike(1, 12, 0.38f, 0.12f, attackRange + 0.65f, Color.red);
                if (!lastStrikeParried && !selfHealth.IsDead)
                {
                    yield return new WaitForSeconds(0.12f);
                    yield return Strike(0, 16, 0.22f, 0.10f, attackRange + 0.45f, new Color(1f, 0.25f, 0.05f));
                }
                if (!lastStrikeParried && !selfHealth.IsDead)
                {
                    yield return new WaitForSeconds(0.10f);
                    yield return Strike(2, 24, 0.26f, 0.09f, attackRange + 1.0f, new Color(1f, 0.12f, 0.02f));
                }
                break;
            default: // royal parry stance: guaranteed in final-boss pattern cycle
                yield return RoyalParryPattern();
                break;
        }

        if (!selfHealth.IsDead && !stunned)
            SetBodyColor(baseColor);

        if (!stunned)
        {
            float recovery = Mathf.Max(0.28f, 0.92f - (stageLevel - 1) * 0.14f);
            if (selfHealth.CurrentHealth <= selfHealth.MaxHealth / 2)
                recovery *= 0.82f;
            yield return new WaitForSeconds(recovery);
        }

        busy = false;
        if (!stunned && !selfHealth.IsDead)
            currentState = BossState.Recover;
    }

    private IEnumerator Strike(
        int motion,
        int damage,
        float telegraph,
        float strikeLead,
        float range,
        Color telegraphColor)
    {
        lastStrikeParried = false;
        SetBodyColor(telegraphColor);
        float preStrike = Mathf.Max(0f, telegraph - strikeLead);
        if (visual != null)
            visual.PlayTelegraph(motion, Mathf.Max(0.18f, preStrike), 1.55f);
        yield return new WaitForSeconds(preStrike);

        if (visual != null)
            visual.PlayAttack(motion, 1.65f);

        // Delay the whoosh into the attack pose so it matches the visible blade acceleration.
        float swingSoundDelay = Mathf.Min(0.11f, strikeLead * 0.55f);
        if (swingSoundDelay > 0f)
            yield return new WaitForSeconds(swingSoundDelay);
        GameAudioManager.Instance.PlayBossSwing(motion);

        float remainingStrikeLead = Mathf.Max(0f, strikeLead - swingSoundDelay);
        if (remainingStrikeLead > 0f)
            yield return new WaitForSeconds(remainingStrikeLead);

        if (player == null || selfHealth.IsDead)
            yield break;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        FacePlayer(toPlayer, turnSpeed * 1.6f);

        float distance = toPlayer.magnitude;
        float facingAngle = toPlayer.sqrMagnitude > 0.0001f
            ? Vector3.Angle(transform.forward, toPlayer.normalized)
            : 0f;

        if (distance > range || facingAngle > 50f)
            yield break;

        if (playerCombat.TryParry(perfectParryWindow))
        {
            lastStrikeParried = true;
            GameAudioManager.Instance.PlayParry();
            HandleSuccessfulParry();
            yield break;
        }

        playerHealth.TakeDamage(damage);
        RemoveFear(GetHitFearLossForStage());

        CharacterVisualController playerVisual = player.GetComponent<CharacterVisualController>();
        if (playerVisual != null)
            playerVisual.PlayHit();

        if (playerKnockback != null)
        {
            Vector3 away = (player.position - transform.position).normalized;
            playerKnockback.ApplyKnockback(away, playerHitKnockback, 0.16f);
        }
    }

    private void HandleSuccessfulParry()
    {
        AddFear(FearGainPerParry);
        lastSuccessfulParryTime = Time.time;

        Vector3 away = player != null
            ? (transform.position - player.position).normalized
            : -transform.forward;

        if (selfKnockback != null)
            selfKnockback.ApplyKnockback(away, bossParryKnockback, 0.18f);

        if (IsFearBroken)
        {
            EnterFearBrokenState();
            return;
        }

        currentPosture = Mathf.Max(0, currentPosture - postureDamagePerParry);
        float duration = currentPosture <= 0 ? postureBreakDuration : parryStunDuration;
        if (currentPosture <= 0)
            currentPosture = maxPosture;

        if (visual != null)
            visual.PlayStagger(duration);

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

    private void EnterFearBrokenState()
    {
        currentFear = maxFear;
        currentState = BossState.Broken;
        stunned = true;
        bossParryActive = false;

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }

        SetBodyColor(Color.white);

        if (visual != null)
            visual.PlayStagger(9999f);
    }

    public void ExecuteFinalStrike()
    {
        if (!CanReceiveFinalStrike)
            return;

        currentState = BossState.Executed;
        stunned = true;
        busy = true;
        bossParryActive = false;
        currentFear = maxFear;

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }

        selfHealth.ForceKill();
    }

    private float GetStageDifficulty01()
    {
        return Mathf.InverseLerp(1f, 10f, Mathf.Clamp(stageLevel, 1, 10));
    }

    private int GetRequiredParriesForStage()
    {
        int required = Mathf.RoundToInt(
            Mathf.Lerp(
                stageOneRequiredParries,
                stageTenRequiredParries,
                GetStageDifficulty01()));

        return Mathf.Max(1, required);
    }

    private float GetHitFearLossForStage()
    {
        return Mathf.Lerp(
            stageOneHitFearLoss,
            stageTenHitFearLoss,
            GetStageDifficulty01());
    }

    private float GetFearDecayDelayForStage()
    {
        return Mathf.Lerp(
            stageOneDecayDelay,
            stageTenDecayDelay,
            GetStageDifficulty01());
    }

    public void AddFear(float amount)
    {
        if (amount <= 0f || IsFearBroken)
            return;

        currentFear = Mathf.Clamp(currentFear + amount, 0f, maxFear);
    }

    public void RemoveFear(float amount)
    {
        if (amount <= 0f || IsFearBroken)
            return;

        currentFear = Mathf.Clamp(currentFear - amount, 0f, maxFear);
    }

    private void UpdateFearDecay()
    {
        if (currentFear <= 0f || IsFearBroken)
            return;

        if (Time.time - lastSuccessfulParryTime < GetFearDecayDelayForStage())
            return;

        currentFear = Mathf.Max(0f, currentFear - fearDecayPerSecond * Time.deltaTime);
    }

    private IEnumerator StunRoutine(float duration)
    {
        stunned = true;
        SetBodyColor(currentPosture == maxPosture ? new Color(1f, 0.75f, 0.15f) : Color.cyan);
        yield return new WaitForSeconds(duration);

        if (IsFearBroken)
            yield break;

        stunned = false;
        if (!selfHealth.IsDead)
            SetBodyColor(baseColor);
    }

    private IEnumerator RoyalParryPattern()
    {
        if (stageLevel < 3)
            yield break;

        bossParryActive = true;
        SetBodyColor(new Color(0.35f, 0.75f, 1f));
        if (visual != null)
            visual.PlayParry();

        yield return new WaitForSeconds(royalParryDuration);
        bossParryActive = false;

        if (!selfHealth.IsDead && !stunned)
            SetBodyColor(baseColor);
    }

    public bool TryParryPlayerAttack()
    {
        if (stageLevel < 3 || !bossParryActive || stunned || selfHealth == null || selfHealth.IsDead)
            return false;

        bossParryActive = false;
        if (visual != null)
            visual.PlayParry();

        if (playerKnockback != null && player != null)
        {
            Vector3 away = (player.position - transform.position).normalized;
            playerKnockback.ApplyKnockback(away, 1.15f, 0.16f);
        }

        SetBodyColor(new Color(0.85f, 0.95f, 1f));
        return true;
    }

    public float GetIncomingDamageMultiplier()
    {
        return stunned ? 1.65f : 1f;
    }

    private void SetBodyColor(Color color)
    {
        // Combat readability is animation-only. Deliberately no material tinting.
    }

    public static float CalculateParryStunDuration(int stage)
    {
        int safeStage = Mathf.Max(1, stage);
        return Mathf.Max(0.2f, 2.0f - (safeStage - 1) * 0.1f);
    }

    private void OnDied(CombatantHealth _)
    {
        StopAllCoroutines();
        currentState = BossState.Dead;
        busy = true;
        stunned = false;
        bossParryActive = false;
        SetBodyColor(Color.gray);

        if (visual != null)
            visual.PlayDeath();

        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null)
            bodyCollider.enabled = false;
    }
}
