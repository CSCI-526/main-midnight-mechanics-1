using System;
using System.Collections.Generic;
using UnityEngine;
using static SkillLibrary;

public class PlayerSkills : MonoBehaviour
{
    public const int MAX_LEVEL = 5;

    [Header("Config")]
    [SerializeField] private int maxActive = 4;
    [SerializeField] private int maxPassive = 4;

    // —— 普攻/通用的基础数值 —— 
    [Header("Base Stats (apply to all attacks incl. basic)")]
    [SerializeField] private int   baseDamage = 1;   // 伤害基准
    [SerializeField] private float baseArea   = 1f;  // 范围系数基准
    [SerializeField] private int   baseCount  = 1;   // 弹道数量基准
    [SerializeField] private float baseSpeed  = 12f; // 弹道速度基准

    // 顺序 = UI 显示顺序
    private readonly List<ActiveSkillId>  _activeEq  = new();
    private readonly List<PassiveSkillId> _passiveEq = new();

    private readonly Dictionary<ActiveSkillId, int>  _activeLv  = new();
    private readonly Dictionary<PassiveSkillId, int> _passiveLv = new();

    public event Action OnChanged;

    void Awake()
    {
        Notify();
    }

    public IReadOnlyList<ActiveSkillId>  Actives  => _activeEq;
    public IReadOnlyList<PassiveSkillId> Passives => _passiveEq;

    public int GetLevel(ActiveSkillId id)  => _activeLv.TryGetValue(id, out var lv) ? lv : 0;
    public int GetLevel(PassiveSkillId id) => _passiveLv.TryGetValue(id, out var lv) ? lv : 0;

    public bool IsFullActive  => _activeEq.Count  >= maxActive;
    public bool IsFullPassive => _passiveEq.Count >= maxPassive;

    public bool TryAddOrLevelUp(ActiveSkillId id)
    {
        if (_activeLv.TryGetValue(id, out var lv))
        {
            if (lv >= MAX_LEVEL) return false;
            _activeLv[id] = lv + 1;
            Notify();
            return true;
        }
        else
        {
            if (IsFullActive) return false;
            _activeEq.Add(id);
            _activeLv[id] = 1;
            Notify();
            return true;
        }
    }

    public bool TryAddOrLevelUp(PassiveSkillId id)
    {
        if (_passiveLv.TryGetValue(id, out var lv))
        {
            if (lv >= MAX_LEVEL) return false;
            _passiveLv[id] = lv + 1;
            Notify();
            return true;
        }
        else
        {
            if (IsFullPassive) return false;
            _passiveEq.Add(id);
            _passiveLv[id] = 1;
            Notify();
            return true;
        }
    }

    public void ResetAll(bool keepNothing = true)
    {
        _activeEq.Clear();
        _passiveEq.Clear();
        _activeLv.Clear();
        _passiveLv.Clear();
        Notify();
    }

    void Notify() => OnChanged?.Invoke();
    
    [Serializable]
    public struct SkillStats
    {
        public int   damage;   // 伤害点数
        public float area;     // 范围系数
        public int   count;    // 弹道数量
        public float speed;    // 弹道速度
    }

    public SkillStats GetCurrentStats()
    {
        int dmgLv   = GetLevel(PassiveSkillId.DamageUp);
        int areaLv  = GetLevel(PassiveSkillId.AreaUp);
        int countLv = GetLevel(PassiveSkillId.CountUp);
        int spdLv   = GetLevel(PassiveSkillId.SpeedUp);
        
        return new SkillStats
        {
            damage = baseDamage + dmgLv,
            area   = baseArea   + areaLv,
            count  = Mathf.Max(1, baseCount + countLv),
            speed  = baseSpeed  + spdLv
        };
    }
}
