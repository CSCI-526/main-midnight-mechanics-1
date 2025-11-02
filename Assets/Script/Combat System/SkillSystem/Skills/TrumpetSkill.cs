using UnityEngine;
using System.Collections;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Trumpet (Burst x3)")]
public sealed class TrumpetSkill : ActiveSkillBase
{
    [SerializeField] private TrumpetProjectile projectilePrefab;
    [SerializeField] private int   damage   = 3;
    [SerializeField] private float speed    = 16f;
    [SerializeField] private float interval = 0.07f; // 三连发间隔
    [SerializeField] private float spreadDeg = 6f;   // 轻微散布
    [SerializeField] private float spawnOffset = 0.12f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab || ctx.Runner == null) return;
        ctx.Runner.StartCoroutine(FireBurst(ctx));
    }

    IEnumerator FireBurst(SkillCastContext ctx)
    {
        Vector2 origin = ctx.Player.position;
        Vector2 dir0   = SkillUtil.AimDirOrRandomUp(origin);
        for (int i = 0; i < 3; i++)
        {
            float off = (i - 1) * spreadDeg;
            Vector2 dir  = SkillUtil.Rotate(dir0, off);
            Vector2 start= origin + dir * spawnOffset;

            var p = Object.Instantiate(projectilePrefab);
            p.Fire(start, dir, Mathf.Max(1, damage), Mathf.Max(0.1f, speed));
            if (i < 2) yield return new WaitForSeconds(interval);
        }
    }
}