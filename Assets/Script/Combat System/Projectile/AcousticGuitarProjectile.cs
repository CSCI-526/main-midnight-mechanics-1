using System.Collections.Generic;
using UnityEngine;

public sealed class AcousticGuitarProjectile : ProjectileBase
{
    Vector2 _center;
    Vector2 _startDir;
    float _radius, _arcTotalDeg, _dur, _t;
    int _damage;

    readonly HashSet<Enemy> _hit = new();

    public void Sweep(Vector2 center, Vector2 facingDir, float radius, float arcTotalDeg, float duration, int damage)
    {
        _center = center;
        _startDir = Quaternion.Euler(0,0,-arcTotalDeg * 0.5f) * (facingDir.sqrMagnitude < 1e-6f ? Vector2.up : facingDir.normalized);
        _radius = radius; _arcTotalDeg = arcTotalDeg; _dur = duration; _damage = damage; _t = 0f;
        transform.position = center;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / _dur);
        float curDeg = k * _arcTotalDeg;
        Vector2 curDir = Quaternion.Euler(0,0,curDeg) * _startDir;

        foreach (var e in Enemy.All)
        {
            if (!e || _hit.Contains(e)) continue;
            Vector2 to = (Vector2)e.transform.position - _center;
            if (to.sqrMagnitude > _radius * _radius) continue;

            float ang = Vector2.SignedAngle(_startDir, to.normalized);
            if (ang >= 0f && ang <= curDeg)
            {
                _hit.Add(e);
                var hp = e.GetComponent<EnemyHealth>();
                if (hp) hp.TakeDamage(_damage);
                else    e.Kill();
            }
        }

        if (k >= 1f) Destroy(gameObject);
    }
}