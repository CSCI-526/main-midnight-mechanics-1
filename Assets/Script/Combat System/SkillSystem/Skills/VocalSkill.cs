using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Vocal")]
public sealed class VocalSkill : ActiveSkillBase
{
    [Header("Shout Wave")]
    [SerializeField] private VocalProjectile projectilePrefab;
    [SerializeField] private float  maxLength   = 6f;
    [SerializeField] private float  width       = 2.2f;
    [SerializeField] private float  growTime    = 0.18f;
    [SerializeField] private int    damage      = 4;
    [SerializeField] private float  knockback   = 4f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 origin  = ctx.Player.position;
        Vector2 dir     = SkillUtil.AimDirOrRandomUp(origin);

        var p = Object.Instantiate(projectilePrefab);
        p.Launch(origin, dir, Mathf.Max(0.5f, maxLength), Mathf.Max(0.2f, width),
            Mathf.Max(0.05f, growTime), Mathf.Max(1, damage), Mathf.Max(0f, knockback));
    }
}