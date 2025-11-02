using UnityEngine;

namespace Game.Skills
{
    /// <summary>主动技能基类（已精简：不再传入被动统计）。</summary>
    public abstract class ActiveSkillBase : ScriptableObject
    {
        public abstract void Cast(SkillCastContext ctx);
    }

    /// <summary>技能运行时上下文。</summary>
    public sealed class SkillCastContext
    {
        public Transform Player;        // 玩家位姿
        public MonoBehaviour Runner;    // 调用者（用于 Spawn/协程等）
    }
}