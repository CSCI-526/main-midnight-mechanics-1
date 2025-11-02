using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class BassProjectile : ProjectileBase
{
    [SerializeField] private float defaultSpeed = 12f;
    [SerializeField] private float lifeTime     = 5f;
    [SerializeField] private bool  useTrigger   = true;
    [SerializeField] private float minDirLen    = 1e-6f;

    Rigidbody2D _rb;
    float _speed;
    int _damage;
    bool _fired;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) { _rb.gravityScale = 0f; _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; }
        var col = GetComponent<Collider2D>(); if (col) col.isTrigger = useTrigger;
    }

    public void Configure(int damage, float speed) { _damage = Mathf.Max(1, damage); _speed = speed; }

    public void FireDir(Vector2 start, Vector2 dir)
    {
        transform.position = start;
        Vector2 v = dir.sqrMagnitude < minDirLen ? Vector2.right : dir.normalized;
        if (_rb) _rb.linearVelocity = v * (_speed > 0f ? _speed : defaultSpeed);
        _fired = true; Destroy(gameObject, Mathf.Max(0.05f, lifeTime));
    }

    void Update()
    {
        if (!_fired || !_rb) return;
        var v = _rb.linearVelocity; if (v.sqrMagnitude > 0f) _rb.linearVelocity = v.normalized * (_speed > 0f ? _speed : defaultSpeed);
    }

    void OnTriggerEnter2D(Collider2D c) { if (useTrigger) Hit(c); }
    void OnCollisionEnter2D(Collision2D c) { if (!useTrigger) Hit(c.collider); }

    void Hit(Collider2D col)
    {
        var e = col ? col.GetComponentInParent<Enemy>() : null;
        if (!e) return;
        var hp = e.GetComponent<EnemyHealth>(); if (hp) hp.TakeDamage(_damage); else e.Kill();
        Destroy(gameObject);
    }
}