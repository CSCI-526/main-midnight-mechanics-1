using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Bass (Spread)")]
public sealed class BassSkill : ActiveSkillBase
{
    [SerializeField] private BassProjectile projectilePrefab;
    [SerializeField] private int   pelletCount  = 5;
    [SerializeField] private int   pelletDamage = 2;
    [SerializeField] private float pelletSpeed  = 16f;
    [SerializeField] private float coneHalfAngleDeg = 22f;
    [SerializeField] private float spawnOffset = 0.15f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;
        Vector2 origin  = ctx.Player.position;
        Vector2 baseDir = SkillUtil.AimDirOrRandomUp(origin);
        int count = Mathf.Max(1, pelletCount);
        float half = Mathf.Max(0f, coneHalfAngleDeg);

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0f : (i/(float)(count-1))*2f-1f; // [-1,1]
            float offset = t * half;
            Vector2 dir  = SkillUtil.Rotate(baseDir, offset).normalized;
            Vector2 start= origin + dir * Mathf.Max(0f, spawnOffset);

            var p = Object.Instantiate(projectilePrefab);
            p.Configure(Mathf.Max(1, pelletDamage), Mathf.Max(0.1f, pelletSpeed));
            p.FireDir(start, dir);
        }
    }
}