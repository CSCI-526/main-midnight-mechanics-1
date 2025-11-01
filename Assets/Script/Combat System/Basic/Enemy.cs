using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    private static readonly HashSet<Enemy> Alive = new();
    public static IReadOnlyCollection<Enemy> All => Alive;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 1.8f;

    [Header("On Touch")]
    [SerializeField] private bool clearAllOnTouch = true; 
    [SerializeField] private bool destroyOnTouch  = true;
    [SerializeField] private bool useLossOverride = false;
    [SerializeField] private Vector2Int touchLossOverride = new Vector2Int(200, 250);

    private Transform target;
    private Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void OnEnable()
    {
        Alive.Add(this);
        if (!target)
        {
            var vs = Object.FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
            target = vs ? vs.transform : GameObject.FindWithTag("Player")?.transform;
        }
    }

    void OnDisable() { Alive.Remove(this); }

    void FixedUpdate()
    {
        if (!target) return;
        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    // ===== External setup API (used by spawner) =====
    public void SetTarget(Transform t) { if (t) target = t; }
    public void SetMoveSpeed(float s)   { moveSpeed = Mathf.Max(0f, s); }

    void OnTriggerEnter2D(Collider2D other) => Touch(other);
    void OnCollisionEnter2D(Collision2D col) => Touch(col.collider);

    void Touch(Collider2D col)
    {
        if (!col) return;
        var viewers = col.GetComponentInParent<ViewerSystem>();
        if (!viewers) return;

        if (useLossOverride) viewers.LoseRandomInRange(touchLossOverride);
        else                 viewers.LoseRandomInRange(viewers.DefaultTouchLossRange);

        // 
        if (clearAllOnTouch) Enemy.KillAll();
        else if (destroyOnTouch) Destroy(gameObject);
    }

    public void Kill() { if (this) Destroy(gameObject); }

    public static void KillAll()
    {
        if (Alive.Count == 0) return;
        var snapshot = new List<Enemy>(Alive);
        foreach (var e in snapshot) if (e) Destroy(e.gameObject);
    }
}
