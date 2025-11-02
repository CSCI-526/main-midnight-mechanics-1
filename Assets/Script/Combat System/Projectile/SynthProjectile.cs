using System.Collections.Generic;
using UnityEngine;
using Game.Skills; 

[RequireComponent(typeof(BoxCollider2D))]
public sealed class SynthProjectile : ProjectileBase
{
    Transform _follow;
    float _dur, _t;
    float _length, _width;
    int _dps;
    float _tickRate;
    float _aimLerp;

    BoxCollider2D _box;
    readonly Dictionary<Enemy, float> _nextTick = new();

    void Awake()
    {
        _box = GetComponent<BoxCollider2D>();
        if (_box) { _box.isTrigger = true; _box.size = new Vector2(4f, 1f); _box.offset = new Vector2(2f, 0f); }
    }

    public void Activate(Transform follow, float duration, float length, float width, int dps, float tickRate, float aimLerp)
    {
        _follow = follow; _dur = duration; _length = length; _width = width;
        _dps = dps; _tickRate = tickRate; _aimLerp = aimLerp;
        _t = 0f;
        if (_box)
        {
            _box.size = new Vector2(_length, _width);
            _box.offset = new Vector2(_length * 0.5f, 0f);
        }
        transform.position = follow ? follow.position : Vector3.zero;
    }

    void Update()
    {
        if (!_follow) { Destroy(gameObject); return; }

        _t += Time.deltaTime;
        if (_t >= _dur) { Destroy(gameObject); return; }

        transform.position = _follow.position;

        var target = SkillUtil.FindNearestEnemy(_follow.position);
        Vector2 wantDir = target ? ((Vector2)target.transform.position - (Vector2)_follow.position).normalized : Vector2.up;
        Vector2 curDir  = transform.right;
        Vector2 newDir  = Vector2.Lerp(curDir, wantDir, Mathf.Clamp01(_aimLerp * Time.deltaTime)).normalized;
        transform.right = newDir;

        float perTick = _dps / Mathf.Max(1f, _tickRate);
        float interval = 1f / Mathf.Max(1f, _tickRate);

        foreach (var e in Enemy.All)
        {
            if (!e) continue;
            Vector2 local = (Vector2)(Quaternion.Inverse(transform.rotation) * (e.transform.position - transform.position));
            if (local.x < 0f || local.x > _length) continue;
            if (Mathf.Abs(local.y) > _width * 0.5f) continue;

            float now = Time.unscaledTime;
            if (!_nextTick.TryGetValue(e, out float next) || now >= next)
            {
                _nextTick[e] = now + interval;
                var hp = e.GetComponent<EnemyHealth>();
                if (hp) hp.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(perTick)));
                else    e.Kill();
            }
        }
    }
}
