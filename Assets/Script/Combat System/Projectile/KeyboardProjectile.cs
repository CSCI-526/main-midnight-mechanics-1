using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public sealed class KeyboardProjectile : ProjectileBase
{
    [SerializeField] private bool useTrigger = true;
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private float minDirLen = 1e-6f;

    Rigidbody2D _rb;
    int _damage;
    float _speed;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) { _rb.gravityScale = 0f; _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; }
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = useTrigger;
    }

    public void Configure(float speed, int damage)
    {
        _speed  = speed;
        _damage = Mathf.Max(1, damage);
    }

    public void FireDir(Vector2 start, Vector2 dir)
    {
        transform.position = start;
        Vector2 v = dir.sqrMagnitude < minDirLen ? Vector2.up : dir.normalized;
        if (_rb) _rb.linearVelocity = v * (_speed > 0f ? _speed : 16f);
        Destroy(gameObject, Mathf.Max(0.1f, lifeTime));
    }

    void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
    void OnCollisionEnter2D(Collision2D c)  { TryHit(c.collider); }

    void TryHit(Collider2D col)
    {
        var e = col ? col.GetComponentInParent<Enemy>() : null;
        if (!e) return;
        var hp = e.GetComponent<EnemyHealth>();
        if (hp) hp.TakeDamage(_damage);
        else    e.Kill();
        Destroy(gameObject);
    }
}