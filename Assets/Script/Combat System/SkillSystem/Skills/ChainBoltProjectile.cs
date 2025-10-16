using UnityEngine;

/// <summary>
/// Chain projectile that travels in a direction, kills on contact,
/// then hops to the next nearest enemy within a radius until hop budget ends.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ChainBoltProjectile : MonoBehaviour
{
    [SerializeField] private float acceleration = 0f;
    [SerializeField] private LayerMask enemyMask = ~0;

    private Rigidbody2D _rb;
    private float _speed;
    private float _searchRadius;
    private int   _remainingHops;
    private float _lifeTime;

    private Vector2 _velocity;
    private Enemy   _seekTarget;

    /// <summary>
    /// Initializes the chain bolt.
    /// </summary>
    public void Initialize(Vector3 startPosition,
                           Vector2 startDirection,
                           float moveSpeed,
                           int maxHops,
                           float searchRadius,
                           float lifeTime)
    {
        transform.position = startPosition;
        _speed         = moveSpeed;
        _remainingHops = Mathf.Max(0, maxHops);
        _searchRadius  = Mathf.Max(0.1f, searchRadius);
        _lifeTime      = Mathf.Max(0.1f, lifeTime);
        _velocity      = startDirection.normalized * _speed;

        CancelInvoke();
        Invoke(nameof(Die), _lifeTime);
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void FixedUpdate()
    {
        if (acceleration > 0f)
        {
            _speed += acceleration * Time.fixedDeltaTime;
            _velocity = _velocity.normalized * _speed;
        }

        // If we have a valid seek target, steer gently towards it.
        if (_seekTarget)
        {
            Vector2 dir = ((Vector2)_seekTarget.transform.position - _rb.position).normalized;
            _velocity = Vector2.Lerp(_velocity.normalized, dir, 0.2f) * _speed;
        }

        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (!enemy) return;

        // Kill this enemy
        enemy.Kill();

        // Try hop to the next one
        if (_remainingHops > 0 && TryAcquireNextTarget(enemy.transform.position))
        {
            _remainingHops--;
            // Reorient velocity towards the new target immediately
            if (_seekTarget)
            {
                Vector2 dir = ((Vector2)_seekTarget.transform.position - _rb.position).normalized;
                _velocity = dir * _speed;
                return;
            }
        }

        // No more hops → die
        Die();
    }

    private bool TryAcquireNextTarget(Vector2 fromPos)
    {
        Enemy best = null;
        float bestSqr = float.PositiveInfinity;

        foreach (var e in Enemy.All)
        {
            if (!e) continue;
            float d2 = ((Vector2)e.transform.position - fromPos).sqrMagnitude;
            if (d2 <= _searchRadius * _searchRadius && d2 < bestSqr)
            {
                bestSqr = d2;
                best = e;
            }
        }

        _seekTarget = best;
        return _seekTarget != null;
    }

    private void Die()
    {
        if (this) Destroy(gameObject);
    }
}
