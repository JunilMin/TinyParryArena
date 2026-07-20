using UnityEngine;

public enum PlayerWeaponClass
{
    Unchosen,
    Greatsword,
    DualSwords,
    Spear
}

public enum SkillKind
{
    Active,
    Passive
}

public sealed class WeaponSkillInfo
{
    public readonly string Name;
    public readonly SkillKind Kind;
    public readonly string Description;
    public readonly float Cooldown;
    public readonly float DamagePercent;

    public WeaponSkillInfo(string name, SkillKind kind, string description, float cooldown = 0f, float damagePercent = 0f)
    {
        Name = name;
        Kind = kind;
        Description = description;
        Cooldown = cooldown;
        DamagePercent = damagePercent;
    }
}

[RequireComponent(typeof(CombatantHealth))]
[RequireComponent(typeof(PlayerCombat))]
public sealed class PlayerProgression : MonoBehaviour
{
    public const int ActiveSkillCount = 5;
    public const int PassiveSkillCount = 5;
    public const int TotalSkillCount = ActiveSkillCount + PassiveSkillCount;
    public const int MaxSkillRank = 5;

    [SerializeField] private int vitality = 10;
    [SerializeField] private int power = 10;
    [SerializeField] private int agility = 10;

    private int statPoints;
    private int skillPoints;
    private readonly int[] learnedSkills = new int[TotalSkillCount];
    private PlayerWeaponClass weaponClass = PlayerWeaponClass.Unchosen;
    private int equippedActiveSkill;
    private CombatantHealth health;
    private PlayerCombat combat;

    public int Vitality => vitality;
    public int Power => power;
    public int Agility => agility;

    // Character Status HUD values.
    public int CurrentHealth => health != null ? health.CurrentHealth : 0;
    public int MaxHealth => health != null ? health.MaxHealth : 0;
    public int AttackPowerPercent => Mathf.RoundToInt((0.86f + (power - 10) * 0.06f) * 100f);
    public int AttackSpeedPercent => Mathf.RoundToInt((0.92f + (agility - 10) * 0.035f) * 100f);

    public int StatPoints => statPoints;
    public int SkillPoints => skillPoints;
    public PlayerWeaponClass WeaponClass => weaponClass;
    public bool HasChosenClass => weaponClass != PlayerWeaponClass.Unchosen;
    public int EquippedActiveSkill => equippedActiveSkill;

    private static readonly WeaponSkillInfo[] GreatswordSkills =
    {
        new WeaponSkillInfo("Breaker Slash", SkillKind.Active, "A heavy diagonal cut that deals high posture and health damage. Reliable after a successful parry.", 7.0f, 210f),
        new WeaponSkillInfo("Iron Rush", SkillKind.Active, "Rush forward behind the greatsword and finish with a wide horizontal cleave.", 10.5f, 245f),
        new WeaponSkillInfo("Execution Cleave", SkillKind.Active, "A slow overhead execution strike. Very high damage, but easy to punish if mistimed.", 14.0f, 310f),
        new WeaponSkillInfo("Earthsplitter", SkillKind.Active, "Drive the blade down with a short shock impact that excels at staggering nearby bosses.", 12.0f, 275f),
        new WeaponSkillInfo("King's Reprisal", SkillKind.Active, "A compact counter-cut. Deals bonus damage when used shortly after a successful parry.", 9.0f, 235f),
        new WeaponSkillInfo("Colossus Strength", SkillKind.Passive, "Each rank increases all greatsword damage by 4%.") ,
        new WeaponSkillInfo("Heavy Guard", SkillKind.Passive, "Each rank reduces damage taken during failed or late parries by 3%.") ,
        new WeaponSkillInfo("Poise Breaker", SkillKind.Passive, "Each rank increases boss stagger damage by 6%.") ,
        new WeaponSkillInfo("Measured Swing", SkillKind.Passive, "Each rank improves greatsword attack speed by 2% without reducing impact.") ,
        new WeaponSkillInfo("Perfect Punishment", SkillKind.Passive, "Each rank increases the next hit after a perfect parry by 7%.")
    };

    private static readonly WeaponSkillInfo[] DualSwordSkills =
    {
        new WeaponSkillInfo("Twin Fang", SkillKind.Active, "Four alternating cuts delivered rapidly. Strong sustained damage with a short cooldown.", 5.5f, 190f),
        new WeaponSkillInfo("Blade Rush", SkillKind.Active, "Dash through the target and finish with a crossing slash.", 8.0f, 225f),
        new WeaponSkillInfo("Cross Tempest", SkillKind.Active, "A fast spinning sequence that deals repeated damage at close range.", 11.0f, 265f),
        new WeaponSkillInfo("Shadow Reversal", SkillKind.Active, "Step backward briefly, then return with two counter-slashes.", 9.5f, 230f),
        new WeaponSkillInfo("Hundred Cuts", SkillKind.Active, "A committed flurry with the highest dual-sword damage, but a long recovery.", 14.5f, 300f),
        new WeaponSkillInfo("Flow Mastery", SkillKind.Passive, "Each rank increases attack speed by 3%.") ,
        new WeaponSkillInfo("Relentless Edge", SkillKind.Passive, "Each rank increases damage of consecutive hits by 2%, stacking up to five times.") ,
        new WeaponSkillInfo("Quick Recovery", SkillKind.Passive, "Each rank reduces active-skill cooldowns by 2%.") ,
        new WeaponSkillInfo("Evasive Steel", SkillKind.Passive, "Each rank increases the first attack after a dodge by 5%.") ,
        new WeaponSkillInfo("Parry Tempo", SkillKind.Passive, "Each rank grants 2% attack speed for three seconds after a successful parry.")
    };

