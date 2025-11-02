using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class KeyboardProjectile : ProjectileBase
{
    [SerializeField] private bool useTrigger = true;

    Rigidbody2D _rb;
    Vector2 _forward, _startPos;
    float _speed, _life, _radius, _rev, _t;
    int _damage;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb)
        {
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = useTrigger;
    }

    public void Launch(Vector2 start, Vector2 dir, int damage, float speed, float life, float orbitRadius, float revsPerSec)
    {
        _startPos = start;
        transform.position = start;

        _forward = dir.sqrMagnitude < 1e-6f ? Vector2.up : dir.normalized;
        _damage  = damage;
        _speed   = Mathf.Max(0.1f, speed);
        _life    = Mathf.Max(0.05f, life);
        _radius  = Mathf.Max(0.01f, orbitRadius);
        _rev     = Mathf.Max(0.01f, revsPerSec);
        _t = 0f;
    }

    void Update()
    {
        if ((_life -= Time.deltaTime) <= 0f) { Destroy(gameObject); return; }
        _t += Time.deltaTime;

        // 仅前进的中心（不带上一次的环绕偏移）
        Vector2 center = _startPos + _forward * (_speed * _t);

        float angle = _t * _rev * 2f * Mathf.PI;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _radius;
        transform.position = center + offset;
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!useTrigger) return;
        var e = c.GetComponentInParent<Enemy>();
        if (!e) return;
        var hp = e.GetComponent<EnemyHealth>();
        if (hp) hp.TakeDamage(_damage); else e.Kill();
        Destroy(gameObject);
    }
}