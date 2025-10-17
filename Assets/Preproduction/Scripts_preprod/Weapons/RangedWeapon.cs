using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class RangedWeapon : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private bool pierce = false;

    private Rigidbody2D rb;
    private Vector2 velocity;
    private int hitCount;
    private const int maxHits = 2;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        hitCount = 0;
    }

    public void Fire(Vector3 from, Vector3 to)
    {
        transform.position = from;
        Vector2 dir = (to - from).normalized;
        velocity = dir * speed;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
        Invoke(nameof(Die), lifeTime);
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<Enemy>();
            if (enemy)
            {
                enemy.Kill();
                hitCount++;
                if (!pierce || hitCount >= maxHits)
                    Die();
            }
        }
    }

    void Die()
    {
        CancelInvoke(nameof(Die));
        Destroy(gameObject);
    }
}