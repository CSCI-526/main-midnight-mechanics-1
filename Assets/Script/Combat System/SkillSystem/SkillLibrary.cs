using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Skill Library")]
public class SkillLibrary : ScriptableObject
{
    public enum SkillTier { Tier1 = 1, Tier2 = 2, Tier3 = 3 }

    public enum ActiveSkillId { ChainBolt = 0, Explosion = 1, OrbitOrb = 2, SpreadShot = 3 }

    [System.Serializable]
    public class ActiveEntry
    {
        [Tooltip("手填数值 ID，例如 100/101/102 等")]
        public int code;

        [HideInInspector] public ActiveSkillId id;     // 运行期用索引赋值
        public string displayName;
        public SkillTier tier = SkillTier.Tier1;
        public Sprite icon;
        public Game.Skills.ActiveSkillBase implementation;

        [Min(0)] public int price = 30;                // 若你仍保留商店 UI，可继续使用
    }

    [Header("Actives (顺序即枚举值：0=ChainBolt,1=Explosion,2=OrbitOrb,3=SpreadShot)")]
    public ActiveEntry[] actives;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (actives != null)
        {
            for (int i = 0; i < actives.Length; i++)
                if (actives[i] != null) actives[i].id = (ActiveSkillId)i;
        }
    }
#endif

    public ActiveEntry GetActive(ActiveSkillId id)
    {
        int i = (int)id;
        if (actives != null && i >= 0 && i < actives.Length)
        {
            var e = actives[i];
            if (e != null) return e;
        }
        if (actives != null)
        {
            for (int k = 0; k < actives.Length; k++)
                if (actives[k] != null && actives[k].id.Equals(id))
                    return actives[k];
        }
        return null;
    }

    public Game.Skills.ActiveSkillBase GetActiveImpl(ActiveSkillId id)
    {
        var e = GetActive(id);
        return e != null ? e.implementation : null;
    }

    public ActiveEntry GetActiveByCode(int code)
    {
        if (actives == null) return null;
        for (int i = 0; i < actives.Length; i++)
            if (actives[i] != null && actives[i].code == code)
                return actives[i];
        return null;
    }

    public bool TryGetActiveIdByCode(int code, out ActiveSkillId id)
    {
        var e = GetActiveByCode(code);
        if (e != null) { id = e.id; return true; }
        id = default; return false;
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
                    id = (ActiveSkillId)i;
                    return true;
                }
            }
        }
        id = default;
        return false;
    }
}
