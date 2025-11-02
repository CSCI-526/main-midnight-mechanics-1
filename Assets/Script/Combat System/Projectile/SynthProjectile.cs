using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class SynthProjectile : ProjectileBase
{
    [SerializeField] private bool useTrigger = true;

    Rigidbody2D _rb;
    Vector2 _forward, _right, _startPos;
    float _speed, _life, _amp, _freq, _t;
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

    public void Launch(Vector2 start, Vector2 dir, int damage, float speed, float life, float amplitude, float frequency)
    {
        _startPos = start;
        transform.position = start;

        _forward = dir.sqrMagnitude < 1e-6f ? Vector2.up : dir.normalized;
        _right   = new Vector2(_forward.y, -_forward.x); // 正交方向
        _damage  = damage;
        _speed   = Mathf.Max(0.1f, speed);
        _life    = Mathf.Max(0.05f, life);
        _amp     = Mathf.Max(0f, amplitude);
        _freq    = Mathf.Max(0.01f, frequency);
        _t = 0f;
    }

    void Update()
    {
        if ((_life -= Time.deltaTime) <= 0f) { Destroy(gameObject); return; }
        _t += Time.deltaTime;

        // 中心轨迹（不会累加偏移）
        Vector2 center = _startPos + _forward * (_speed * _t);
        float sway = Mathf.Sin(_t * Mathf.PI * 2f * _freq) * _amp;
        transform.position = center + _right * sway;
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