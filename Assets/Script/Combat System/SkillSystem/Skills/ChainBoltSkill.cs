using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Chain Bolt")]
public sealed class ChainBoltSkill : ActiveSkillBase
{
    [SerializeField] private ChainBoltProjectile projectile;
    [SerializeField] private float chainSearchRadius = 6f;
    [SerializeField] private int   chainBaseHops = 1;
    [SerializeField] private float chainLifeTime = 5f;
    [SerializeField] private float chainSpreadStepDeg = 8f;

    /// <inheritdoc />
    public override void Cast(SkillCastContext ctx, PlayerSkills.SkillStats stats)
    {
        if (!ctx?.Player || !projectile) return;

        var nearest = SkillUtil.FindNearestEnemy(ctx.Player.position);
        if (!nearest) return;

        int count     = Mathf.Max(1, stats.count);
        int hopBudget = Mathf.Max(0, chainBaseHops + (stats.count - 1));
        float speed   = Mathf.Max(0.1f, stats.speed);

        float radius = Mathf.Max(0.1f, chainSearchRadius + Mathf.Max(0f, stats.area - 1f));
        Vector2 baseDir = (nearest.transform.position - ctx.Player.position).normalized;
        if (baseDir.sqrMagnitude < 1e-6f) baseDir = Vector2.right;

        float half = (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float offset = (i - half) * chainSpreadStepDeg;
            Vector2 dir  = SkillUtil.Rotate(baseDir, offset);

            var bolt = Object.Instantiate(projectile);
            bolt.Initialize(ctx.Player.position, dir, speed, hopBudget, radius, chainLifeTime);
        }
    }
}