using UnityEngine;
using Game.Skills; 

public sealed class TrumpetProjectile : ProjectileBase
{
    Transform _target;
    Vector2 _dir;
    float _speed, _steer, _life, _age;
    int _damage;

    [SerializeField] private float minDirLen = 1e-6f;
    [SerializeField] private float hitRadius = 0.4f;

    public void Launch(Vector2 start, Vector2 dir, Enemy target, float speed, float steer, float lifeTime, int damage)
    {
        transform.position = start;
        _dir = dir.sqrMagnitude < minDirLen ? Vector2.up : dir.normalized;
        _target = target ? target.transform : null;
        _speed = speed; _steer = steer; _life = lifeTime; _damage = damage; _age = 0f;
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age >= _life) { Destroy(gameObject); return; }

        if (_target)
        {
            Vector2 want = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            _dir = Vector2.Lerp(_dir, want, Mathf.Clamp01(_steer * Time.deltaTime)).normalized;
        }

        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
        transform.right = _dir;

        var nearest = SkillUtil.FindNearestEnemy(transform.position);
        if (nearest && ((Vector2)(nearest.transform.position) - (Vector2)transform.position).sqrMagnitude <= hitRadius * hitRadius)
        {
            var hp = nearest.GetComponent<EnemyHealth>();
            if (hp) hp.TakeDamage(_damage);
            else    nearest.Kill();
            Destroy(gameObject);
        }
    }
}