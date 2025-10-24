using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHp = 5;

    public int MaxHp => maxHp;
    public int CurrentHp { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action OnDied;

    void Awake()
    {
        CurrentHp = Mathf.Max(1, maxHp);
        IsDead = false;
        RaiseChanged();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        int dmg = Mathf.Max(0, amount);
        if (dmg == 0) return;

        CurrentHp = Mathf.Max(0, CurrentHp - dmg);
        Debug.Log($"[HP] Player: {CurrentHp}/{maxHp}");
        
        Enemy.KillAll();

        RaiseChanged();

        if (CurrentHp <= 0 && !IsDead)
        {
            IsDead = true;
            Debug.LogWarning("[HP] Player Dead");
            OnDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        int heal = Mathf.Max(0, amount);
        if (heal == 0) return;

        int before = CurrentHp;
        CurrentHp = Mathf.Min(maxHp, CurrentHp + heal);
        if (CurrentHp != before) RaiseChanged();
    }

    public void SetMaxHp(int newMax, bool refillToFull = false)
    {
        maxHp = Mathf.Max(1, newMax);
        if (refillToFull) CurrentHp = maxHp;
        CurrentHp = Mathf.Clamp(CurrentHp, 0, maxHp);
        RaiseChanged();
    }

    public void ResetFull()
    {
        IsDead = false;
        CurrentHp = maxHp;
        RaiseChanged();
    }

    void RaiseChanged() => OnHealthChanged?.Invoke(CurrentHp, maxHp);
}