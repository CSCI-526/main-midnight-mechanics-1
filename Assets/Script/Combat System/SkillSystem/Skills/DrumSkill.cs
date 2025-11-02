using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Drum")]
public sealed class DrumSkill : ActiveSkillBase
{
    [Header("Shock Ring")]
    [SerializeField] private DrumProjectile projectilePrefab;
    [SerializeField] private int   damage       = 6;
    [SerializeField] private float startRadius  = 0.25f;
    [SerializeField] private float endRadius    = 3.0f;
    [SerializeField] private float duration     = 0.22f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player || !projectilePrefab) return;

        Vector2 center;
        var nearest = SkillUtil.FindNearestEnemy(ctx.Player.position);
        center = nearest ? (Vector2)nearest.transform.position : (Vector2)ctx.Player.position;

        var p = Object.Instantiate(projectilePrefab);
        p.Configure(center, Mathf.Max(0.01f, startRadius), Mathf.Max(startRadius, endRadius),
            Mathf.Max(0.05f, duration), Mathf.Max(1, damage));
    }
}