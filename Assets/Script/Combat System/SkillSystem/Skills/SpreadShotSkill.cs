using UnityEngine;
using Game.Skills;

/// <summary>
/// Active skill: fires pellets in a cone around a random base direction each cast.
/// Pellets = base + (count-1)*step; SpeedUp/DamageUp applied to projectile.
/// </summary>
[CreateAssetMenu(menuName = "Game/Skills/Active/Spread Shot")]
public sealed class SpreadShotSkill : ActiveSkillBase
{
    [Header("Projectile")]
    [SerializeField] private SpreadPelletProjectile projectilePrefab;

    [Header("Pellet Count")]
    [SerializeField] private int basePellets = 3;
    [SerializeField] private int pelletsPerExtraCount = 1;

    [Header("Cone")]
    [SerializeField, Tooltip("Half-angle of the cone (degrees).")]
    private float coneHalfAngleDeg = 20f;
    [SerializeField, Tooltip("Even spacing across cone if true; otherwise random within cone.")]
    private bool evenDistribution = true;
    [SerializeField, Tooltip("Extra random jitter (deg) per pellet.")]
    private float randomJitterDeg = 2f;

    [Header("Spawn")]
    [SerializeField, Tooltip("Small random position jitter to avoid perfect overlap.")]
    private float spawnJitterRadius = 0f;

    public override void Cast(SkillCastContext ctx, PlayerSkills.SkillStats stats)
    {
        if (ctx?.Player == null || projectilePrefab == null) return;

        int pellets = Mathf.Max(1, basePellets + Mathf.Max(0, (stats.count - 1)) * Mathf.Max(0, pelletsPerExtraCount));
        float speed = (stats.speed > 0f) ? stats.speed : projectilePrefab.DefaultSpeed;
        int   dmg   = stats.damage;

        Vector2 origin = ctx.Player.position;

        // random base direction per cast
        float baseAngleDeg = Random.Range(0f, 360f);
        Vector2 baseDir = new Vector2(Mathf.Cos(baseAngleDeg * Mathf.Deg2Rad), Mathf.Sin(baseAngleDeg * Mathf.Deg2Rad));

        for (int i = 0; i < pellets; i++)
        {
            float offsetDeg;
            if (evenDistribution && pellets > 1)
            {
                float t = (i / (float)(pellets - 1)) * 2f - 1f;  // [-1, +1]
                offsetDeg = t * coneHalfAngleDeg;
            }
            else
            {
                offsetDeg = Random.Range(-coneHalfAngleDeg, +coneHalfAngleDeg);
            }

            if (randomJitterDeg > 0f)
                offsetDeg += Random.Range(-randomJitterDeg, +randomJitterDeg);

            Vector2 dir   = SkillUtil.Rotate(baseDir, offsetDeg).normalized;
            Vector2 start = origin + (spawnJitterRadius > 0f ? Random.insideUnitCircle * spawnJitterRadius : Vector2.zero);

            var p = Object.Instantiate(projectilePrefab);
            p.Configure(speed, dmg);
            p.FireDir(start, dir);
        }
    }
}
