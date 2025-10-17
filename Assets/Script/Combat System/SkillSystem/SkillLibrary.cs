using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Skill Library")]
public class SkillLibrary : ScriptableObject
{
    public enum SkillTier { Tier1 = 1, Tier2 = 2, Tier3 = 3, Tier4 = 4 }

    public enum ActiveSkillId { ChainBolt = 0, Explosion = 1, OrbitOrb = 2, SpreadShot = 3 }
    public enum PassiveSkillId { DamageUp = 0, AreaUp = 1, CountUp = 2, SpeedUp = 3 }

    [System.Serializable]
    public class ActiveEntry
    {
        [Tooltip("手填数值 ID，例如 100/101/102 等")]
        public int code;

        [HideInInspector] public ActiveSkillId id;   // 运行期仍使用，但在编辑器中隐藏
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

        [HideInInspector] public PassiveSkillId id;
        public string displayName;
        public SkillTier tier = SkillTier.Tier1;
        public Sprite icon;
    }

    [Header("Actives (顺序即枚举值：0=ChainBolt,1=Explosion,2=OrbitOrb,3=SpreadShot)")]
    public ActiveEntry[] actives;

    [Header("Passives (顺序即枚举值：0=DamageUp,1=AreaUp,2=CountUp,3=SpeedUp)")]
    public PassiveEntry[] passives;

#if UNITY_EDITOR
    // ☆ 关键：在编辑器里自动把隐藏枚举 id 同步为“数组索引”。
    // 这样你只用保证数组顺序正确，不需要手动改隐藏 id。
    private void OnValidate()
    {
        if (actives != null)
        {
            for (int i = 0; i < actives.Length; i++)
            {
                if (actives[i] != null)
                    actives[i].id = (ActiveSkillId)i;
            }
        }
        if (passives != null)
        {
            for (int i = 0; i < passives.Length; i++)
            {
                if (passives[i] != null)
                    passives[i].id = (PassiveSkillId)i;
            }
        }
    }
#endif

    // —— 按“枚举 id”取条目：优先用索引直取，失败再兜底遍历 —— 
    public ActiveEntry GetActive(ActiveSkillId id)
    {
        int i = (int)id;
        if (actives != null && i >= 0 && i < actives.Length)
        {
            var e = actives[i];
            if (e != null) return e;
        }
        // 兜底：旧资产异常时用遍历
        if (actives != null)
            for (int k = 0; k < actives.Length; k++)
                if (actives[k] != null && actives[k].id.Equals(id))
                    return actives[k];
        return null;
    }

    public PassiveEntry GetPassive(PassiveSkillId id)
    {
        int i = (int)id;
        if (passives != null && i >= 0 && i < passives.Length)
        {
            var e = passives[i];
            if (e != null) return e;
        }
        if (passives != null)
            for (int k = 0; k < passives.Length; k++)
                if (passives[k] != null && passives[k].id.Equals(id))
                    return passives[k];
        return null;
    }

    public Game.Skills.ActiveSkillBase GetActiveImpl(ActiveSkillId id)
    {
        var e = GetActive(id);
        return e != null ? e.implementation : null;
    }

    // —— 由“实现 SO”反查枚举 id：按索引返回，避免依赖隐藏 id —— 
    public bool TryGetActiveIdByImpl(Game.Skills.ActiveSkillBase impl, out ActiveSkillId id)
    {
        if (impl != null && actives != null)
        {
            for (int i = 0; i < actives.Length; i++)
            {
                var e = actives[i];
                if (e != null && e.implementation == impl)
                {
                    id = (ActiveSkillId)i; // ☆ 用索引作为 id
                    return true;
                }
            }
        }
        id = default;
        return false;
    }

    // —— 你保留的“手填数字 code”相关接口 —— 
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
