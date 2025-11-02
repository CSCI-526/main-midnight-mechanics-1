using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Synth (Wavy)")]
public sealed class SynthSkill : ActiveSkillBase
{
    [SerializeField] private SynthProjectile projectilePrefab;
    [SerializeField] private int   damage = 3;
    [SerializeField] private float speed  = 8f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float swayAmplitude = 0.6f; // 左右摆动幅度
    [SerializeField] private float swayFrequency = 2.5f; // Hz
    [SerializeField] private float spawnOffset = 0.1f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;
        Vector2 origin = ctx.Player.position;
        Vector2 dir    = SkillUtil.AimDirOrRandomUp(origin);
        Vector2 start  = origin + dir * Mathf.Max(0f, spawnOffset);

        var p = Object.Instantiate(projectilePrefab);
        p.Launch(start, dir, Mathf.Max(1, damage), Mathf.Max(0.1f, speed),
            Mathf.Max(0.05f, lifetime), Mathf.Max(0f, swayAmplitude), Mathf.Max(0.01f, swayFrequency));
    }
}