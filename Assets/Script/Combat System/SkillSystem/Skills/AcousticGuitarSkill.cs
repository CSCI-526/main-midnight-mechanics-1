using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Acoustic Guitar")]
public sealed class AcousticGuitarSkill : ActiveSkillBase
{
    [Header("Chord Slash (Arc)")]
    [SerializeField] private AcousticGuitarProjectile projectilePrefab;
    [SerializeField] private float radius     = 2.6f;
    [SerializeField] private float arcDegrees = 100f;
    [SerializeField] private float duration   = 0.22f;
    [SerializeField] private int   damage     = 6;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 origin = ctx.Player.position;
        Vector2 dir    = SkillUtil.AimDirOrRandomUp(origin);

        var p = Object.Instantiate(projectilePrefab);
        p.Sweep(origin, dir, Mathf.Max(0.5f, radius), Mathf.Max(10f, arcDegrees),
            Mathf.Max(0.05f, duration), Mathf.Max(1, damage));
    }
}