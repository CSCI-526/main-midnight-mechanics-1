using UnityEngine;
using Game.Skills;

/// <summary>
/// Active skill: spawns orbiting projectiles around the player.
/// Count scales with passives; radius scales with Area; angular speed scales with Speed.
/// </summary>
[CreateAssetMenu(menuName = "Game/Skills/Active/Orbit Orb")]
public sealed class OrbitOrbSkill : ActiveSkillBase
{
    [Header("Prefab")]
    [SerializeField] private OrbitOrbProjectile projectilePrefab;

    [Header("Tuning")]
    [SerializeField] private float baseRadius = 2.6f;
    [SerializeField] private float radiusPerArea = 0.5f;
    [SerializeField] private float baseAngularSpeedDeg = 140f;
    [SerializeField] private float angularSpeedPerSpeed = 20f; // per (speed-1)
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private bool evenPhase = true;

    /// <inheritdoc />
    public override void Cast(SkillCastContext ctx, PlayerSkills.SkillStats stats)
    {
        if (ctx?.Player == null || projectilePrefab == null) return;

        int count = Mathf.Max(1, stats.count);

        // Radius scales with AreaUp: base + (area-1) * radiusPerArea
        float radius = Mathf.Max(0.1f,
            baseRadius + Mathf.Max(0f, (stats.area - 1f)) * Mathf.Max(0f, radiusPerArea));

        // Angular speed scales with SpeedUp: base + (speed-1) * angularSpeedPerSpeed
        float ang = Mathf.Max(0f,
            baseAngularSpeedDeg + (stats.speed - 1f) * angularSpeedPerSpeed);

        float basePhase = Random.Range(0f, 360f);
        float step = (count > 1 && evenPhase) ? 360f / count : 0f;

        for (int i = 0; i < count; i++)
        {
            float phase = basePhase + step * i;
            var proj = Object.Instantiate(projectilePrefab);
            proj.Initialize(ctx.Player, radius, ang, lifeTime, phase);
        }
    }
}