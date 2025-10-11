using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    private static readonly HashSet<Enemy> Alive = new();
    public static IReadOnlyCollection<Enemy> All => Alive;

    [SerializeField] private float moveSpeed = 1.8f;

    private Transform target;   
    private Rigidbody2D rb;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void OnEnable()
    {
        Alive.Add(this);
        
        if (!target)
        {
            var tagged = GameObject.FindWithTag("Player");
            target = tagged ? tagged.transform : FindObjectOfType<PlayerHealth>()?.transform;
        }
    }

    void OnDisable() { Alive.Remove(this); }

    void FixedUpdate()
    {
        if (!target) return;
        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    public void SetTarget(Transform t) => target = t;
    public void SetMoveSpeed(float s)  => moveSpeed = s;
    
    public void Kill()
    {
        if (this) Destroy(gameObject);
    }
    
    void OnTriggerEnter2D(Collider2D other)   => TryTouchPlayer(other);
    void OnCollisionEnter2D(Collision2D col)  => TryTouchPlayer(col.collider);


    void TryTouchPlayer(Collider2D col)
    {
        if (!col) return;
        
        var hp = col.GetComponentInParent<PlayerHealth>();
        if (!hp) return;

        hp.TakeDamage(1); 
    }
    
    public static void KillAll()
    {
        if (Alive.Count == 0) return;
        var snapshot = new List<Enemy>(Alive);
        foreach (var e in snapshot)
        {
            if (e) Destroy(e.gameObject);
        }
    }
}
