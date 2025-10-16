using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHp = 5;
    public int MaxHp => maxHp;
    public int CurrentHp { get; private set; }

    public event Action OnDied;
    private bool _dead = false;

    void Awake()
    {
        CurrentHp = maxHp;
        _dead = false;
    }
    
    public void TakeDamage(int amount)
    {
        int dmg = Mathf.Abs(amount);
        if (dmg <= 0 || _dead) return;

        CurrentHp = Mathf.Max(0, CurrentHp - dmg);
        Debug.Log($"[HP] Player: {CurrentHp}/{maxHp}");
        
        Enemy.KillAll();

        if (CurrentHp <= 0 && !_dead)
        {
            _dead = true;
            Debug.LogWarning("[HP] Player Dead");
            OnDied?.Invoke();     // ☆ 通知UI
        }
    }
}