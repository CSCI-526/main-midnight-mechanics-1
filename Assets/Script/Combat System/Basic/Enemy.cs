using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private bool useLossOverride = false;
    [SerializeField] private Vector2Int touchLossOverride = new Vector2Int(200, 250);

    // --- 命中反馈（无需改技能） ---
    [Header("Hit Feedback")]
    [Tooltip("命中后原地停顿时长（秒）")]
    [SerializeField] private float hitStopSecondsDefault = 0.06f;
    [Tooltip("命中时抖动幅度（世界单位）")]
    [SerializeField] private float shakeAmplitudeDefault = 0.08f;
    [Tooltip("抖动衰减强度（越大越快停）")]
    [SerializeField] private float shakeDamp = 12f;

    private float _hitStopUntil;
    private Vector2 _shakePrev;     // 上一帧的抖动位移
    private Vector2 _shakeCurr;     // 当前帧的抖动位移
    private float _shakeAmp;        // 当前抖动幅度（逐帧衰减）
    private float _shakeSeed;       // 每只敌人一份噪声种子

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

        // 计算抖动位移（视觉位移，使用差分避免累积漂移）
        Vector2 shakeDelta = Vector2.zero;
        if (_shakeAmp > 0f)
        {
            // 用 Perlin 噪声做更“连贯”的抖动
            float t = Time.time;
            float nx = Mathf.PerlinNoise(_shakeSeed, t * 50f) * 2f - 1f;
            float ny = Mathf.PerlinNoise(_shakeSeed + 123.45f, t * 50f) * 2f - 1f;
            _shakeCurr = new Vector2(nx, ny) * _shakeAmp;

            shakeDelta = _shakeCurr - _shakePrev;
            _shakePrev = _shakeCurr;

            // 衰减
            _shakeAmp -= shakeDamp * Time.fixedDeltaTime * _shakeAmp;
            if (_shakeAmp <= 0.001f) { _shakeAmp = 0f; _shakePrev = _shakeCurr = Vector2.zero; }
        }

        // 基于差分位移的移动（不会把抖动永久写入位置）
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

        if (useLossOverride) viewers.LoseRandomInRange(touchLossOverride);
        else                 viewers.LoseRandomInRange(viewers.DefaultTouchLossRange);

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

    // —— 供 EnemyHealth 调用：命中停顿 + 抖动（参数可覆盖默认值）——
    public void HitPauseAndShake(float stopSeconds = -1f, float shakeAmplitude = -1f)
    {
        float dur = (stopSeconds > 0f) ? stopSeconds : hitStopSecondsDefault;
        _hitStopUntil = Time.time + dur;

        float amp = (shakeAmplitude >= 0f) ? shakeAmplitude : shakeAmplitudeDefault;
        _shakeAmp = Mathf.Max(_shakeAmp, amp);      // 叠加命中时取较大幅度
        // 立即刷新一次抖动位移，避免第一帧不动
        _shakePrev = _shakeCurr = Vector2.zero;
    }
}
