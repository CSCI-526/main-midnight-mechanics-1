using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization; // ★ 为了 FormerlySerializedAs

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    // Runtime registry
    private static readonly HashSet<Enemy> Alive = new();
    public static IReadOnlyCollection<Enemy> All => Alive;

    // Move / Touch
    [Header("Move")]
    [SerializeField] private float moveSpeed = 1.8f;

    [Header("On Touch")]
    [SerializeField] private bool clearAllOnTouch = true;
    [SerializeField] private bool destroyOnTouch  = true;

    // —— 旧：绝对掉粉（保留以免 Prefab 崩；建议以后不再使用）——
    [FormerlySerializedAs("useLossOverride")]
    [SerializeField, Tooltip("Legacy: 使用绝对掉粉范围（不再推荐）。")]
    private bool useAbsoluteLossOverride = false;

    [FormerlySerializedAs("touchLossOverride")]
    [SerializeField, Tooltip("Legacy: 碰撞时的绝对掉粉（人数）范围。")]
    private Vector2Int touchLossOverrideLegacy = new Vector2Int(200, 250);

    // —— 新：百分比掉粉（推荐）——
    [SerializeField, Tooltip("使用百分比掉粉（建议）")]
    private bool usePercentOverride = true;

    [SerializeField, Tooltip("碰撞一次的掉粉百分比区间（相对当前观众数）。例如 6~9 表示掉 6%~9%")]
    private Vector2 touchLossPercentOverride = new Vector2(6f, 9f);

    // --- 命中反馈（无需改技能） ---
    [Header("Hit Feedback")]
    [Tooltip("命中后原地停顿时长（秒）")]
    [SerializeField] private float hitStopSecondsDefault = 0.06f;
    [Tooltip("命中时抖动幅度（世界单位）")]
    [SerializeField] private float shakeAmplitudeDefault = 0.08f;
    [Tooltip("抖动衰减强度（越大越快停）")]
    [SerializeField] private float shakeDamp = 12f;

    private float _hitStopUntil;
    private Vector2 _shakePrev;
    private Vector2 _shakeCurr;
    private float _shakeAmp;
    private float _shakeSeed;

    // Internals
    private Transform   _target;
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _shakeSeed = Random.value * 1000f;
    }

    void OnEnable()
    {
        Alive.Add(this);
        EnsureTarget();
    }

    void OnDisable()
    {
        Alive.Remove(this);
    }

    void FixedUpdate()
    {
        if (!_rb) return;

        // 追踪速度（命中停顿期间为 0）
        Vector2 chaseVel = Vector2.zero;
        if (_target)
        {
            if (Time.time >= _hitStopUntil)
            {
                Vector2 dir = ((Vector2)_target.position - _rb.position).normalized;
                chaseVel = dir * moveSpeed;
            }
        }

        // 抖动位移（差分）
        Vector2 shakeDelta = Vector2.zero;
        if (_shakeAmp > 0f)
        {
            float t = Time.time;
            float nx = Mathf.PerlinNoise(_shakeSeed, t * 50f) * 2f - 1f;
            float ny = Mathf.PerlinNoise(_shakeSeed + 123.45f, t * 50f) * 2f - 1f;
            _shakeCurr = new Vector2(nx, ny) * _shakeAmp;

            shakeDelta = _shakeCurr - _shakePrev;
            _shakePrev = _shakeCurr;

            _shakeAmp -= shakeDamp * Time.fixedDeltaTime * _shakeAmp;
            if (_shakeAmp <= 0.001f) { _shakeAmp = 0f; _shakePrev = _shakeCurr = Vector2.zero; }
        }

        Vector2 totalMove = chaseVel * Time.fixedDeltaTime + shakeDelta;
        _rb.MovePosition(_rb.position + totalMove);
    }

    void OnTriggerEnter2D(Collider2D other) => Touch(other);
    void OnCollisionEnter2D(Collision2D c)  => Touch(c.collider);

    void Touch(Collider2D col)
    {
        if (!col) return;
        var viewers = col.GetComponentInParent<ViewerSystem>();
        if (!viewers) return;

        // —— 掉粉逻辑：优先百分比，其次兼容旧绝对值 —— 
        if (usePercentOverride)
        {
            viewers.LoseRandomPercentInRange(touchLossPercentOverride);
        }
        else if (useAbsoluteLossOverride)
        {
            viewers.LoseRandomInRange(touchLossOverrideLegacy);
        }
        else
        {
            // 使用系统默认的百分比区间
            viewers.LoseRandomPercentInRange(viewers.DefaultTouchLossPercentRange);
        }

        if (clearAllOnTouch) KillAll();
        else if (destroyOnTouch) Destroy(gameObject);
    }

    void EnsureTarget()
    {
        if (_target) return;
        var vs = FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
        _target = vs ? vs.transform : GameObject.FindWithTag("Player")?.transform;
    }

    // External API
    public void SetTarget(Transform t) { if (t) _target = t; }
    public void SetMoveSpeed(float s)  { moveSpeed = Mathf.Max(0f, s); }
    public void Kill() { if (this) Destroy(gameObject); }

    public static void KillAll()
    {
        if (Alive.Count == 0) return;
        var snapshot = new List<Enemy>(Alive);
        foreach (var e in snapshot) if (e) Destroy(e.gameObject);
    }

    // —— 供 EnemyHealth 调用：命中停顿 + 抖动 —— 
    public void HitPauseAndShake(float stopSeconds = -1f, float shakeAmplitude = -1f)
    {
        float dur = (stopSeconds > 0f) ? stopSeconds : hitStopSecondsDefault;
        _hitStopUntil = Time.time + dur;

        float amp = (shakeAmplitude >= 0f) ? shakeAmplitude : shakeAmplitudeDefault;
        _shakeAmp = Mathf.Max(_shakeAmp, amp);
        _shakePrev = _shakeCurr = Vector2.zero;
    }
}
