using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Vocal (Pierce)")]
public sealed class VocalSkill : ActiveSkillBase
{
    [SerializeField] private VocalProjectile projectilePrefab;
    [SerializeField] private int   damage   = 5;
    [SerializeField] private float speed    = 18f;
    [SerializeField] private float lifetime = 2.8f;
    [SerializeField] private float spawnOffset = 0.2f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;
        Vector2 origin = ctx.Player.position;
        Vector2 dir = SkillUtil.AimDirOrRandomUp(origin);
        Vector2 start = origin + dir * spawnOffset;

        var p = Object.Instantiate(projectilePrefab);
        p.Fire(start, dir, Mathf.Max(1, damage), Mathf.Max(0.1f, speed), Mathf.Max(0.05f, lifetime));
    }
}