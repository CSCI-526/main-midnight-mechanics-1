using UnityEngine;

/// <summary>
/// Simple pellet for SpreadShot. Kinematic 2D projectile; trigger-hit kills enemies.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class SpreadPelletProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private int   damage = 1;       // 预留：如果以后做敌人血量可用
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody2D _rb;
    private Vector2     _velocity;

    public float DefaultSpeed => speed;

    /// <summary>Configure projectile stats.</summary>
    public void Configure(float moveSpeed, int dmg)
    {
        speed  = Mathf.Max(0.01f, moveSpeed);
        damage = Mathf.Max(0, dmg);
    }

    /// <summary>Fire in a direction from a start position.</summary>
    public void FireDir(Vector2 start, Vector2 dir)
    {
        transform.position = start;
        _velocity = dir.normalized * speed;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType     = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.simulated    = true;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        Invoke(nameof(Die), lifeTime);
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Kill();
            Die();
        }
    }

    private void Die()
    {
        if (this != null) Destroy(gameObject);
    }
}