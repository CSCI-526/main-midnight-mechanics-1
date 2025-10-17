using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Skill Library")]
public class SkillLibrary : ScriptableObject
{
    public enum SkillTier { Tier1 = 1, Tier2 = 2, Tier3 = 3, Tier4 = 4 }

    // 旧枚举仍保留以兼容现有代码
    public enum ActiveSkillId { ChainBolt = 0, Explosion = 1, OrbitOrb = 2, SpreadShot = 3 }
    public enum PassiveSkillId { DamageUp = 0, AreaUp = 1, CountUp = 2, SpeedUp = 3 }

    [System.Serializable]
    public class ActiveEntry
    {
        [Tooltip("手填数值 ID，例如 100/101/102 等")]
        public int code;

        [HideInInspector] public ActiveSkillId id;   // 旧字段：隐藏下拉
        public string displayName;
        public SkillTier tier = SkillTier.Tier1;
        public Sprite icon;
        public Game.Skills.ActiveSkillBase implementation;
    }

    [System.Serializable]
    public class PassiveEntry
    {
        [Tooltip("手填数值 ID，例如 200/201/202 等")]
        public int code;

        [HideInInspector] public PassiveSkillId id;  // 旧字段：隐藏下拉
        public string displayName;
        public SkillTier tier = SkillTier.Tier1;
        public Sprite icon;
    }

    [Header("Actives")]
    public ActiveEntry[] actives;

    [Header("Passives")]
    public PassiveEntry[] passives;

    // —— 旧接口（按枚举）——
    public ActiveEntry GetActive(ActiveSkillId id)
    {
        if (actives == null) return null;
        for (int i = 0; i < actives.Length; i++)
            if (actives[i] != null && actives[i].id.Equals(id))
                return actives[i];
        return null;
    }

    public PassiveEntry GetPassive(PassiveSkillId id)
    {
        if (passives == null) return null;
        for (int i = 0; i < passives.Length; i++)
            if (passives[i] != null && passives[i].id.Equals(id))
                return passives[i];
        return null;
    }

    public Game.Skills.ActiveSkillBase GetActiveImpl(ActiveSkillId id)
    {
        var e = GetActive(id);
        return e != null ? e.implementation : null;
    }

    public bool TryGetActiveIdByImpl(Game.Skills.ActiveSkillBase impl, out ActiveSkillId id)
    {
        if (impl != null && actives != null)
        {
            for (int i = 0; i < actives.Length; i++)
            {
                var e = actives[i];
                if (e != null && e.implementation == impl)
                {
                    id = e.id;
                    return true;
                }
            }
        }
        id = default;
        return false;
    }

    // —— 新增：按“手填数字 code”查询（供以后商店/存档使用）——
    public ActiveEntry GetActiveByCode(int code)
    {
        if (actives == null) return null;
        for (int i = 0; i < actives.Length; i++)
            if (actives[i] != null && actives[i].code == code)
                return actives[i];
        return null;
    }

    public PassiveEntry GetPassiveByCode(int code)
    {
        if (passives == null) return null;
        for (int i = 0; i < passives.Length; i++)
            if (passives[i] != null && passives[i].code == code)
                return passives[i];
        return null;
    }

    public bool TryGetActiveIdByCode(int code, out ActiveSkillId id)
    {
        var e = GetActiveByCode(code);
        if (e != null) { id = e.id; return true; }
        id = default; return false;
    }

    public bool TryGetPassiveIdByCode(int code, out PassiveSkillId id)
    {
        var e = GetPassiveByCode(code);
        if (e != null) { id = e.id; return true; }
        id = default; return false;
    }
}
