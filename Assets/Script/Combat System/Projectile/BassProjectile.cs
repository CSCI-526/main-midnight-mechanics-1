using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class BassProjectile : ProjectileBase
{
    [Header("Defaults")]
    [SerializeField] private float defaultSpeed = 12f;
    [SerializeField] private float lifeTime     = 5f;

    [Header("Collision")]
    [SerializeField] private bool  useTrigger  = true;
    [SerializeField] private float minDirLen   = 1e-6f;

    Rigidbody2D _rb;
    float _speed;
    int   _damage;
    bool  _fired;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb) { _rb.gravityScale = 0f; _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; }
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = useTrigger;
    }

    public void Configure(float speed, int damage)
    {
        _speed  = (speed > 0f) ? speed : defaultSpeed;
        _damage = Mathf.Max(1, damage);
    }

    public void FireDir(Vector2 start, Vector2 dir)
    {
        transform.position = start;
        Vector2 v = dir.sqrMagnitude < minDirLen ? Vector2.right : dir.normalized;
        if (_rb) _rb.linearVelocity = v * (_speed > 0f ? _speed : defaultSpeed);
        _fired = true;
        Destroy(gameObject, Mathf.Max(0.01f, lifeTime));
    }

    void Update()
    {
        if (!_fired || !_rb) return;
        var v = _rb.linearVelocity;
        if (v.sqrMagnitude > 0f)
            _rb.linearVelocity = v.normalized * (_speed > 0f ? _speed : defaultSpeed);
    }

    void OnTriggerEnter2D(Collider2D other) { if (!useTrigger) return; Hit(other); }
    void OnCollisionEnter2D(Collision2D c)  { if (useTrigger) return; Hit(c.collider); }

    void Hit(Collider2D col)
    {
        var enemy = col ? col.GetComponentInParent<Enemy>() : null;
        if (!enemy) return;

        var hp = enemy.GetComponent<EnemyHealth>();
        if (hp) hp.TakeDamage(_damage);
        else    enemy.Kill();

        Destroy(gameObject);
    }
}