    private static readonly WeaponSkillInfo[] SpearSkills =
    {
        new WeaponSkillInfo("Long Thrust", SkillKind.Active, "A long-reaching thrust that is safe, precise, and strong at punishing recovery windows.", 6.0f, 205f),
        new WeaponSkillInfo("Vault Strike", SkillKind.Active, "Leap forward into a descending spear strike with strong stagger damage.", 9.0f, 245f),
        new WeaponSkillInfo("Impale", SkillKind.Active, "A committed piercing attack with excellent single-target damage and reach.", 12.5f, 285f),
        new WeaponSkillInfo("Sweeping Horizon", SkillKind.Active, "A broad circular sweep designed to catch lateral movement.", 10.0f, 235f),
        new WeaponSkillInfo("Dragonpiercer", SkillKind.Active, "Charge briefly, then release the spear's strongest straight-line thrust.", 15.0f, 320f),
        new WeaponSkillInfo("Reach Mastery", SkillKind.Passive, "Each rank increases spear attack range by 4%.") ,
        new WeaponSkillInfo("Counterpoint", SkillKind.Passive, "Each rank increases damage after a successful parry by 5%.") ,
        new WeaponSkillInfo("Steady Hands", SkillKind.Passive, "Each rank increases thrust damage by 3%.") ,
        new WeaponSkillInfo("Hunter's Step", SkillKind.Passive, "Each rank slightly improves movement during spear attacks and skills.") ,
        new WeaponSkillInfo("Piercing Rhythm", SkillKind.Passive, "Each rank reduces spear active-skill cooldowns by 1.5%.")
    };

    private void Awake()
    {
        health = GetComponent<CombatantHealth>();
        combat = GetComponent<PlayerCombat>();
        ApplyStats(true);
        ApplySkills();
    }

    public void AwardStageClearPoints(int clearedStage)
    {
        statPoints++;
        if (clearedStage >= 4) skillPoints++;
    }

    public bool SpendStatPoint(int index)
    {
        if (statPoints <= 0) return false;
        switch (index)
        {
            case 0: vitality++; break;
            case 1: power++; break;
            case 2: agility++; break;
            default: return false;
        }
        statPoints--;
        ApplyStats(index == 0);
        return true;
    }

    public bool SpendSkillPoint(int slot)
    {
        if (!HasChosenClass || skillPoints <= 0 || slot < 0 || slot >= TotalSkillCount || learnedSkills[slot] >= MaxSkillRank)
            return false;
        learnedSkills[slot]++;
        skillPoints--;
        ApplySkills();
        return true;
    }

    public int GetSkillRank(int slot) => slot >= 0 && slot < TotalSkillCount ? learnedSkills[slot] : 0;
    public bool IsSkillLearned(int slot) => GetSkillRank(slot) > 0;

    public bool EquipActiveSkill(int slot)
    {
        if (!HasChosenClass || slot < 0 || slot >= ActiveSkillCount || !IsSkillLearned(slot))
            return false;
        equippedActiveSkill = slot;
        combat.EquipActiveSkill(slot);
        return true;
    }

    public string GetEquippedActiveSkillName() => HasChosenClass ? GetSkillName(equippedActiveSkill) : "LOCKED";
    public bool IsActiveSkill(int slot) => slot >= 0 && slot < ActiveSkillCount;
    public string GetSkillTypeLabel(int slot) => IsActiveSkill(slot) ? "A" : "P";

    public void ChooseClass(PlayerWeaponClass chosen)
    {
        if (chosen == PlayerWeaponClass.Unchosen) return;
        weaponClass = chosen;
        equippedActiveSkill = 0;
        learnedSkills[0] = Mathf.Max(1, learnedSkills[0]); // Every weapon starts with one usable active skill.
        combat.SetWeaponClass(chosen);
        ApplySkills();
        combat.EquipActiveSkill(equippedActiveSkill);
        ApplyStats(false);
    }

    public string GetClassName()
    {
        switch (weaponClass)
        {
            case PlayerWeaponClass.Greatsword: return "GREATSWORD";
            case PlayerWeaponClass.DualSwords: return "DUAL SWORDS";
            case PlayerWeaponClass.Spear: return "SPEAR";
            default: return "UNSELECTED";
        }
    }

    public WeaponSkillInfo GetSkillInfo(int slot)
    {
        WeaponSkillInfo[] skills = GetCurrentSkillSet();
        return skills[Mathf.Clamp(slot, 0, TotalSkillCount - 1)];
    }

    public string GetSkillName(int slot) => GetSkillInfo(slot).Name;
    public string GetSkillDescription(int slot) => GetSkillInfo(slot).Description;
    public float GetSkillCooldown(int slot) => GetSkillInfo(slot).Cooldown;
    public float GetSkillDamagePercent(int slot) => GetSkillInfo(slot).DamagePercent;

    private WeaponSkillInfo[] GetCurrentSkillSet()
    {
        switch (weaponClass)
        {
            case PlayerWeaponClass.DualSwords: return DualSwordSkills;
            case PlayerWeaponClass.Spear: return SpearSkills;
            default: return GreatswordSkills;
        }
    }

    private void ApplySkills()
    {
        if (combat != null) combat.SetSkillRanks(learnedSkills);
    }

    private void ApplyStats(bool refill)
    {
        int maxHp = 80 + (vitality - 10) * 8;
        float damageMultiplier = 0.86f + (power - 10) * 0.06f;
        float speedMultiplier = 0.92f + (agility - 10) * 0.035f;
        health.ConfigureMaxHealth(maxHp, refill);
        combat.SetProgressionMultipliers(damageMultiplier, speedMultiplier);
    }
}
