using System;
using UnityEngine;
using UnityEngine.UI;

public enum NoteJudgement { Perfect, Good, Miss }

/// <summary>
/// 管理观众数、动量、环境增长、爆火窗口，并向其它系统提供统一的频率倍率。
/// 新增：
/// 1) Startup Surge：开局在设定时长内平滑冲到目标人数（作为“地板”基线，不干扰你手动增减）
/// 2) 趋势箭头：按最近一次可视更新的净变化设置涨/跌图标（带节流 & 死区）
/// </summary>
public class ViewerSystem : MonoBehaviour
{
    // ===== 基础配置 =====
    [Header("Starting Viewers")]
    [SerializeField] private int startViewers = 200;

    [Header("On Enemy Touch (global default)")]
    [SerializeField] private Vector2Int touchLossRange = new Vector2Int(200, 250);

    [Header("Judgement Deltas (per hit)")]
    [SerializeField] private int perfectGain = 20;
    [SerializeField] private int goodGain    = 10;
    [SerializeField] private int missLoss    = 18;

    // ===== 环境批量可视更新（1~2s 一次，避免数字闪太快）=====
    [Header("Ambient Update Cadence")]
    [Tooltip("每次“批量可视更新”的间隔范围（秒）。建议 1~2s）")]
    [SerializeField] private Vector2 ambientUpdateEvery = new Vector2(1.0f, 2.0f);

    [Header("Ambient Tilt: % gain per minute (baseline)")]
    [Tooltip("基线：每分钟的期望增幅百分比（随观众/表现/进度/爆火再放大）。")]
    [SerializeField, Range(0f, 200f)]
    private float ambientTiltPercentPerMinute = 6f;

    [Header("Ambient Jitter (small symmetric noise per tick)")]
    [SerializeField] private Vector2Int ambientJitterStepRange = new Vector2Int(0, 3);
    [SerializeField] private bool ambientEmitDeltaEvents = false;
    [SerializeField] private bool ambientCanDeplete = false;

    [Header("Idle Decay")]
    [Tooltip("多少秒内没有 Perfect/Good 视为“无高质量输入”的空窗")]
    [SerializeField] private float idleGraceSeconds = 1.8f;
    [Tooltip("空窗期间的额外掉粉（百分比/分钟，按当前人数计算）")]
    [SerializeField, Range(0f, 200f)]
    private float idleLossPercentPerMinute = 14f;

    // ===== 表现动量 & 进度加速 =====
    [Header("Performance Momentum")]
    [SerializeField] private float perfectBump = +0.08f;
    [SerializeField] private float goodBump    = +0.03f;
    [SerializeField] private float missBump    = -0.12f;
    [Tooltip("动量每秒向0衰减")]
    [SerializeField] private float momentumDecayPerSec = 0.35f;
    [Tooltip("动量如何放大频率/增长（最终乘以 1 + max(0, m)*factor）")]
    [SerializeField] private float momentumToRateFactor = 1.15f;

    [Header("Progress Boost (song progress 0..1 → x倍率)")]
    [SerializeField] private AnimationCurve progressBoost = new AnimationCurve(
        new Keyframe(0f, 0.0f),
        new Keyframe(0.25f, 0.2f),
        new Keyframe(0.50f, 0.6f),
        new Keyframe(0.75f, 1.1f),
        new Keyframe(1.00f, 1.6f)
    );

    [Tooltip("可选引用 LevelRunner，读取 Progress01；为空则按0处理")]
    [SerializeField] private LevelRunner levelRunner;

    // ===== 统一倍率（给 ChatFeed / LiveRewardTicker 用）=====
    [Header("Unified Rate Multiplier by Viewers")]
    [SerializeField] private int rateBaselineViewers = 1000; // 以此为 1.0x
    [SerializeField] private AnimationCurve rateByViewers = new AnimationCurve(
        new Keyframe(0.00f, 0.00f),
        new Keyframe(0.10f, 0.02f),
        new Keyframe(0.25f, 0.10f),
        new Keyframe(0.50f, 0.30f),
        new Keyframe(1.00f, 1.00f),
        new Keyframe(2.00f, 2.50f),
        new Keyframe(5.00f, 6.00f),
        new Keyframe(10.0f, 10.0f)
    );

