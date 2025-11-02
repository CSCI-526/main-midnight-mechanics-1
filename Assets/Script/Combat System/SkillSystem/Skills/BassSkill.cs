using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Bass")]
public sealed class BassSkill : ActiveSkillBase
{
    [Header("Pellets")]
    [SerializeField] private int   pelletCount  = 5;
    [SerializeField] private int   pelletDamage = 2;
    [SerializeField] private float pelletSpeed  = 16f;

    [Header("Cone")]
    [SerializeField, Tooltip("半角（度）")] private float coneHalfAngleDeg = 22f;
    [SerializeField] private bool evenDistribution = true;

    [Header("Projectile")]
    [SerializeField] private BassProjectile projectilePrefab;
    [SerializeField] private float spawnOffset = 0.15f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 origin  = ctx.Player.position;
        Vector2 baseDir = SkillUtil.AimDirOrRandomUp(origin);

        int   count = Mathf.Max(1, pelletCount);
        float half  = Mathf.Max(0f, coneHalfAngleDeg);

        for (int i = 0; i < count; i++)
        {
            float offsetDeg = evenDistribution && count > 1
                ? ((i / (float)(count - 1)) * 2f - 1f) * half
                : Random.Range(-half, +half);

            Vector2 dir   = SkillUtil.Rotate(baseDir, offsetDeg).normalized;
            Vector2 start = origin + dir * Mathf.Max(0f, spawnOffset);

            var p = Object.Instantiate(projectilePrefab);
            p.Configure(pelletSpeed, Mathf.Max(1, pelletDamage));
            p.FireDir(start, dir);
        }
    }
}