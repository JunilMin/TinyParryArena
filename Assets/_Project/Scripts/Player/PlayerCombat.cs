using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class PlayerCombat : MonoBehaviour
{
    [SerializeField] private bool useRecommendedMeleeTargeting = true;
    [SerializeField] private int[] comboDamage = { 14, 18, 28 };
    [SerializeField] private float attackRange = 2.65f;
    [SerializeField] private float attackAngle = 95f;
    [SerializeField] private float comboResetTime = 0.85f;
    [SerializeField] private float parryWindow = 0.23f;
    [SerializeField] private float parryCooldown = 0.55f;

    [Header("Final Strike")]
    [SerializeField, Min(1f)] private float finalStrikeRange = 3.4f;
    [SerializeField, Range(30f, 180f)] private float finalStrikeAngle = 110f;
    [SerializeField, Min(0f)] private float finalStrikeImpactDelay = 0.28f;

    [Header("Visual References")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform shieldPivot;
    [SerializeField] private Renderer shieldRenderer;
    [SerializeField] private GameObject swordModel;
    [SerializeField] private GameObject shieldModel;
    [SerializeField] private GameObject bowModel;
    [SerializeField] private GameObject arrowPrefab;

    private int comboIndex;
    private float lastAttackTime = -10f;
    private float nextParryTime;
    private float parryStartedTime = -10f;
    private bool busy;
    private bool parrying;
    private bool controlsEnabled = true;
    private Coroutine parriedRoutine;
    private CharacterVisualController visual;
    private PlayerLockOn lockOn;
    private float damageMultiplier = 1f;
    private float attackSpeedMultiplier = 1f;
    private readonly int[] skillRanks = new int[PlayerProgression.TotalSkillCount];
    private PlayerWeaponClass weaponClass = PlayerWeaponClass.Unchosen;
    private int equippedActiveSkill;
    private float nextActiveSkillTime;
    private float currentActiveSkillCooldown;

    public float ActiveSkillCooldownRemaining => Mathf.Max(0f, nextActiveSkillTime - Time.time);
    public float ActiveSkillCooldownDuration => currentActiveSkillCooldown > 0f ? currentActiveSkillCooldown : GetActiveSkillCooldown(equippedActiveSkill);
    public bool IsActiveSkillReady => ActiveSkillCooldownRemaining <= 0f;

    private Quaternion shieldIdleRotation;
    private Vector3 shieldIdlePosition;

    public bool IsParrying => parrying;
    public int EquippedActiveSkill => equippedActiveSkill;

    public void SetControlsEnabled(bool value) => controlsEnabled = value;

    public void SetProgressionMultipliers(float damage, float speed)
    {
        damageMultiplier = Mathf.Max(0.1f, damage);
        attackSpeedMultiplier = Mathf.Max(0.35f, speed);
    }

    public void SetSkillRanks(int first, int second, int third)
    {
        skillRanks[0] = Mathf.Max(0, first);
        skillRanks[1] = Mathf.Max(0, second);
        skillRanks[5] = Mathf.Max(0, third);
    }

    public void SetSkillRanks(int[] ranks)
    {
        if (ranks == null) return;
        for (int i = 0; i < skillRanks.Length; i++)
            skillRanks[i] = i < ranks.Length ? Mathf.Max(0, ranks[i]) : 0;
    }

    public void EquipActiveSkill(int slot)
    {
        equippedActiveSkill = Mathf.Clamp(slot, 0, PlayerProgression.ActiveSkillCount - 1);
        currentActiveSkillCooldown = GetActiveSkillCooldown(equippedActiveSkill);
    }

    public void SetWeaponClass(PlayerWeaponClass value)
    {
        weaponClass = value;
        if (swordModel != null) swordModel.SetActive(value == PlayerWeaponClass.Unchosen || value == PlayerWeaponClass.Greatsword);
        if (shieldModel != null) shieldModel.SetActive(false);
        if (bowModel != null) bowModel.SetActive(value == PlayerWeaponClass.Bow);
        visual?.SetWeaponClass(value);
        WeaponBodyClearance clearance = weaponPivot != null ? weaponPivot.GetComponent<WeaponBodyClearance>() : null;
        if (clearance != null)
            clearance.SetClearance(value == PlayerWeaponClass.Bow ? 0.54f : 0.48f);

        // The hand roots remain the animation pivots. Parry works with the greatsword blade, crossed swords, or spear shaft.
        if (weaponPivot != null) weaponPivot.localScale = Vector3.one;
        if (shieldPivot != null)
        {
            shieldPivot.gameObject.SetActive(true);
            shieldPivot.localScale = Vector3.one;
            shieldPivot.localRotation = shieldIdleRotation;
        }
    }

    // Kept for compatibility with the previous builder.
    public void ConfigureHUD(TinyParryHUD value) { }

    private void Awake()
    {
        visual = GetComponent<CharacterVisualController>();
        lockOn = GetComponent<PlayerLockOn>();
        EnsureBowEquipment();
        if (useRecommendedMeleeTargeting)
        {
            attackRange = Mathf.Max(attackRange, 2.65f);
            attackAngle = Mathf.Max(attackAngle, 95f);
        }

        if (shieldPivot != null)
        {
            shieldIdleRotation = shieldPivot.localRotation;
            shieldIdlePosition = shieldPivot.localPosition;
        }
    }

    private void EnsureBowEquipment()
    {
        if (arrowPrefab == null)
            arrowPrefab = Resources.Load<GameObject>("Weapons/Arrow");
        if (bowModel != null || weaponPivot == null)
            return;

        GameObject prefab = Resources.Load<GameObject>("Weapons/Bow");
        if (prefab == null)
            return;

        bowModel = Instantiate(prefab, weaponPivot, false);
        bowModel.name = "Player Bow";
        bowModel.transform.localPosition = new Vector3(0.30f, 0.02f, 0.22f);
        bowModel.transform.localRotation = Quaternion.identity;
        bowModel.transform.localScale = Vector3.one * 1.25f;
        bowModel.SetActive(false);
    }

    private void Start()
    {
        // Re-apply the current class after all scene objects have finished Awake.
        // This guarantees that the starter greatsword and the selected Greatsword class are visible.
        SetWeaponClass(weaponClass);

        if (shieldPivot != null)
        {
            shieldIdleRotation = shieldPivot.localRotation;
            shieldIdlePosition = shieldPivot.localPosition;
        }
    }

    private void Update()
    {
        if (!controlsEnabled)
            return;

        if (Time.time - lastAttackTime > comboResetTime)
            comboIndex = 0;

        if (AttackPressed() && !busy)
        {
            if (!TryStartFinalStrike())
                StartCoroutine(AttackRoutine());
        }

        if (ParryPressed() && !busy && Time.time >= nextParryTime)
            StartCoroutine(ParryRoutine());

        if (ActiveSkillPressed() && !busy && weaponClass != PlayerWeaponClass.Unchosen && Time.time >= nextActiveSkillTime)
            StartCoroutine(ActiveSkillRoutine());

    }

    public bool TryParry()
    {
        return parrying;
    }

    public bool TryParry(float requiredWindow)
    {
        if (!parrying)
            return false;

        float elapsed = Time.time - parryStartedTime;
        return elapsed >= 0f && elapsed <= Mathf.Min(parryWindow, requiredWindow);
    }

    private bool TryStartFinalStrike()
    {
        BossAI target = FindFinalStrikeTarget();
        if (target == null)
            return false;

        StartCoroutine(FinalStrikeRoutine(target));
        return true;
    }

    private BossAI FindFinalStrikeTarget()
    {
        BossAI[] bosses = FindObjectsByType<BossAI>(FindObjectsSortMode.None);
        BossAI best = null;
        float nearest = float.MaxValue;

        foreach (BossAI candidate in bosses)
        {
            if (candidate == null ||
                !candidate.gameObject.activeInHierarchy ||
                !candidate.CanReceiveFinalStrike)
                continue;

            Vector3 delta = candidate.transform.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            if (distance > finalStrikeRange || distance >= nearest)
                continue;

            if (delta.sqrMagnitude > 0.0001f &&
                Vector3.Angle(transform.forward, delta.normalized) > finalStrikeAngle * 0.5f)
                continue;

            best = candidate;
            nearest = distance;
        }

        return best;
    }

    private IEnumerator FinalStrikeRoutine(BossAI target)
    {
        busy = true;
        parrying = false;

        if (target == null || !target.CanReceiveFinalStrike)
        {
            busy = false;
            yield break;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

        if (visual != null)
            visual.PlayAttack(2);

        GameAudioManager.Instance.PlayPlayerSwing(2);
        yield return new WaitForSeconds(finalStrikeImpactDelay);

        if (target != null && target.CanReceiveFinalStrike)
        {
            target.ExecuteFinalStrike();
            GameAudioManager.Instance.PlayWeaponHit();
        }

        yield return new WaitForSeconds(0.22f);
        comboIndex = 0;
        busy = false;
    }

    private IEnumerator AttackRoutine()
    {
        busy = true;
        FaceLockedTarget();
        int attack = comboIndex;
        lastAttackTime = Time.time;
        if (visual != null) visual.PlayAttack(attack);

        Quaternion start = weaponPivot != null ? weaponPivot.localRotation : Quaternion.identity;
        Vector3 startPosition = weaponPivot != null ? weaponPivot.localPosition : Vector3.zero;

        Quaternion windup;
        Quaternion contact;
        Vector3 windupPosition = startPosition;
        Vector3 contactPosition = startPosition;
        float windupTime;
        float contactTime;

        switch (attack)
        {
            case 0: // diagonal forehand
                windup = start * Quaternion.Euler(-20f, -65f, 35f);
                contact = start * Quaternion.Euler(35f, 70f, -55f);
                windupPosition += new Vector3(-0.05f, 0.08f, -0.08f);
                contactPosition += new Vector3(0.08f, -0.04f, 0.18f);
                windupTime = 0.14f;
                contactTime = 0.11f;
                break;
            case 1: // reverse horizontal cut
                windup = start * Quaternion.Euler(5f, 80f, -45f);
                contact = start * Quaternion.Euler(-10f, -85f, 45f);
                windupPosition += new Vector3(0.05f, 0.02f, -0.10f);
                contactPosition += new Vector3(-0.06f, 0.01f, 0.20f);
                windupTime = 0.16f;
                contactTime = 0.12f;
                break;
            default: // committed overhead finisher
                windup = start * Quaternion.Euler(-105f, 0f, 8f);
                contact = start * Quaternion.Euler(65f, 0f, -12f);
                windupPosition += new Vector3(0f, 0.18f, -0.12f);
                contactPosition += new Vector3(0f, -0.08f, 0.24f);
                windupTime = 0.22f;
                contactTime = 0.14f;
                break;
        }

        float classSpeed = weaponClass == PlayerWeaponClass.Bow ? 1.35f : weaponClass == PlayerWeaponClass.Greatsword ? 0.82f : 1f;
        float passiveSpeed = weaponClass == PlayerWeaponClass.Bow ? 1f + skillRanks[5] * 0.03f : weaponClass == PlayerWeaponClass.Greatsword ? 1f + skillRanks[8] * 0.02f : 1f;
        float effectiveSpeed = Mathf.Max(0.35f, attackSpeedMultiplier * classSpeed * passiveSpeed);
        float authoredImpactDelay = visual != null ? visual.GetAttackImpactDelay(attack) : -1f;
        if (authoredImpactDelay > 0f)
        {
            authoredImpactDelay /= Mathf.Max(0.35f, attackSpeedMultiplier);
            windupTime = authoredImpactDelay * 0.82f;
            contactTime = authoredImpactDelay * 0.18f;
        }
        else
        {
            windupTime /= effectiveSpeed;
            contactTime /= effectiveSpeed;
        }

        yield return MoveAndRotateWeapon(startPosition, windupPosition, start, windup, windupTime);

        // Play the whoosh when the blade leaves the wind-up pose, not when the button is pressed.
        GameAudioManager.Instance.PlayPlayerSwing(attack);
        yield return MoveAndRotateWeapon(windupPosition, contactPosition, windup, contact, contactTime);
        int baseDamage = comboDamage[Mathf.Clamp(attack, 0, comboDamage.Length - 1)];
        float classDamage = weaponClass == PlayerWeaponClass.Bow ? 0.82f : weaponClass == PlayerWeaponClass.Greatsword ? 1.28f : 1f;
        float skillBonus = 1f + skillRanks[0] * 0.025f;
        skillBonus *= PassiveDamageMultiplier();
        DealDamage(Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageMultiplier * classDamage * skillBonus)));
        yield return new WaitForSeconds(0.045f);
        float recoveryTime = authoredImpactDelay > 0f
            ? (attack == 2 ? 0.40f : 0.32f) / Mathf.Max(0.35f, attackSpeedMultiplier)
            : (attack == 2 ? 0.20f : 0.15f) / effectiveSpeed;
        yield return MoveAndRotateWeapon(contactPosition, startPosition, contact, start, recoveryTime);

        comboIndex = (comboIndex + 1) % comboDamage.Length;
        busy = false;
    }

    private IEnumerator ParryRoutine()
    {
        busy = true;
        parrying = true;
        parryStartedTime = Time.time;
        if (visual != null) visual.PlayParry();
        nextParryTime = Time.time + parryCooldown;

        Quaternion guardRotation = Quaternion.Euler(-18f, 0f, 0f);
        Vector3 guardPosition = shieldIdlePosition + new Vector3(0f, 0.08f, 0.48f);

        yield return MoveAndRotateShield(
            shieldIdlePosition,
            guardPosition,
            shieldIdleRotation,
            guardRotation,
            0.07f
        );

        yield return new WaitForSeconds(parryWindow);

        parrying = false;

        yield return MoveAndRotateShield(
            guardPosition,
            shieldIdlePosition,
            guardRotation,
            shieldIdleRotation,
            0.10f
        );

        yield return new WaitForSeconds(0.08f);
        busy = false;
    }


    private float GetActiveSkillCooldown(int slot)
    {
        slot = Mathf.Clamp(slot, 0, PlayerProgression.ActiveSkillCount - 1);
        float[] cooldowns;
        switch (weaponClass)
        {
            case PlayerWeaponClass.Bow: cooldowns = new[] { 5.0f, 7.5f, 4.0f, 10.0f, 14.0f }; break;
            default: cooldowns = new[] { 7.0f, 10.5f, 14.0f, 12.0f, 9.0f }; break;
        }
        float reduction = weaponClass == PlayerWeaponClass.Bow ? skillRanks[7] * 0.02f : 0f;
        return cooldowns[slot] * Mathf.Clamp(1f - reduction, 0.65f, 1f);
    }

    private IEnumerator ActiveSkillRoutine()
    {
        busy = true;
        currentActiveSkillCooldown = GetActiveSkillCooldown(equippedActiveSkill);
        nextActiveSkillTime = Time.time + currentActiveSkillCooldown;
        int rank = skillRanks[Mathf.Clamp(equippedActiveSkill, 0, PlayerProgression.ActiveSkillCount - 1)];

        switch (weaponClass)
        {
            case PlayerWeaponClass.Bow:
                yield return BowSkill(equippedActiveSkill, rank);
                break;
            default:
                yield return GreatswordSkill(equippedActiveSkill % 3, rank + equippedActiveSkill);
                break;
        }

        ResetEquipmentPivots();
        busy = false;
    }

    private IEnumerator GreatswordSkill(int slot, int rank)
    {
        Quaternion weaponStart = weaponPivot != null ? weaponPivot.localRotation : Quaternion.identity;
        Vector3 weaponPos = weaponPivot != null ? weaponPivot.localPosition : Vector3.zero;

        if (slot == 0) // Breaker Slash: guarded wind-up into a powerful diagonal cut.
        {
            if (visual != null) visual.PlayParry();
            Quaternion guard = weaponStart * Quaternion.Euler(-35f, -25f, 55f);
            Quaternion impact = weaponStart * Quaternion.Euler(20f, 115f, -70f);
            yield return MoveAndRotateWeapon(weaponPos, weaponPos + new Vector3(-0.10f, 0.12f, -0.10f), weaponStart, guard, 0.18f / attackSpeedMultiplier);
            GameAudioManager.Instance.PlayPlayerSwing(1);
            yield return MoveAndRotateWeapon(weaponPos + new Vector3(-0.10f, 0.12f, -0.10f), weaponPos + new Vector3(0.14f, -0.04f, 0.30f), guard, impact, 0.15f / attackSpeedMultiplier);
            DealDamage(Mathf.RoundToInt((50 + rank * 10) * damageMultiplier * PassiveDamageMultiplier()));
            yield return new WaitForSeconds(0.15f);
        }
        else if (slot == 1) // Greatsword Rush: advancing shoulder-led horizontal cleave.
        {
            if (visual != null) visual.PlayAttack(1);
            Quaternion ready = weaponStart * Quaternion.Euler(0f, -100f, 55f);
            yield return MoveAndRotateWeapon(weaponPos, weaponPos + new Vector3(-0.12f, 0f, -0.08f), weaponStart, ready, 0.13f / attackSpeedMultiplier);
            yield return DashBodyForward(3.1f, 0.22f);
            GameAudioManager.Instance.PlayPlayerSwing(2);
            yield return MoveAndRotateWeapon(weaponPos + new Vector3(-0.12f, 0f, -0.08f), weaponPos + new Vector3(0.18f, 0f, 0.34f), ready, weaponStart * Quaternion.Euler(0f, 115f, -65f), 0.13f / attackSpeedMultiplier);
            DealDamage(Mathf.RoundToInt((62 + rank * 11) * damageMultiplier * PassiveDamageMultiplier()));
            yield return new WaitForSeconds(0.16f);
        }
        else // Execution Cleave: slow overhead strike with the highest impact.
        {
            if (visual != null) visual.PlayAttack(2);
            Quaternion windup = weaponStart * Quaternion.Euler(-145f, 0f, 8f);
            Quaternion impact = weaponStart * Quaternion.Euler(85f, 0f, -18f);
            yield return MoveAndRotateWeapon(weaponPos, weaponPos + new Vector3(0f, 0.30f, -0.18f), weaponStart, windup, 0.30f / attackSpeedMultiplier);
            GameAudioManager.Instance.PlayPlayerSwing(2);
            yield return MoveAndRotateWeapon(weaponPos + new Vector3(0f, 0.30f, -0.18f), weaponPos + new Vector3(0f, -0.10f, 0.38f), windup, impact, 0.17f / attackSpeedMultiplier);
            DealDamage(Mathf.RoundToInt((66 + rank * 13) * damageMultiplier));
            yield return new WaitForSeconds(0.22f);
        }
    }

    private IEnumerator BowSkill(int slot, int rank)
    {
        FaceLockedTarget();
        if (visual != null)
            visual.PlayBowShot();

        float drawTime = slot == 2 ? 0.14f : slot == 1 || slot == 4 ? 0.38f : 0.24f;
        yield return new WaitForSeconds(drawTime / Mathf.Max(0.5f, attackSpeedMultiplier));

        CombatantHealth target = FindArrowTarget();
        Vector3 origin = transform.position + Vector3.up * 1.15f + transform.forward * 0.55f;
        Vector3 destination = target != null
            ? target.transform.position + Vector3.up * 0.85f
            : origin + transform.forward * 24f;
        float speed = (slot == 1 || slot == 4 ? 22f : 28f) * (1f + skillRanks[9] * 0.05f);
        int damage = Mathf.RoundToInt((28f + slot * 8f + rank * 6f) * damageMultiplier * (1f + skillRanks[6] * 0.04f));
        yield return FlyArrow(origin, destination, speed, target, damage);
        yield return new WaitForSeconds(slot == 2 ? 0.08f : 0.16f);
    }

    private CombatantHealth FindArrowTarget()
    {
        if (lockOn != null && lockOn.IsLockedOn && lockOn.CurrentTarget != null)
            return lockOn.CurrentTarget.GetComponent<CombatantHealth>();

        CombatantHealth best = null;
        float bestScore = float.MaxValue;
        foreach (CombatantHealth candidate in FindObjectsByType<CombatantHealth>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.gameObject == gameObject || candidate.IsDead || !candidate.gameObject.activeInHierarchy)
                continue;
            Vector3 delta = candidate.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, delta);
            if (delta.magnitude > 24f || angle > 35f)
                continue;
            float score = delta.magnitude + angle * 0.08f;
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }
        return best;
    }

    private IEnumerator FlyArrow(Vector3 origin, Vector3 destination, float speed, CombatantHealth target, int damage)
    {
        GameObject arrow = arrowPrefab != null ? Instantiate(arrowPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrow.name = "Player Arrow";
        arrow.transform.position = origin;
        arrow.transform.localScale = arrowPrefab != null ? Vector3.one : new Vector3(0.025f, 0.32f, 0.025f);
        Vector3 direction = (destination - origin).normalized;
        arrow.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        float distance = Vector3.Distance(origin, destination);
        float duration = distance / Mathf.Max(1f, speed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            arrow.transform.position = Vector3.Lerp(origin, destination, t);
            yield return null;
        }

        if (target != null && !target.IsDead)
        {
            target.TakeDamage(Mathf.Max(1, damage));
            GameAudioManager.Instance.PlayWeaponHit();
        }
        Destroy(arrow);
    }

    private IEnumerator DualSwordSkill(int slot, int rank)
    {
        Quaternion rightStart = weaponPivot != null ? weaponPivot.localRotation : Quaternion.identity;
        Quaternion leftStart = shieldPivot != null ? shieldPivot.localRotation : Quaternion.identity;
        Vector3 rightPos = weaponPivot != null ? weaponPivot.localPosition : Vector3.zero;
        Vector3 leftPos = shieldPivot != null ? shieldPivot.localPosition : Vector3.zero;

        if (slot == 0) // Rapid Chain: alternating right/left strikes.
        {
            if (visual != null) visual.PlayAttack(0);
            for (int i = 0; i < 4; i++)
            {
                bool right = i % 2 == 0;
                Transform pivot = right ? weaponPivot : shieldPivot;
                Quaternion from = right ? rightStart : leftStart;
                Vector3 pos = right ? rightPos : leftPos;
                Quaternion cut = from * Quaternion.Euler(10f, right ? 95f : -95f, right ? -55f : 55f);
                yield return MovePivot(pivot, pos, pos + new Vector3(right ? 0.08f : -0.08f, 0f, 0.22f), from, cut, 0.075f / attackSpeedMultiplier);
                GameAudioManager.Instance.PlayPlayerSwing(i % 3);
                DealDamage(Mathf.RoundToInt((16 + rank * 3) * damageMultiplier * PassiveDamageMultiplier()));
                yield return MovePivot(pivot, pos + new Vector3(right ? 0.08f : -0.08f, 0f, 0.22f), pos, cut, from, 0.055f / attackSpeedMultiplier);
            }
        }
        else if (slot == 1) // Shadow Step: evasive forward burst into a crossing cut.
        {
            if (visual != null) visual.PlayAttack(1);
            yield return DashBodyForward(3.6f, 0.16f);
            Quaternion rightCross = rightStart * Quaternion.Euler(0f, 110f, -70f);
            Quaternion leftCross = leftStart * Quaternion.Euler(0f, -110f, 70f);
            Coroutine leftMove = StartCoroutine(MovePivot(shieldPivot, leftPos, leftPos + new Vector3(-0.10f, 0f, 0.25f), leftStart, leftCross, 0.11f));
            yield return MovePivot(weaponPivot, rightPos, rightPos + new Vector3(0.10f, 0f, 0.25f), rightStart, rightCross, 0.11f);
            if (leftMove != null) yield return leftMove;
            GameAudioManager.Instance.PlayPlayerSwing(1);
            DealDamage(Mathf.RoundToInt((48 + rank * 9) * damageMultiplier * PassiveDamageMultiplier()));
            yield return new WaitForSeconds(0.12f);
        }
        else // Cross Rend: both blades cut inward together.
        {
            if (visual != null) visual.PlayAttack(2);
            Quaternion rightWind = rightStart * Quaternion.Euler(-15f, -85f, 40f);
            Quaternion leftWind = leftStart * Quaternion.Euler(-15f, 85f, -40f);
            yield return MovePivot(weaponPivot, rightPos, rightPos + new Vector3(-0.08f, 0.10f, -0.08f), rightStart, rightWind, 0.13f);
            yield return MovePivot(shieldPivot, leftPos, leftPos + new Vector3(0.08f, 0.10f, -0.08f), leftStart, leftWind, 0.05f);
            GameAudioManager.Instance.PlayPlayerSwing(2);
            Coroutine leftCut = StartCoroutine(MovePivot(shieldPivot, leftPos + new Vector3(0.08f, 0.10f, -0.08f), leftPos + new Vector3(-0.10f, -0.02f, 0.28f), leftWind, leftStart * Quaternion.Euler(20f, -95f, 60f), 0.12f));
            yield return MovePivot(weaponPivot, rightPos + new Vector3(-0.08f, 0.10f, -0.08f), rightPos + new Vector3(0.10f, -0.02f, 0.28f), rightWind, rightStart * Quaternion.Euler(20f, 95f, -60f), 0.12f);
            if (leftCut != null) yield return leftCut;
            DealDamage(Mathf.RoundToInt((48 + rank * 10) * damageMultiplier));
            yield return new WaitForSeconds(0.16f);
        }
    }

    private IEnumerator SpearSkill(int slot, int rank)
    {
        Quaternion startRot = weaponPivot != null ? weaponPivot.localRotation : Quaternion.identity;
        Vector3 startPos = weaponPivot != null ? weaponPivot.localPosition : Vector3.zero;

        if (slot == 0) // Long Thrust: straight, extended polearm stab.
        {
            if (visual != null) visual.PlayAttack(1);
            Quaternion aim = startRot * Quaternion.Euler(-5f, 0f, 0f);
            yield return MoveAndRotateWeapon(startPos, startPos + new Vector3(0f, 0f, 0.75f), startRot, aim, 0.16f / attackSpeedMultiplier);
            GameAudioManager.Instance.PlayPlayerSwing(0);
            float oldRange = attackRange;
            attackRange = 4.6f;
            DealDamage(Mathf.RoundToInt((46 + rank * 9) * damageMultiplier * 1.15f * PassiveDamageMultiplier()));
            attackRange = oldRange;
            yield return new WaitForSeconds(0.14f);
        }
        else if (slot == 1) // Vault Strike: short leap/advance and descending spear hit.
        {
            if (visual != null) visual.PlayAttack(2);
            yield return VaultForward(3.2f, 0.28f, 0.85f);
            Quaternion down = startRot * Quaternion.Euler(65f, 0f, 0f);
            yield return MoveAndRotateWeapon(startPos + new Vector3(0f, 0.18f, -0.08f), startPos + new Vector3(0f, -0.10f, 0.50f), startRot, down, 0.12f);
            GameAudioManager.Instance.PlayPlayerSwing(2);
            DealDamage(Mathf.RoundToInt((58 + rank * 10) * damageMultiplier * 1.15f * PassiveDamageMultiplier()));
            yield return new WaitForSeconds(0.16f);
        }
        else // Impale: braced charge into a powerful thrust.
        {
            if (visual != null) visual.PlayAttack(2);
            Quaternion brace = startRot * Quaternion.Euler(-10f, 0f, -12f);
            yield return MoveAndRotateWeapon(startPos, startPos + new Vector3(0f, 0f, -0.18f), startRot, brace, 0.18f);
            yield return DashBodyForward(4.2f, 0.24f);
            GameAudioManager.Instance.PlayPlayerSwing(2);
            float oldRange = attackRange;
            attackRange = 4.9f;
            DealDamage(Mathf.RoundToInt((58 + rank * 12) * damageMultiplier * 1.15f));
            attackRange = oldRange;
            yield return new WaitForSeconds(0.18f);
        }
    }

    private IEnumerator DashBodyForward(float distance, float duration)
    {
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * distance;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.0001f)));
            yield return null;
        }
    }

    private IEnumerator VaultForward(float distance, float duration, float height)
    {
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * distance;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.0001f));
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * height;
            transform.position = position;
            yield return null;
        }
    }

    private IEnumerator MovePivot(Transform pivot, Vector3 fromPosition, Vector3 toPosition, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        if (pivot == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.0001f));
            float eased = t * t * (3f - 2f * t);
            pivot.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, eased);
            pivot.localRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, eased);
            yield return null;
        }
        pivot.localPosition = toPosition;
        pivot.localRotation = toRotation;
    }

    private void ResetEquipmentPivots()
    {
        if (weaponPivot != null)
        {
            weaponPivot.localPosition = Vector3.zero;
            weaponPivot.localRotation = Quaternion.identity;
        }
        if (shieldPivot != null)
        {
            shieldPivot.localPosition = shieldIdlePosition;
            shieldPivot.localRotation = shieldIdleRotation;
        }
    }

    public void ApplyBossParried(float duration = 0.65f)
    {
        if (parriedRoutine != null)
            StopCoroutine(parriedRoutine);
        parriedRoutine = StartCoroutine(BossParriedRoutine(duration));
    }

    private IEnumerator BossParriedRoutine(float duration)
    {
        busy = true;
        parrying = false;
        if (visual != null)
            visual.PlayStagger(duration);
        yield return new WaitForSeconds(duration);
        busy = false;
        parriedRoutine = null;
    }

    private IEnumerator RotateTransform(
        Transform target,
        Quaternion from,
        Quaternion to,
        float duration)
    {
        if (target == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.localRotation = Quaternion.Slerp(
                from,
                to,
                Mathf.Clamp01(elapsed / duration)
            );
            yield return null;
        }

        target.localRotation = to;
    }

    private IEnumerator MoveAndRotateWeapon(
        Vector3 fromPosition,
        Vector3 toPosition,
        Quaternion fromRotation,
        Quaternion toRotation,
        float duration)
    {
        if (visual != null && visual.UsesAuthoredAnimations)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }
        if (weaponPivot == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.0001f));
            float eased = t * t * (3f - 2f * t);
            weaponPivot.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, eased);
            weaponPivot.localRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, eased);
            yield return null;
        }

        weaponPivot.localPosition = toPosition;
        weaponPivot.localRotation = toRotation;
    }

    private IEnumerator MoveAndRotateShield(
        Vector3 fromPosition,
        Vector3 toPosition,
        Quaternion fromRotation,
        Quaternion toRotation,
        float duration)
    {
        if (visual != null && visual.UsesAuthoredAnimations)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }
        if (shieldPivot == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            shieldPivot.localPosition = Vector3.Lerp(
                fromPosition,
                toPosition,
                t
            );

            shieldPivot.localRotation = Quaternion.Slerp(
                fromRotation,
                toRotation,
                t
            );

            yield return null;
        }

        shieldPivot.localPosition = toPosition;
        shieldPivot.localRotation = toRotation;
    }

    private float PassiveDamageMultiplier()
    {
        switch (weaponClass)
        {
            case PlayerWeaponClass.Greatsword: return 1f + skillRanks[5] * 0.04f;
            case PlayerWeaponClass.Bow: return 1f + skillRanks[8] * 0.03f;
            default: return 1f;
        }
    }

    private float PassiveRangeBonus()
    {
        return 0f;
    }

    private void FaceLockedTarget()
    {
        if (lockOn == null || !lockOn.IsLockedOn)
            return;

        Vector3 direction = lockOn.CurrentTarget.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void DealDamage(int damage)
    {
        CombatantHealth[] targets =
            FindObjectsByType<CombatantHealth>(FindObjectsSortMode.None);

        CombatantHealth lockedHealth = null;
        if (lockOn != null && lockOn.IsLockedOn && lockOn.CurrentTarget != null)
            lockedHealth = lockOn.CurrentTarget.GetComponentInParent<CombatantHealth>();

        CombatantHealth best = null;
        float nearest = float.MaxValue;

        foreach (CombatantHealth target in targets)
        {
            if (target.gameObject == gameObject ||
                target.IsDead ||
                !target.gameObject.activeInHierarchy)
                continue;

            Vector3 origin = transform.position + Vector3.up * 0.9f;
            Vector3 targetPoint = target.transform.position;
            Collider targetCollider = target.GetComponentInChildren<Collider>();
            if (targetCollider != null)
                targetPoint = targetCollider.ClosestPoint(origin);

            Vector3 delta = targetPoint - origin;
            delta.y = 0f;
            float distance = delta.magnitude;

            bool isLockedTarget = lockedHealth == target;
            float effectiveRange = attackRange + PassiveRangeBonus() + (isLockedTarget ? 0.75f : 0f);

            if (distance > effectiveRange)
                continue;

            Vector3 attackForward = transform.forward;
            attackForward.y = 0f;
            if (!isLockedTarget &&
                delta.sqrMagnitude > 0.001f &&
                Vector3.Angle(attackForward.normalized, delta.normalized) > attackAngle * 0.5f)
                continue;

            if (distance < nearest)
            {
                best = target;
                nearest = distance;
            }
        }

        if (best != null)
        {
            BossAI boss = best.GetComponent<BossAI>();
            if (boss != null && boss.TryParryPlayerAttack())
            {
                GameAudioManager.Instance.PlayParry();
                ApplyBossParried();
                return;
            }

            float multiplier = boss != null ? boss.GetIncomingDamageMultiplier() : 1f;
            best.TakeDamage(Mathf.RoundToInt(damage * multiplier));
            GameAudioManager.Instance.PlayWeaponHit();
        }
    }


    private static bool ActiveSkillPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

    private static bool SelectSkillPressed(int slot)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        return slot == 0 ? keyboard.zKey.wasPressedThisFrame : slot == 1 ? keyboard.xKey.wasPressedThisFrame : keyboard.cKey.wasPressedThisFrame;
#else
        return slot == 0 ? Input.GetKeyDown(KeyCode.Z) : slot == 1 ? Input.GetKeyDown(KeyCode.X) : Input.GetKeyDown(KeyCode.C);
#endif
    }

    private static bool AttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static bool ParryPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.rightButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(1);
#endif
    }
}
