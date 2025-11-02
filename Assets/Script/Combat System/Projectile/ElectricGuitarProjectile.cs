using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class ElectricGuitarProjectile : ProjectileBase
{
    [Header("Collision")]
    [SerializeField] private bool  useTrigger = true;
    [SerializeField] private float minDirLen  = 1e-6f;

    [Header("Hit Cooldown")]
    [SerializeField] private float hitCooldownAfterImpact = 0.03f;
    [SerializeField] private bool  destroyIfNoNextTarget  = true;

    Rigidbody2D _rb;
    Vector2 _dir;
    float _speed, _lifeLeft, _searchRadius, _hitCDUntil;
    int _hopBudget, _damage;
    readonly HashSet<Enemy> _visited = new();

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) { _rb.gravityScale = 0f; _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; }
        var col = GetComponent<Collider2D>(); if (col) col.isTrigger = useTrigger;
    }

    public void Initialize(Vector2 start, Vector2 dir, float speed, int bounces, float searchRadius, float lifetime, int damage)
    {
        transform.position = start;
        _dir          = dir.sqrMagnitude < minDirLen ? Vector2.right : dir.normalized;
        _speed        = speed; _hopBudget = bounces; _searchRadius = searchRadius;
        _lifeLeft     = lifetime; _damage = damage;
        if (_rb) _rb.linearVelocity = _dir * _speed;
    }

    void Update()
    {
        if ((_lifeLeft -= Time.deltaTime) <= 0f) { Destroy(gameObject); return; }
        if (_rb) _rb.linearVelocity = _dir * _speed;
    }

    void OnTriggerEnter2D(Collider2D c) { if (useTrigger) TryHit(c); }
    void OnCollisionEnter2D(Collision2D c) { if (!useTrigger) TryHit(c.collider); }

    void TryHit(Collider2D col)
    {
        if (Time.unscaledTime < _hitCDUntil) return;
        var enemy = col ? col.GetComponentInParent<Enemy>() : null;
        if (!enemy || _visited.Contains(enemy)) return;

        _visited.Add(enemy);
        var hp = enemy.GetComponent<EnemyHealth>(); if (hp) hp.TakeDamage(_damage); else enemy.Kill();

        if (_hopBudget-- > 0)
        {
            var next = FindNextTarget(enemy.transform.position);
            if (next)
            {
                Vector2 nd = ((Vector2)next.transform.position - (Vector2)transform.position);
                if (nd.sqrMagnitude > minDirLen) _dir = nd.normalized;
                _hitCDUntil = Time.unscaledTime + hitCooldownAfterImpact;
                return;
            }
        }
        if (destroyIfNoNextTarget) Destroy(gameObject);
        else _hitCDUntil = Time.unscaledTime + hitCooldownAfterImpact;
    }

    Enemy FindNextTarget(Vector2 from)
    {
        float bestSqr = _searchRadius * _searchRadius;
        Enemy best = null;
        foreach (var e in Enemy.All)
        {
            if (!e || _visited.Contains(e)) continue;
            float d2 = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (d2 <= bestSqr) { bestSqr = d2; best = e; }
        }
        return best;
    }
}
