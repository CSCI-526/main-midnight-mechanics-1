using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class VocalProjectile : ProjectileBase
{
    [SerializeField] private bool  useTrigger = true;
    [SerializeField] private float minDirLen  = 1e-6f;

    Rigidbody2D _rb;
    Vector2 _dir;
    float _speed, _life;
    int _damage;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) { _rb.gravityScale = 0f; _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; }
        var col = GetComponent<Collider2D>(); if (col) col.isTrigger = useTrigger;
        // 提示：把 Collider2D 做得“更大”（比如 Capsule/Box）以体现“巨大弹体”
    }

    public void Fire(Vector2 start, Vector2 dir, int damage, float speed, float life)
    {
        transform.position = start;
        _dir = dir.sqrMagnitude < minDirLen ? Vector2.right : dir.normalized;
        _damage = damage; _speed = speed; _life = life;
        if (_rb) _rb.linearVelocity = _dir * _speed;
    }

    void Update()
    {
        if ((_life -= Time.deltaTime) <= 0f) { Destroy(gameObject); return; }
        if (_rb) _rb.linearVelocity = _dir * _speed;
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!useTrigger) return;
        var e = c.GetComponentInParent<Enemy>(); if (!e) return;
        var hp = e.GetComponent<EnemyHealth>(); if (hp) hp.TakeDamage(_damage); else e.Kill();
        // 穿透：不销毁
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (useTrigger) return;
        var e = c.collider.GetComponentInParent<Enemy>(); if (!e) return;
        var hp = e.GetComponent<EnemyHealth>(); if (hp) hp.TakeDamage(_damage); else e.Kill();
        // 穿透：不销毁
    }
}