using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class TrumpetProjectile : ProjectileBase
{
    [SerializeField] private bool  useTrigger = true;
    [SerializeField] private float lifeTime   = 3.5f;
    [SerializeField] private float minDirLen  = 1e-6f;

    Rigidbody2D _rb;
    int _damage; float _speed; Vector2 _dir;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) { _rb.gravityScale = 0f; _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; }
        var col = GetComponent<Collider2D>(); if (col) col.isTrigger = useTrigger;
    }

    public void Fire(Vector2 start, Vector2 dir, int damage, float speed)
    {
        transform.position = start;
        _dir = dir.sqrMagnitude < minDirLen ? Vector2.right : dir.normalized;
        _damage = damage; _speed = speed;
        if (_rb) _rb.linearVelocity = _dir * _speed;
        Destroy(gameObject, Mathf.Max(0.05f, lifeTime));
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!useTrigger) return;
        var e = c.GetComponentInParent<Enemy>(); if (!e) return;
        var hp = e.GetComponent<EnemyHealth>(); if (hp) hp.TakeDamage(_damage); else e.Kill();
        Destroy(gameObject);
    }
}