    // ===== 罕见“爆火”窗口 =====
    [Header("Viral Spike (rare burst)")]
    [SerializeField, Range(0f, 1f)] private float hypeBaseChancePerMinute = 0.015f;
    [SerializeField] private float hypeExtraChancePerPerfect = 0.0006f;
    [SerializeField] private Vector2 hypeDurationRange = new Vector2(8f, 16f);
    [SerializeField] private Vector2 hypeTiltBoostRange = new Vector2(3f, 7f);
    [SerializeField] private float hypeRateMultiplier = 2.0f;

    // ===== 开局冲刺（不影响你的命中逻辑，只提供“地板基线”）=====
    [Header("Startup Surge (quick baseline to target)")]
    [SerializeField] private bool  startupSurgeEnabled = true;
    [SerializeField] private int   surgeTargetViewers   = 1000;
    [SerializeField, Min(0.5f)] private float surgeDurationSeconds = 10f;

    // ===== 趋势箭头 UI =====
    [Header("Trend Arrow UI")]
    [SerializeField] private Image  trendArrowImage;     // 绑定一个 Image
    [SerializeField] private Sprite trendUpSprite;       // 绿色向上
    [SerializeField] private Sprite trendDownSprite;     // 红色向下
    [SerializeField, Min(0f)] private float trendMinUpdateInterval = 0.5f; // 节流
    [SerializeField, Min(0)] private int   trendDeadzone = 3;              // 死区：变化太小不切换

    // ===== 运行时状态 =====
    public int Current { get; private set; }
    public bool IsDepleted { get; private set; }
    public Vector2Int DefaultTouchLossRange => touchLossRange;

    public event Action<int> OnViewersChanged;
    public event Action<int> OnDeltaApplied;
    public event Action OnDepleted;

    float _momentum;
    float _lastGoodOrPerfectTime;
    float _accTilt;
    float _nextAmbientAtU;
    int   _perfectsSinceLastHype;
    bool  _hypeActive;
    float _hypeUntilU;
    float _hypeTiltBoost;

    // Startup Surge
    bool  _surgeActive;
    float _surgeStartU;
    float _surgeEndU;
    int   _surgeStartViewers;

    // Trend Arrow
    float _nextTrendAllowedU;

    // ===== 生命周期 =====
    void Awake()
    {
        ResetToStart();
        if (!levelRunner) levelRunner = FindFirstObjectByType<LevelRunner>(FindObjectsInactive.Include);

        HitJudge.OnPerfect += OnPerfect;
        HitJudge.OnGood    += OnGood;
        HitJudge.OnMiss    += OnMiss;

        ScheduleNextAmbient();
    }

    void OnDestroy()
    {
        HitJudge.OnPerfect -= OnPerfect;
        HitJudge.OnGood    -= OnGood;
        HitJudge.OnMiss    -= OnMiss;
    }

    // ===== 命中事件 → 动量 & 立即增减 =====
    void OnPerfect()
    {
        _momentum += perfectBump;
        _perfectsSinceLastHype++;
        _lastGoodOrPerfectTime = Time.unscaledTime;

        int before = Current;
        Gain(perfectGain);
        UpdateTrendIndicatorThrottled(before, Current);
    }

    void OnGood()
    {
        _momentum += goodBump;
        _lastGoodOrPerfectTime = Time.unscaledTime;

        int before = Current;
        Gain(goodGain);
        UpdateTrendIndicatorThrottled(before, Current);
    }

    void OnMiss()
    {
        _momentum += missBump;

        int before = Current;
        Lose(missLoss);
        UpdateTrendIndicatorThrottled(before, Current);
    }

    // ===== 主循环 =====
    void Update()
    {
        if (IsDepleted) return;

        // 动量向 0 衰减并夹限
        if (_momentum != 0f)
            _momentum = Mathf.MoveTowards(_momentum, 0f, momentumDecayPerSec * Time.unscaledDeltaTime);
        _momentum = Mathf.Clamp(_momentum, -1f, 2f);

        // 爆火结束？
        if (_hypeActive && Time.unscaledTime >= _hypeUntilU)
            _hypeActive = false;

        // 到了批量可视更新的时刻？
        if (Time.unscaledTime >= _nextAmbientAtU)
        {
            AmbientTick();
            ScheduleNextAmbient();
        }
    }

