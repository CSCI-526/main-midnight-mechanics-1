using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody2D rb;
    private Vector2 velocity;

    // 初始化：给它一个朝向
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
            enemy.Kill();
            Die();
        }
    }

    void Die() => Destroy(gameObject);
}