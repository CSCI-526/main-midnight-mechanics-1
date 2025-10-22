using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHp = 5;

    public int MaxHp => maxHp;
    public int CurrentHp { get; private set; }

    // current, max
    public System.Action<int, int> OnHealthChanged;

    void Awake()
    {
        CurrentHp = Mathf.Max(1, maxHp);
        RaiseChanged();
    }

    public void TakeDamage(int amount)
    {
        int dmg = Mathf.Max(0, amount);
        if (dmg <= 0 || CurrentHp <= 0) return;

        CurrentHp = Mathf.Max(0, CurrentHp - dmg);
        Debug.Log($"[HP] Player: {CurrentHp}/{maxHp}");

        Enemy.KillAll(); // 你的清屏逻辑

        RaiseChanged();

        if (CurrentHp <= 0)
        {
            Debug.LogWarning("[HP] Player Dead");
            // TODO: Game Over 流程
        }
    }

    public void Heal(int amount)
    {
        int heal = Mathf.Max(0, amount);
        if (heal <= 0 || CurrentHp <= 0) return;
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

    public void ForceNotify() => RaiseChanged();

    void RaiseChanged() => OnHealthChanged?.Invoke(CurrentHp, maxHp);
}