using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Trumpet")]
public sealed class TrumpetSkill : ActiveSkillBase
{
    [Header("Homing Blast")]
    [SerializeField] private TrumpetProjectile projectilePrefab;
    [SerializeField] private float speed     = 18f;
    [SerializeField] private int   damage    = 12;
    [SerializeField] private float steer     = 6f;
    [SerializeField] private float lifeTime  = 3.5f;
    [SerializeField] private float spawnOffset = 0.15f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 origin = ctx.Player.position;
        var target = SkillUtil.FindNearestEnemy(origin);
        Vector2 dir = target ? ((Vector2)target.transform.position - origin).normalized
            : SkillUtil.AimDirOrRandomUp(origin);
        Vector2 start = origin + dir * Mathf.Max(0f, spawnOffset);

        var p = Object.Instantiate(projectilePrefab);
        p.Launch(start, dir, target, Mathf.Max(0.1f, speed), Mathf.Max(0.1f, steer),
            Mathf.Max(0.1f, lifeTime), Mathf.Max(1, damage));
    }
}