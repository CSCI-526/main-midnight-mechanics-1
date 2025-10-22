using System;
using System.Collections.Generic;
using UnityEngine;
using static SkillLibrary;

public class PlayerSkills : MonoBehaviour
{
    public const int MAX_LEVEL = 5;

    [Header("Active Slots")]
    [SerializeField] private int maxActive = 4;

    [Header("Base Stats (affect all attacks incl. basic)")]
    [SerializeField] private int   baseDamage = 1;
    [SerializeField] private float baseArea   = 1f;
    [SerializeField] private int   baseCount  = 1;
    [SerializeField] private float baseSpeed  = 12f;

    // Actives are ordered for HUD.
    private readonly List<ActiveSkillId>  _activeEq  = new();
    // Passives are recorded for bookkeeping; no slot limit nor HUD usage.
    private readonly List<PassiveSkillId> _passiveEq = new();

    private readonly Dictionary<ActiveSkillId,  int> _activeLv  = new();
    private readonly Dictionary<PassiveSkillId, int> _passiveLv = new();

    public event Action OnChanged;

    public IReadOnlyList<ActiveSkillId>  Actives  => _activeEq;
    public IReadOnlyList<PassiveSkillId> Passives => _passiveEq;

    public int GetLevel(ActiveSkillId id)  => _activeLv.TryGetValue(id, out var lv) ? lv : 0;
    public int GetLevel(PassiveSkillId id) => _passiveLv.TryGetValue(id, out var lv) ? lv : 0;

    public bool IsFullActive => _activeEq.Count >= maxActive;

    private void Awake()
    {
        Notify();
    }

    /// <summary>Add or level-up an active skill. Respects active slots and MAX_LEVEL.</summary>
    public bool TryAddOrLevelUp(ActiveSkillId id)
    {
        if (_activeLv.TryGetValue(id, out var lv))
        {
            if (lv >= MAX_LEVEL) return false;
            _activeLv[id] = lv + 1;
            Notify();
            return true;
        }

        if (IsFullActive) return false;
        _activeEq.Add(id);
        _activeLv[id] = 1;
        Notify();
        return true;
    }

    /// <summary>Add or level-up a passive skill. No slot limit; only MAX_LEVEL applies.</summary>
    public bool TryAddOrLevelUp(PassiveSkillId id)
    {
        if (_passiveLv.TryGetValue(id, out var lv))
        {
            if (lv >= MAX_LEVEL) return false;
            _passiveLv[id] = lv + 1;
            Notify();
            return true;
        }

        _passiveEq.Add(id);   // kept for bookkeeping/testing; not shown in HUD
        _passiveLv[id] = 1;
        Notify();
        return true;
    }

    /// <summary>Clear all equipped skills and levels.</summary>
    public void ResetAll(bool keepNothing = true)
    {
        _activeEq.Clear();
        _passiveEq.Clear();
        _activeLv.Clear();
        _passiveLv.Clear();
        Notify();
    }

    private void Notify() => OnChanged?.Invoke();

    [Serializable]
    public struct SkillStats
    {
        public int   damage;
        public float area;
        public int   count;
        public float speed;
    }

    /// <summary>Aggregate current passive levels into runtime stats.</summary>
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
