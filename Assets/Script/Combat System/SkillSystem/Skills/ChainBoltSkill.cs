using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Chain Bolt (Simple)")]
public sealed class ChainBoltSkill : ActiveSkillBase
{
    [Header("Damage & Chain")]
    [SerializeField] private int   damage       = 4;   // 每跳伤害
    [SerializeField] private int   bounces      = 2;   // 弹射次数
    [SerializeField] private float searchRadius = 6f;  // 下一目标搜索半径

    [Header("Projectile")]
    [SerializeField] private ChainBoltProjectile projectilePrefab;
    [SerializeField] private float speed    = 14f;
    [SerializeField] private float lifetime = 5f;

    [Header("Aim")]
    [SerializeField] private float spawnOffset = 0.2f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 origin = ctx.Player.position;
        Vector2 dir    = SkillUtil.AimDirOrRandomUp(origin);                // ★ 关键改动
        Vector2 start  = origin + dir * Mathf.Max(0f, spawnOffset);

        var bolt = Object.Instantiate(projectilePrefab);
        bolt.Initialize(start, dir, speed, bounces, searchRadius, lifetime, damage);
    }
}