using UnityEngine;

namespace Game.Skills
{
    /// <summary>Base type for active skills.</summary>
    public abstract class ActiveSkillBase : ScriptableObject
    {
        /// <summary>Cast the skill.</summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="stats">Aggregated passive stats.</param>
        public abstract void Cast(SkillCastContext ctx, PlayerSkills.SkillStats stats);
    }

    /// <summary>Runtime context passed into skills.</summary>
    public sealed class SkillCastContext
    {
        public Transform Player;
        public MonoBehaviour Runner;
    }
}