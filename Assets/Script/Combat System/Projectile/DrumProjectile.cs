using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class DrumProjectile : ProjectileBase
{
    [SerializeField] private bool useTrigger = true;

    CircleCollider2D _col;
    Vector2 _center;
    float _r0, _r1, _dur, _t;
    int _damage;
    readonly HashSet<Enemy> _hit = new();

    void Awake()
    {
        _col = GetComponent<CircleCollider2D>();
        if (_col) { _col.isTrigger = useTrigger; _col.radius = 0.01f; }
    }

    public void Configure(Vector2 center, float startRadius, float endRadius, float duration, int damage)
    {
        _center = center; _r0 = startRadius; _r1 = endRadius; _dur = duration; _damage = damage;
        transform.position = _center;
        _t = 0f;
        if (_col) _col.radius = _r0;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / _dur);
        float r = Mathf.Lerp(_r0, _r1, k);
        if (_col) _col.radius = r;
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
    }
}