using UnityEngine;
using System.Collections.Generic;

public sealed class DrumProjectile : ProjectileBase
{
    [Header("Optional Ring")]
    [SerializeField] private Material ringMaterial;
    [SerializeField] private Color ringColor = new(1f, 0.6f, 0f, 0.7f);
    [SerializeField, Range(12,128)] private int ringSegments = 48;
    [SerializeField] private float ringWidth = 0.06f;

    public void Explode(Vector2 center, float radius, int damage, float vfxLife)
    {
        // Damage
        var victims = new List<Enemy>(32);
        float r2 = radius * radius;
        foreach (var e in Enemy.All) if (e && ((Vector2)e.transform.position - center).sqrMagnitude <= r2) victims.Add(e);
        foreach (var v in victims) { var hp = v.GetComponent<EnemyHealth>(); if (hp) hp.TakeDamage(damage); else v.Kill(); }

        // VFX (optional)
        DrawRing(center, radius, vfxLife);
        Destroy(gameObject, Mathf.Max(0.01f, vfxLife));
    }

    void DrawRing(Vector2 c, float r, float life)
    {
        var go = new GameObject("DrumRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true;
        lr.positionCount = Mathf.Max(12, ringSegments);
        lr.startWidth = lr.endWidth = Mathf.Max(0.001f, ringWidth);
        lr.material = ringMaterial != null ? ringMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = ringColor;
        float step = 2f * Mathf.PI / lr.positionCount;
        for (int i = 0; i < lr.positionCount; i++)
            lr.SetPosition(i, c + new Vector2(Mathf.Cos(i*step), Mathf.Sin(i*step)) * r);
        Object.Destroy(go, Mathf.Max(0.05f, life));
    }
}