using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHp = 5;
    public int MaxHp => maxHp;
    public int CurrentHp { get; private set; }

    void Awake() => CurrentHp = maxHp;
    
    public void TakeDamage(int amount)
    {
        int dmg = Mathf.Abs(amount);
        if (dmg <= 0) return;

        CurrentHp = Mathf.Max(0, CurrentHp - dmg);
        Debug.Log($"[HP] Player: {CurrentHp}/{maxHp}");
        
        Enemy.KillAll();

        if (CurrentHp <= 0)
        {
            Debug.LogWarning("[HP] Player Dead");
            // TODO: Game Over 流程
        }
    }
}