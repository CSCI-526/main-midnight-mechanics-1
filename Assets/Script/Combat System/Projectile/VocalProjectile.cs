using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class VocalProjectile : ProjectileBase
{
    BoxCollider2D _box;
    Vector2 _origin;
    Vector2 _dir;
    float _maxLen, _width, _growTime, _t;
    int _damage;
    float _knock;

    readonly HashSet<Enemy> _hit = new();

    void Awake()
    {
        _box = GetComponent<BoxCollider2D>();
        if (_box) { _box.isTrigger = true; _box.size = new Vector2(0.1f, 0.1f); }
    }

    public void Launch(Vector2 origin, Vector2 dir, float maxLen, float width, float growTime, int damage, float knockback)
    {
        _origin = origin; _dir = dir.sqrMagnitude < 1e-6f ? Vector2.up : dir.normalized;
        _maxLen = maxLen; _width = width; _growTime = growTime; _damage = damage; _knock = knockback;
        transform.position = origin;
        transform.right = _dir;
        _t = 0f;
        if (_box)
        {
            _box.size = new Vector2(0.1f, Mathf.Max(0.1f, _width));
            _box.offset = new Vector2(_box.size.x * 0.5f, 0f);
        }
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / _growTime);
        float len = Mathf.Lerp(0.1f, _maxLen, k);
        if (_box)
        {
            _box.size = new Vector2(len, Mathf.Max(0.1f, _width));
            _box.offset = new Vector2(len * 0.5f, 0f);
        }
        if (k >= 1f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var e = other ? other.GetComponentInParent<Enemy>() : null;
        if (!e || _hit.Contains(e)) return;
        _hit.Add(e);

        var hp = e.GetComponent<EnemyHealth>();
        if (hp) hp.TakeDamage(_damage);
        else    e.Kill();

        var rb = e.GetComponent<Rigidbody2D>();
        if (rb) rb.AddForce((e.transform.position - transform.position).normalized * _knock, ForceMode2D.Impulse);
    }
}