using UnityEngine;

/// <summary>
/// Projectile that orbits around a pivot (player) and kills enemies on trigger contact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class OrbitOrbProjectile : MonoBehaviour
{
    [SerializeField] private float orbitRadius = 3f;
    [SerializeField] private float angularSpeedDeg = 140f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float startAngleDeg = 0f;

    private Transform _pivot;
    private float _angle;
    private Rigidbody2D _rb;

    /// <summary>
    /// Initialize the orbit projectile.
    /// </summary>
    /// <param name="pivot">Orbit center (usually player transform).</param>
    /// <param name="radius">Orbit radius in world units.</param>
    /// <param name="angularSpeed">Angular speed in degrees/second.</param>
    /// <param name="ttlSeconds">Lifetime before auto-despawn.</param>
    /// <param name="startAngle">Initial angle (degrees) around the pivot.</param>
    public void Initialize(Transform pivot, float radius, float angularSpeed, float ttlSeconds, float startAngle)
    {
        _pivot = pivot;
        orbitRadius = Mathf.Max(0.01f, radius);
        angularSpeedDeg = angularSpeed;
        lifeTime = Mathf.Max(0.01f, ttlSeconds);
        startAngleDeg = startAngle;

        _angle = startAngleDeg;
        UpdatePositionImmediate();

        CancelInvoke();
        Invoke(nameof(Die), lifeTime);
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.simulated = true;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void FixedUpdate()
    {
        if (_pivot == null) { Die(); return; }

        _angle += angularSpeedDeg * Time.fixedDeltaTime;
        var rad = _angle * Mathf.Deg2Rad;
        var targetPos = (Vector2)_pivot.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        _rb.MovePosition(targetPos);
    }

    private void UpdatePositionImmediate()
    {
        if (_pivot == null) return;
        var rad = _angle * Mathf.Deg2Rad;
        transform.position = (Vector2)_pivot.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null) enemy.Kill();
    }

    private void Die()
    {
        if (this != null) Destroy(gameObject);
    }
}
