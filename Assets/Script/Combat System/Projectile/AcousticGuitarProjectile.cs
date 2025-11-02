using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class AcousticGuitarProjectile : ProjectileBase
{
    [Header("Collision")]
    [SerializeField] private bool  useTrigger = true;
    [SerializeField] private float minDirLen  = 1e-6f;

    [Header("Split")]
    [SerializeField, Tooltip("碎弹出生时从命中点向外偏移的距离")]
    private float shardSpawnRadius = 0.18f;
    [SerializeField, Tooltip("碎弹生成后忽略命中检测的时间")]
    private float shardSpawnImmuneSeconds = 0.03f;

    Rigidbody2D _rb;
    Vector2 _dir;
    float _speed, _life, _immuneUntil;
    int _damage;
    bool _isShard;

    // split params
    int _shardCount; int _shardDamage; float _shardSpeed; float _shardLife;

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

    public void Fire(
        Vector2 start, Vector2 dir,
        int damage, float speed, float life,
        bool isShard,
        int shardCount, int shardDamage, float shardSpeed, float shardLife)
    {
        transform.position = start;
        _dir = dir.sqrMagnitude < minDirLen ? Vector2.right : dir.normalized;
        _damage = Mathf.Max(1, damage);
        _speed  = Mathf.Max(0.1f, speed);
        _life   = Mathf.Max(0.05f, life);
        _isShard = isShard;

        _shardCount  = Mathf.Max(0, shardCount);
        _shardDamage = Mathf.Max(1, shardDamage);
        _shardSpeed  = Mathf.Max(0.1f, shardSpeed);
        _shardLife   = Mathf.Max(0.05f, shardLife);

        // 碎弹给一个短暂无敌，避免出生重叠瞬间触发
        _immuneUntil = Time.unscaledTime + (_isShard ? shardSpawnImmuneSeconds : 0f);

        if (_rb) _rb.linearVelocity = _dir * _speed;
        Destroy(gameObject, _life);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!useTrigger) return;
        if (Time.unscaledTime < _immuneUntil) return;

        var e = c.GetComponentInParent<Enemy>();
        if (!e) return;

        var hp = e.GetComponent<EnemyHealth>();
        if (hp) hp.TakeDamage(_damage); else e.Kill();

        // 只在“主弹”命中时分裂
        if (!_isShard && _shardCount > 0)
        {
            SpawnShardsAt(transform.position);
        }
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (useTrigger) return;
        if (Time.unscaledTime < _immuneUntil) return;

        var e = col.collider.GetComponentInParent<Enemy>();
        if (!e) return;

        var hp = e.GetComponent<EnemyHealth>();
        if (hp) hp.TakeDamage(_damage); else e.Kill();

        if (!_isShard && _shardCount > 0)
        {
            SpawnShardsAt(transform.position);
        }
        Destroy(gameObject);
    }

    void SpawnShardsAt(Vector2 hitPoint)
    {
        float step = 360f / _shardCount;
        for (int i = 0; i < _shardCount; i++)
        {
            float ang = i * step;
            Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
            Vector2 spawn = hitPoint + dir * shardSpawnRadius;

            // 用同一 prefab 克隆（克隆整个 GameObject 更稳）
            var shardGO = Instantiate(gameObject);
            var shard   = shardGO.GetComponent<AcousticGuitarProjectile>();
            shard.Fire(spawn, dir, _shardDamage, _shardSpeed, _shardLife,
                       isShard: true, shardCount: 0, shardDamage: 0, shardSpeed: 0f, shardLife: 0f);
        }
    }
}
