using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class ChainBoltProjectile : ProjectileBase
{
    [Header("Hit")]
    [SerializeField] private float hitCooldownAfterImpact = 0.03f;   // 防止连帧多次触发
    [SerializeField] private bool  destroyIfNoNextTarget = true;     // 没有下一个目标就结束

    [Header("Collision")]
    [SerializeField] private bool  useTrigger = true;                 // 建议 projectile 的 Collider2D 勾 isTrigger
    [SerializeField] private float minDirLen = 1e-6f;

    // runtime
    private Rigidbody2D _rb;
    private float _speed;
    private int   _hopBudget;
    private float _searchRadius;
    private int   _damage;
    private float _lifeLeft;
    private float _hitCDUntil;
    private Vector2 _dir;
    private readonly HashSet<Enemy> _visited = new();

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        TryHit(other);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (useTrigger) return;
        TryHit(c.collider);
    }

    void TryHit(Collider2D col)
    {
        if (Time.unscaledTime < _hitCDUntil) return;
        if (!col) return;

        var enemy = col.GetComponentInParent<Enemy>();
        if (!enemy || !enemy.gameObject.activeInHierarchy) return;
        if (_visited.Contains(enemy)) return; // 已命中过就跳过

        // 记录+伤害
        _visited.Add(enemy);
        ApplyDamage(enemy);

        // 选择下一个目标
        if (_hopBudget > 0)
        {
            _hopBudget--;
            var next = FindNextTarget(enemy.transform.position);
            if (next)
            {
                Vector2 nextDir = ((Vector2)next.transform.position - (Vector2)transform.position);
                _dir = nextDir.sqrMagnitude < minDirLen ? _dir : nextDir.normalized;
                _hitCDUntil = Time.unscaledTime + hitCooldownAfterImpact; // 短暂无敌，防止在同一帧再次触发
                return; // 继续飞
            }
        }

        // 没下一个目标了
        if (destroyIfNoNextTarget) Destroy(gameObject);
        else _hitCDUntil = Time.unscaledTime + hitCooldownAfterImpact; // 允许继续穿行（可按需关闭）
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
            if (d2 <= bestSqr)
            {
                bestSqr = d2;
                best = e;
            }
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
