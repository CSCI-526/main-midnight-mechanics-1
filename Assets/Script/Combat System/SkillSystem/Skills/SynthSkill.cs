using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Synth")]
public sealed class SynthSkill : ActiveSkillBase
{
    [Header("Guided Beam")]
    [SerializeField] private SynthProjectile projectilePrefab;
    [SerializeField] private float  duration  = 0.6f;
    [SerializeField] private float  length    = 7f;
    [SerializeField] private float  width     = 1.2f;
    [SerializeField] private int    dps       = 10;
    [SerializeField] private float  tickRate  = 10f;
    [SerializeField] private float  aimLerp   = 12f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        var p = Object.Instantiate(projectilePrefab);
        p.Activate(ctx.Player, Mathf.Max(0.1f, duration),
            Mathf.Max(0.5f, length), Mathf.Max(0.2f, width),
            Mathf.Max(1, dps), Mathf.Max(1f, tickRate), Mathf.Max(1f, aimLerp));
    }
}