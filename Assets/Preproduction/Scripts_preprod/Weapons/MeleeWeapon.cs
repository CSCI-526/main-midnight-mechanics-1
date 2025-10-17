using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeleeWeapon : MonoBehaviour
{
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackAngle = 90f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 0.5f;

    private float cooldownTimer;
    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        cooldownTimer = 0f;
    }

    void Update()
    {
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;
    }

    public void Attack(Vector3 origin, Vector2 direction)
    {
        if (cooldownTimer > 0)
            return;

        transform.position = origin;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Vector2 toEnemy = hit.transform.position - origin;
                float angle = Vector2.Angle(direction, toEnemy);
                if (angle <= attackAngle * 0.5f)
                {
                    var enemy = hit.GetComponent<Enemy>();
                    if (enemy)
                        enemy.Kill();
                }
            }
        }

        cooldownTimer = cooldown;
    }
}