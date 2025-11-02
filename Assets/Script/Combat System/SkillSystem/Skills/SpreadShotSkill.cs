using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Spread Shot (Simple)")]
public sealed class SpreadShotSkill : ActiveSkillBase
{
    [Header("Pellets")]
    [SerializeField] private int   pelletCount  = 5;
    [SerializeField] private int   pelletDamage = 2;
    [SerializeField] private float pelletSpeed  = 16f;

    [Header("Cone")]
    [SerializeField, Tooltip("半角（度）")] private float coneHalfAngleDeg = 22f;
    [SerializeField] private bool evenDistribution = true;

    [Header("Projectile")]
    [SerializeField] private SpreadPelletProjectile projectilePrefab;
    [SerializeField] private float spawnOffset = 0.15f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 origin  = ctx.Player.position;
        Vector2 baseDir = SkillUtil.AimDirOrRandomUp(origin);              // ★ 关键改动

        int   count = Mathf.Max(1, pelletCount);
        float half  = Mathf.Max(0f, coneHalfAngleDeg);

        for (int i = 0; i < count; i++)
        {
            float offsetDeg;
            if (evenDistribution && count > 1)
            {
                float t = (i / (float)(count - 1)) * 2f - 1f;  // [-1, +1]
                offsetDeg = t * half;
            }
            else
            {
                offsetDeg = Random.Range(-half, +half);
            }

            Vector2 dir   = SkillUtil.Rotate(baseDir, offsetDeg).normalized;
            Vector2 start = origin + dir * Mathf.Max(0f, spawnOffset);

            var p = Object.Instantiate(projectilePrefab);
            p.Configure(pelletSpeed, pelletDamage);
            p.FireDir(start, dir);
        }
    }
}