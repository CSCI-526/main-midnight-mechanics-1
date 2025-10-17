using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Basic Shot (Hidden)")]
public sealed class BasicShotSkill : ActiveSkillBase
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float spreadStepDeg = 6f;

    /// <inheritdoc />
    public override void Cast(SkillCastContext ctx, PlayerSkills.SkillStats stats)
    {
        if (!ctx?.Player || !bulletPrefab) return;

        var nearest = SkillUtil.FindNearestEnemy(ctx.Player.position);
        if (!nearest) return;

        Vector2 baseDir = (nearest.transform.position - ctx.Player.position).normalized;
        if (baseDir.sqrMagnitude < 1e-6f) baseDir = Vector2.right;

        int count = Mathf.Max(1, stats.count);
        float speed = (stats.speed > 0f) ? stats.speed : bulletPrefab.DefaultSpeed;

        float half = (count - 1) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float offset = (i - half) * spreadStepDeg;
            Vector2 dir = SkillUtil.Rotate(baseDir, offset);

            var b = Object.Instantiate(bulletPrefab);
            b.Configure(speed, stats.damage);
            b.FireDir(ctx.Player.position, dir);
        }
    }
}