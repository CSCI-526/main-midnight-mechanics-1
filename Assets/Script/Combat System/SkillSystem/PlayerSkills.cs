using System;
using System.Collections.Generic;
using UnityEngine;
using static SkillLibrary;

public class PlayerSkills : MonoBehaviour
{
    [Header("Active Loadout")]
    [SerializeField, Min(1)] private int maxActive = 4;
    [SerializeField] private List<ActiveSkillId> preselected = new(); // 可选：在编辑器预填，Awake 时应用

    [Header("Global Base Stats")]
    [SerializeField] private int   baseDamage = 1;  // 给投射物/范围技当作基础伤害
    [SerializeField] private float baseArea   = 1f; // 影响半径/搜索范围
    [SerializeField] private int   baseCount  = 1;  // 影响同屏生成数量/弹数
    [SerializeField] private float baseSpeed  = 12f;// 影响弹速/角速度等

    private readonly List<ActiveSkillId> _actives = new();

    public event Action OnChanged;
    public IReadOnlyList<ActiveSkillId> Actives => _actives;

    private void Awake()
    {
        _actives.Clear();
        if (preselected != null)
        {
            for (int i = 0; i < preselected.Count && _actives.Count < maxActive; i++)
                TryAdd(preselected[i]);
        }
        Notify();
    }

    public bool TryAdd(ActiveSkillId id)
    {
        if (_actives.Count >= maxActive) return false;
        if (_actives.Contains(id)) return false;
        _actives.Add(id);
        Notify();
        return true;
    }

    public bool Remove(ActiveSkillId id)
    {
        bool ok = _actives.Remove(id);
        if (ok) Notify();
        return ok;
    }

    public void SetLoadout(IEnumerable<ActiveSkillId> ids)
    {
        _actives.Clear();
        if (ids != null)
        {
            foreach (var id in ids)
            {
                if (_actives.Count >= maxActive) break;
                if (!_actives.Contains(id)) _actives.Add(id);
            }
        }
        Notify();
    }

    public void ResetAll()
    {
        _actives.Clear();
        Notify();
    }

    public SkillStats GetCurrentStats() => new SkillStats
    {
        damage = baseDamage,
        area   = baseArea,
        count  = Mathf.Max(1, baseCount),
        speed  = baseSpeed
    };

    [Serializable]
    public struct SkillStats
    {
        public int   damage;
        public float area;
        public int   count;
        public float speed;
    }

    private void Notify() => OnChanged?.Invoke();
}
