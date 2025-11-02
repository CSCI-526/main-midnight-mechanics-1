using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Electric Guitar")]
public sealed class ElectricGuitarSkill : ActiveSkillBase
{
    [Header("Electric Chain")]
    [SerializeField] private ElectricGuitarProjectile projectilePrefab;
    [SerializeField] private int   damage       = 4;
    [SerializeField] private int   bounces      = 2;
    [SerializeField] private float searchRadius = 6f;
    [SerializeField] private float speed        = 14f;
    [SerializeField] private float lifetime     = 5f;
    [SerializeField] private float spawnOffset  = 0.2f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 origin = ctx.Player.position;
        Vector2 dir    = SkillUtil.AimDirOrRandomUp(origin);
        Vector2 start  = origin + dir * Mathf.Max(0f, spawnOffset);

        var p = Object.Instantiate(projectilePrefab);
        p.Initialize(start, dir, speed, bounces, searchRadius, lifetime, Mathf.Max(1, damage));
    }
}