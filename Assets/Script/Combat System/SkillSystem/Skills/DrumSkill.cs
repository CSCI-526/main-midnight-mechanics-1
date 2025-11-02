using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Drum (Explosion)")]
public sealed class DrumSkill : ActiveSkillBase
{
    [SerializeField] private DrumProjectile projectilePrefab;
    [SerializeField] private int   damage = 6;
    [SerializeField] private float radius = 3f;
    [SerializeField] private float vfxLifetime = 0.25f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;
        Vector2 center;
        var n = SkillUtil.FindNearestEnemy(ctx.Player.position);
        center = n ? (Vector2)n.transform.position : (Vector2)ctx.Player.position;

        var p = Object.Instantiate(projectilePrefab);
        p.Explode(center, Mathf.Max(0.1f, radius), Mathf.Max(1, damage), Mathf.Max(0.05f, vfxLifetime));
    }
}