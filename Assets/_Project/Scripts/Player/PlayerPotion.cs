using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CombatantHealth))]
public sealed class PlayerPotion : MonoBehaviour
{
    [SerializeField] private int startingPotions = 10;
    [SerializeField] private int healAmount = 40;
    [SerializeField] private float useCooldown = 0.8f;

    private CombatantHealth health;
    private int remaining;
    private float nextUseTime;

    public int Remaining => remaining;
    public int HealAmount => healAmount;
    public event Action<int> PotionCountChanged;

    private void Awake()
    {
        health = GetComponent<CombatantHealth>();
        remaining = Mathf.Max(0, startingPotions);
    }

    private void Start() => PotionCountChanged?.Invoke(remaining);

    private void Update()
    {
        if (UsePotionPressed()) TryUsePotion();
    }

    public void ResetPotions()
    {
        remaining = Mathf.Max(0, startingPotions);
        nextUseTime = 0f;
        PotionCountChanged?.Invoke(remaining);
    }

    public void AddPotions(int amount)
    {
        if (amount <= 0) return;
        remaining += amount;
        PotionCountChanged?.Invoke(remaining);
    }

    public bool TryUsePotion()
    {
        if (health == null || health.IsDead || remaining <= 0 || Time.time < nextUseTime)
            return false;
        if (health.CurrentHealth >= health.MaxHealth)
            return false;

        int healed = health.Heal(healAmount);
        if (healed <= 0) return false;

        remaining--;
        nextUseTime = Time.time + useCooldown;
        PotionCountChanged?.Invoke(remaining);

        CharacterVisualController visual = GetComponent<CharacterVisualController>();
        if (visual != null) visual.PlayDrink();
        GameAudioManager.Instance.PlayPotion();
        return true;
    }

    private static bool UsePotionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}
