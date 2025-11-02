using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Acoustic Guitar (Split)")]
public sealed class AcousticGuitarSkill : ActiveSkillBase
{
    [SerializeField] private AcousticGuitarProjectile projectilePrefab;
    [SerializeField] private int   damageMain   = 4;
    [SerializeField] private float speedMain    = 12f;
    [SerializeField] private float lifeMain     = 4f;

    [Header("Split")]
    [SerializeField] private int   shardCount   = 4;
    [SerializeField] private int   shardDamage  = 2;
    [SerializeField] private float shardSpeed   = 13f;
    [SerializeField] private float shardLife    = 2.5f;
    [SerializeField] private float spawnOffset  = 0.12f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;
        Vector2 origin = ctx.Player.position;
        Vector2 dir = SkillUtil.AimDirOrRandomUp(origin);
        Vector2 start = origin + dir * spawnOffset;

        var p = Object.Instantiate(projectilePrefab);
        p.Fire(start, dir, damageMain, speedMain, lifeMain, false, shardCount, shardDamage, shardSpeed, shardLife);
    }
}