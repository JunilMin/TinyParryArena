using System;
using UnityEngine;

public sealed class CombatantHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;
    public bool IsInvincible { get; private set; }

    public event Action<CombatantHealth> Died;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void Configure(int value)
    {
        maxHealth = Mathf.Max(1, value);
        CurrentHealth = maxHealth;
    }

    public void ConfigureMaxHealth(int value, bool refill)
    {
        int previousMax = Mathf.Max(1, maxHealth);
        float ratio = Mathf.Clamp01((float)CurrentHealth / previousMax);
        maxHealth = Mathf.Max(1, value);
        CurrentHealth = refill ? maxHealth : Mathf.Clamp(Mathf.RoundToInt(maxHealth * ratio), 1, maxHealth);
    }

    public void ForceKill()
    {
        if (IsDead) return;
        bool previous = IsInvincible;
        IsInvincible = false;
        TakeDamage(Mathf.Max(1, CurrentHealth));
        IsInvincible = previous;
    }

    public int Heal(int amount)
    {
        if (IsDead || amount <= 0) return 0;
        int before = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        return CurrentHealth - before;
    }

    public void SetInvincible(bool value) => IsInvincible = value;

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0 || IsInvincible) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

        if (IsDead)
        {
            GameAudioManager.Instance.PlayDeath();
            Died?.Invoke(this);
        }
        else
        {
            GameAudioManager.Instance.PlayHurt();
        }
    }
}