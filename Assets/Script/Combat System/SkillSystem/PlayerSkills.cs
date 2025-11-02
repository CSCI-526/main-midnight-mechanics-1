using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Skills; 

public class PlayerSkills : MonoBehaviour
{
    [Header("Active Loadout")]
    [SerializeField, Min(1)] private int maxActive = 4;
    [SerializeField] private List<SkillLibrary.ActiveSkillId> preselected = new();

    private readonly List<SkillLibrary.ActiveSkillId> _actives = new();

    public event Action OnChanged;
    public IReadOnlyList<SkillLibrary.ActiveSkillId> Actives => _actives;

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

    public bool TryAdd(SkillLibrary.ActiveSkillId id)
    {
        if (_actives.Count >= maxActive) return false;
        if (_actives.Contains(id)) return false;
        _actives.Add(id);
        Notify();
        return true;
    }

    public bool Remove(SkillLibrary.ActiveSkillId id)
    {
        bool ok = _actives.Remove(id);
        if (ok) Notify();
        return ok;
    }

    public void SetLoadout(IEnumerable<SkillLibrary.ActiveSkillId> ids)
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

    private void Notify() => OnChanged?.Invoke();
}