    // ===== 每次“可视批量更新”时做合并计算 =====
    void AmbientTick()
    {
        int before = Current;

        // 1) 倾斜收益（基线 × 规模/表现/进度/爆火）
        float basePerSec = Current * (ambientTiltPercentPerMinute / 100f) / 60f;

        float viewerMul = RateByViewersMultiplier();
        float perfMul   = 1f + Mathf.Max(0f, _momentum) * momentumToRateFactor;
        float progMul   = progressBoost.Evaluate(Progress01());
        float hypeMul   = _hypeActive ? _hypeTiltBoost : 1f;

        float perSec = basePerSec * viewerMul * perfMul * progMul * hypeMul;

        // 这次 tick 代表的“秒数”
        float dt = RandRange(ambientUpdateEvery);

        // 累积到 1 再结算
        _accTilt += perSec * dt;
        int inc = Mathf.FloorToInt(_accTilt);
        if (inc > 0)
        {
            _accTilt -= inc;
            Current += inc;
        }

        // 2) 抖动（小幅 ±）
        int jMin = Mathf.Min(ambientJitterStepRange.x, ambientJitterStepRange.y);
        int jMax = Mathf.Max(ambientJitterStepRange.x, ambientJitterStepRange.y);
        int mag  = UnityEngine.Random.Range(jMin, jMax + 1);
        if (mag != 0)
        {
            int sign = (UnityEngine.Random.value < 0.5f) ? -1 : +1;
            int jitter = sign * mag;
            if (!ambientCanDeplete) jitter = Mathf.Max(jitter, -(Current - 1));
            Current = Mathf.Max(0, Current + jitter);
        }

        // 3) Idle 掉粉：空窗超时则按百分比额外扣
        if (Time.unscaledTime - _lastGoodOrPerfectTime > idleGraceSeconds)
        {
            float idlePerSec = Current * (idleLossPercentPerMinute / 100f) / 60f;
            int idleLoss = Mathf.Max(0, Mathf.FloorToInt(idlePerSec * dt));
            if (idleLoss > 0) Current = Mathf.Max(0, Current - idleLoss);
        }

        // 4) Startup Surge：对“当前值”施加地板基线（平滑到目标）
        if (startupSurgeEnabled && _surgeActive)
        {
            float t01 = Mathf.InverseLerp(_surgeStartU, _surgeEndU, Time.unscaledTime);
            t01 = Mathf.Clamp01(t01);
            float te = EaseOutCubic(t01); // 平滑更自然
            int minTarget = Mathf.RoundToInt(Mathf.Lerp(_surgeStartViewers, surgeTargetViewers, te));
            if (Current < minTarget) Current = minTarget;
            if (t01 >= 1f) _surgeActive = false;
        }

        // 5) 罕见“爆火”判定
        TryTriggerHype(dt);

        // 事件派发（节流：每 tick 一次）
        int delta = Current - before;
        OnViewersChanged?.Invoke(Current);
        if (ambientEmitDeltaEvents && delta != 0) OnDeltaApplied?.Invoke(delta);

        // 趋势箭头（依据本 tick 的净变化）
        UpdateTrendIndicatorThrottled(before, Current);

        if (Current <= 0 && !IsDepleted)
        {
            if (!ambientCanDeplete) Current = 1; // 保护：环境不致死
            else { IsDepleted = true; OnDepleted?.Invoke(); }
        }
    }

    // ===== 爆火触发 =====
    void TryTriggerHype(float dtSeconds)
    {
        float pBase = hypeBaseChancePerMinute / 60f * dtSeconds;
        float pPerf = _perfectsSinceLastHype * hypeExtraChancePerPerfect;
        float p     = Mathf.Clamp01(pBase + pPerf);

        if (!_hypeActive && UnityEngine.Random.value < p)
        {
            _hypeActive = true;
            _hypeUntilU = Time.unscaledTime + UnityEngine.Random.Range(hypeDurationRange.x, hypeDurationRange.y);
            _hypeTiltBoost = UnityEngine.Random.Range(hypeTiltBoostRange.x, hypeTiltBoostRange.y);
            _perfectsSinceLastHype = 0;

            // 瞬时小幅跃升，营造“涨粉条”的感觉
            int surge = Mathf.RoundToInt(Current * UnityEngine.Random.Range(0.03f, 0.10f));
            Current += Mathf.Max(1, surge);
        }
    }

