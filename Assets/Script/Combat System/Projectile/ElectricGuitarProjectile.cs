using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class ElectricGuitarProjectile : ProjectileBase
{
    [Header("Hit")]
    [SerializeField] private float hitCooldownAfterImpact = 0.03f;
    [SerializeField] private bool  destroyIfNoNextTarget = true;

    [Header("Collision")]
    [SerializeField] private bool  useTrigger = true;
    [SerializeField] private float minDirLen = 1e-6f;

    Rigidbody2D _rb;
    float _speed;
    int   _hopBudget;
    float _searchRadius;
    int   _damage;
    float _lifeLeft;
    float _hitCDUntil;
    Vector2 _dir;
    readonly HashSet<Enemy> _visited = new();

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) { _rb.gravityScale = 0f; _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; }
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = useTrigger;
    }

    public void Initialize(Vector2 startPos, Vector2 dir, float speed, int hopBudget, float searchRadius, float lifeTime, int damagePerHit)
    {
        transform.position = startPos;
        _dir          = dir.sqrMagnitude < minDirLen ? Vector2.right : dir.normalized;
        _speed        = Mathf.Max(0.01f, speed);
        _hopBudget    = Mathf.Max(0, hopBudget);
        _searchRadius = Mathf.Max(0.05f, searchRadius);
        _lifeLeft     = Mathf.Max(0.01f, lifeTime);
        _damage       = Mathf.Max(1, damagePerHit);

        if (_rb) _rb.linearVelocity = _dir * _speed;
    }

    void Update()
    {
        if (_lifeLeft <= 0f) { Destroy(gameObject); return; }
        _lifeLeft -= Time.deltaTime;

        if (_rb) _rb.linearVelocity = _dir * _speed;
    }

    void OnTriggerEnter2D(Collider2D other) { if (useTrigger) TryHit(other); }
    void OnCollisionEnter2D(Collision2D c)  { if (!useTrigger) TryHit(c.collider); }

    void TryHit(Collider2D col)
    {
        if (Time.unscaledTime < _hitCDUntil) return;
        if (!col) return;

        var enemy = col.GetComponentInParent<Enemy>();
        if (!enemy || !enemy.gameObject.activeInHierarchy) return;
        if (_visited.Contains(enemy)) return;

        _visited.Add(enemy);
        ApplyDamage(enemy);

        if (_hopBudget > 0)
        {
            _hopBudget--;
            var next = FindNextTarget(enemy.transform.position);
            if (next)
            {
                Vector2 nextDir = ((Vector2)next.transform.position - (Vector2)transform.position);
                _dir = nextDir.sqrMagnitude < minDirLen ? _dir : nextDir.normalized;
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
            if (!e || !e.gameObject.activeInHierarchy) continue;
            if (_visited.Contains(e)) continue;

            float d2 = ((Vector2)e.transform.position - from).sqrMagnitude;
            if (d2 <= bestSqr) { bestSqr = d2; best = e; }
        }
        return best;
    }

    void ApplyDamage(Enemy e)
    {
        var hp = e.GetComponent<EnemyHealth>();
        if (hp) hp.TakeDamage(_damage);
        else    e.Kill();
    }
}
