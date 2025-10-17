using System.Collections.Generic;
using UnityEngine;
using Game.Skills;

[CreateAssetMenu(menuName = "Game/Skills/Active/Explosion")]
public sealed class ExplosionSkill : ActiveSkillBase
{
    [Header("Explosion")]
    [SerializeField] private float baseRadius = 2.8f;
    [SerializeField] private float radiusPerArea = 0.6f;
    [SerializeField] private float scatterRadius = 6f;

    [Header("Debug Ring")]
    [SerializeField] private bool  debugShowRing = true;     // ← 开关
    [SerializeField] private float ringLifetime  = 0.25f;
    [SerializeField] private Material ringMaterial;          // 可空，默认 Sprites/Default
    [SerializeField] private Color   ringColor = new(1f, 0.6f, 0f, 0.7f);
    [SerializeField, Range(12,128)] private int ringSegments = 48;
    [SerializeField] private float   ringWidth = 0.06f;

    public override void Cast(SkillCastContext ctx, PlayerSkills.SkillStats stats)
    {
        if (ctx?.Player == null) return;

        int   count  = Mathf.Max(1, stats.count);
        float radius = Mathf.Max(0.1f, baseRadius + Mathf.Max(0f, (stats.area - 1f)) * Mathf.Max(0f, radiusPerArea));

        for (int i = 0; i < count; i++)
        {
            Vector2 center = (Vector2)ctx.Player.position + Random.insideUnitCircle * Mathf.Max(0f, scatterRadius);

            KillInCircle_Safe(center, radius);

            if (debugShowRing)
                DrawRing(center, radius, Mathf.Max(0.05f, ringLifetime));
        }
    }

    /// <summary>
    /// 先快照再击杀，避免在遍历 HashSet 时修改集合导致 InvalidOperationException。
    /// </summary>
    private static void KillInCircle_Safe(Vector2 center, float radius)
    {
        float r2 = radius * radius;
        var victims = new List<Enemy>(64);

        // 先收集
        foreach (var e in Enemy.All)
        {
            if (!e) continue;
            Vector2 p = e.transform.position;
            if ((p - center).sqrMagnitude <= r2)
                victims.Add(e);
        }

        // 再逐个 Kill（会触发集合变动也没问题）
        for (int i = 0; i < victims.Count; i++)
        {
            if (victims[i]) victims[i].Kill();
        }
    }

    private void DrawRing(Vector2 center, float radius, float life)
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
            lr.SetPosition(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }

        Object.Destroy(go, life);
    }
}
