using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnergyBlade : MonoBehaviour
{
    [SerializeField] private float slashRange = 1.5f;
    [SerializeField] private float slashAngle = 120f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 0.4f;

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

    public void Slash(Vector3 origin, Vector2 direction)
    {
        if (cooldownTimer > 0)
            return;

        transform.position = origin;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, slashRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Vector2 toTarget = hit.transform.position - origin;
                float angle = Vector2.Angle(direction, toTarget);
                if (angle <= slashAngle * 0.5f)
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