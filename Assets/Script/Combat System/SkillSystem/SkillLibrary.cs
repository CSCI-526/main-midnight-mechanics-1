using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skills/Skill Library")]
public class SkillLibrary : ScriptableObject
{
    public enum SkillTier { Tier1 = 1, Tier2 = 2, Tier3 = 3, Tier4 = 4 }

    public enum ActiveSkillId
    {
        ChainBolt = 0,   // 弹射攻击
        Explosion = 1,   // 爆炸攻击
        OrbitOrb  = 2,   // 围绕攻击
        SpreadShot= 3,   // 散弹攻击
    }

    public enum PassiveSkillId
    {
        DamageUp = 0,    // 伤害+1
        AreaUp   = 1,    // 攻击范围+1
        CountUp  = 2,    // 弹道数量+1
        SpeedUp  = 3,    // 弹道飞行速度+1
    }

    [System.Serializable]
    public class ActiveEntry
    {
        public ActiveSkillId id;
        public string displayName;
        public SkillTier tier = SkillTier.Tier1;
        public Sprite icon;
    }

    [System.Serializable]
    public class PassiveEntry
    {
        public PassiveSkillId id;
        public string displayName;
        public SkillTier tier = SkillTier.Tier1;
        public Sprite icon;
    }

    [Header("Actives (4+)")]
    public ActiveEntry[] actives;

    [Header("Passives (4+)")]
    public PassiveEntry[] passives;

    public ActiveEntry GetActive(ActiveSkillId id)
    {
        foreach (var e in actives) if (e.id == id) return e;
        return null;
    }
    public PassiveEntry GetPassive(PassiveSkillId id)
    {
        foreach (var e in passives) if (e.id == id) return e;
        return null;
    }
}