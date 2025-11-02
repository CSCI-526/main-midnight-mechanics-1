using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Keyboard (Orbit Flight)")]
public sealed class KeyboardSkill : ActiveSkillBase
{
    [SerializeField] private KeyboardProjectile projectilePrefab;
    [SerializeField] private int   damage = 3;
    [SerializeField] private float speed  = 9f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float orbitRadius = 0.8f;
    [SerializeField] private float orbitRevsPerSec = 2f;
    [SerializeField] private float spawnOffset = 0.1f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;
        Vector2 origin = ctx.Player.position;
        Vector2 dir = SkillUtil.AimDirOrRandomUp(origin);
        Vector2 start = origin + dir * spawnOffset;

        var p = Object.Instantiate(projectilePrefab);
        p.Launch(start, dir, Mathf.Max(1, damage), Mathf.Max(0.1f, speed),
            Mathf.Max(0.05f, lifetime), Mathf.Max(0.05f, orbitRadius), Mathf.Max(0.01f, orbitRevsPerSec));
    }
}