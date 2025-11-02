using System.Collections.Generic;
using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Explosion (Simple)")]
public sealed class ExplosionSkill : ActiveSkillBase
{
    [Header("Explosion")]
    [SerializeField] private int   damage = 6;
    [SerializeField] private float radius = 3.0f;

    [Header("Visual (optional)")]
    [SerializeField] private bool  debugShowRing = true;
    [SerializeField] private float ringLifetime  = 0.25f;
    [SerializeField] private Material ringMaterial;
    [SerializeField] private Color   ringColor = new(1f, 0.6f, 0f, 0.7f);
    [SerializeField, Range(12,128)] private int ringSegments = 48;
    [SerializeField] private float   ringWidth = 0.06f;

    public override void Cast(SkillCastContext ctx)
    {
        if (!ctx?.Player) return;

        // 优先瞄准最近敌人；没有就炸在玩家附近
        Vector2 center;
        var nearest = SkillUtil.FindNearestEnemy(ctx.Player.position);
        if (nearest) center = nearest.transform.position;
        else         center = (Vector2)ctx.Player.position;

        DamageInCircle_Safe(center, Mathf.Max(0.1f, radius), Mathf.Max(1, damage));

        if (debugShowRing) DrawRing(center, Mathf.Max(0.1f, radius), Mathf.Max(0.05f, ringLifetime));
    }

    static void DamageInCircle_Safe(Vector2 center, float r, int dmg)
    {
        float r2 = r * r;
        var victims = new List<Enemy>(32);
        foreach (var e in Enemy.All)
        {
            if (!e) continue;
            if (((Vector2)e.transform.position - center).sqrMagnitude <= r2)
                victims.Add(e);
        }

        for (int i = 0; i < victims.Count; i++)
        {
            var v = victims[i];
            if (!v) continue;
            var hp = v.GetComponent<EnemyHealth>();
            if (hp) hp.TakeDamage(dmg);
            else    v.Kill();
        }
    }

    void DrawRing(Vector2 center, float r, float life)
    {
        var go = new GameObject("ExplosionRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = Mathf.Max(12, ringSegments);
        lr.startWidth = lr.endWidth = Mathf.Max(0.001f, ringWidth);
        lr.material = ringMaterial != null ? ringMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = ringColor;

        float step = 2f * Mathf.PI / lr.positionCount;
        for (int i = 0; i < lr.positionCount; i++)
        {
            float a = i * step;
            lr.SetPosition(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
        }
        Object.Destroy(go, life);
    }
}