    // ===== 安排下一次批量更新 =====
    void ScheduleNextAmbient()
    {
        float dt = RandRange(ambientUpdateEvery);
        _nextAmbientAtU = Time.unscaledTime + dt;
    }

    // ===== 小工具 =====
    float RandRange(Vector2 v)
    {
        float a = Mathf.Min(v.x, v.y);
        float b = Mathf.Max(v.x, v.y);
        return UnityEngine.Random.Range(a, b);
    }

    float Progress01()
    {
        if (levelRunner) return Mathf.Clamp01(levelRunner.Progress01);
        return 0f;
    }

    static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    // ===== 统一倍率（外部系统拉频率用）=====
    float RateByViewersMultiplier()
    {
        float n = (float)Current / Mathf.Max(1, rateBaselineViewers);
        n = Mathf.Clamp(n, 0f, 10f);
        return rateByViewers.Evaluate(n);
    }

    public float GetUnifiedRateMultiplier()
    {
        float viewerMul = RateByViewersMultiplier();
        float perfMul   = 1f + Mathf.Max(0f, _momentum) * momentumToRateFactor;
        float progMul   = progressBoost.Evaluate(Progress01());
        float hypeMul   = _hypeActive ? hypeRateMultiplier : 1f;
        return Mathf.Max(0f, viewerMul * perfMul * progMul * hypeMul);
    }

    // ===== 外部 API（与你原版保持一致）=====
    public void ResetToStart()
    {
        IsDepleted = false;
        Current = Mathf.Max(0, startViewers);

        _accTilt = 0f;
        _momentum = 0f;
        _hypeActive = false;
        _perfectsSinceLastHype = 0;
        _lastGoodOrPerfectTime = Time.unscaledTime;

        // 初始化 Startup Surge（作为“地板基线”的时间轴）
        _surgeActive       = startupSurgeEnabled;
        _surgeStartU       = Time.unscaledTime;
        _surgeEndU         = _surgeStartU + Mathf.Max(0.5f, surgeDurationSeconds);
        _surgeStartViewers = Current;

        // 趋势箭头节流复位
        _nextTrendAllowedU = 0f;

        OnViewersChanged?.Invoke(Current);
    }

    public void ApplyJudgement(NoteJudgement j)
    {
        switch (j)
        {
            case NoteJudgement.Perfect: OnPerfect(); break;
            case NoteJudgement.Good:    OnGood();    break;
            case NoteJudgement.Miss:    OnMiss();    break;
        }
    }

    public void LoseRandomInRange(Vector2Int range)
    {
        int a = Mathf.Min(range.x, range.y);
        int b = Mathf.Max(range.x, range.y);
        int dmg = UnityEngine.Random.Range(a, b + 1);
        Lose(dmg);
    }

    public void Gain(int amount)
    {
        if (IsDepleted) return;
        int inc = Mathf.Max(0, amount);
        if (inc == 0) return;

        Current += inc;
        OnViewersChanged?.Invoke(Current);
        OnDeltaApplied?.Invoke(+inc);
    }

    public void Lose(int amount)
    {
        if (IsDepleted) return;
        int dec = Mathf.Max(0, amount);
        if (dec == 0) return;

        int before = Current;
        Current = Mathf.Max(0, Current - dec);
        OnViewersChanged?.Invoke(Current);
        OnDeltaApplied?.Invoke(-(before - Current));

        if (Current <= 0 && !IsDepleted)
        {
            IsDepleted = true;
            Debug.LogWarning("[Viewers] Depleted → GAME OVER");
            OnDepleted?.Invoke();
        }
    }

    // ===== 趋势箭头（节流 + 死区）=====
    void UpdateTrendIndicatorThrottled(int before, int after)
    {
        if (!trendArrowImage) return;

        if (Time.unscaledTime < _nextTrendAllowedU) return;

        int delta = after - before;
        if (Mathf.Abs(delta) < trendDeadzone) return;

        Sprite s = (delta > 0) ? trendUpSprite : trendDownSprite;
        if (s)
        {
            trendArrowImage.sprite  = s;
            trendArrowImage.enabled = true;
        }
        else
        {
            trendArrowImage.enabled = false;
        }

        _nextTrendAllowedU = Time.unscaledTime + trendMinUpdateInterval;
    }
}
