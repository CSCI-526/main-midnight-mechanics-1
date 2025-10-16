using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody2D rb;
    private Vector2 velocity;
    private int damage = 1; // TODO: 敌人做血量后启用

    public float DefaultSpeed => speed;

    public void Configure(float speedOverride, int damageValue)
    {
        if (speedOverride > 0f) speed = speedOverride;
        damage = Mathf.Max(1, damageValue);
    }
    
    public void FireDir(Vector3 from, Vector2 dir)
    {
        transform.position = from;
        velocity = dir.normalized * speed;
    }
    
    public void FireTowards(Vector3 from, Vector3 to)
    {
        transform.position = from;
        Vector2 dir = (to - from).normalized;
        velocity = dir * speed;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Invoke(nameof(Die), lifeTime);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy)
        {
            // TODO: 敌人做 HP 后：enemy.TakeDamage(damage);
            enemy.Kill();
            Die();
        }
    }

    void Die() => Destroy(gameObject);
}