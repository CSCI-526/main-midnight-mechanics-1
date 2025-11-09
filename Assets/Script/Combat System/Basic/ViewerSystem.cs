using System;
using UnityEngine;
using UnityEngine.UI;

public enum NoteJudgement { Perfect, Good, Miss }

/// <summary>
/// 观众系统（保守高频小步版）
/// - PERFECT/GOOD：固定整数增量
/// - MISS / 敌人碰撞：按“当前人数百分比”扣
/// - 环境倾斜：很小的基线增长 + 绝对步长上限（看起来“跳得勤但每次很小”）
/// - 抖动：使用绝对小整数步（不随人数放大），且带轻微向下偏置
/// - Idle 掉粉：百分比，但每 tick 有上限，避免巨幅变化
/// - 爆火：很小的瞬时跃升（0.1%~0.3%）
/// </summary>
public class ViewerSystem : MonoBehaviour
{
    // ===== 基础配置 =====
    [Header("Starting Viewers")]
    [SerializeField] private int startViewers = 200;

    // 命中/失误：固定加分 + 百分比扣分
    [Header("Judgement Deltas")]
    [Tooltip("一次 PERFECT 固定增加多少观众")]
    [SerializeField, Min(0)] private int perfectGain = 20;
    [Tooltip("一次 GOOD 固定增加多少观众")]
    [SerializeField, Min(0)] private int goodGain    = 10;
    [Tooltip("一次 MISS 按当前人数扣的百分比")]
    [SerializeField, Range(0f, 5f)] private float missLossPercent = 1.2f;

    // 敌人碰撞：百分比区间
    [Header("On Enemy Touch Loss (% of current)")]
    [SerializeField] private Vector2 touchLossPercentRange = new Vector2(6f, 9f);

    // ===== 环境批量可视更新（高频小步）=====
    [Header("Ambient Update Cadence")]
    [Tooltip("每次“批量可视更新”的间隔范围（秒）。建议 0.4~0.8s，显得“常在跳”。")]
    [SerializeField] private Vector2 ambientUpdateEvery = new Vector2(0.45f, 0.80f);

    [Header("Ambient Tilt (very small baseline)")]
    [Tooltip("基线：每分钟的期望增幅百分比（非常小）。示例：0.25%/min。")]
    [SerializeField, Range(0f, 5f)] private float ambientTiltPercentPerMinute = 0.25f;

    [Tooltip("环境增长的单 tick 绝对步长上限（避免一次跳很多）。例如 6。")]
    [SerializeField, Min(1)] private int ambientMaxStepPerTick = 6;

    [Header("Ambient Jitter (absolute steps)")]
    [Tooltip("每 tick 加一点小抖动（绝对人数而非百分比），不随人数膨胀。")]
    [SerializeField] private Vector2Int ambientJitterStepAbsRange = new Vector2Int(0, 4);
    [Tooltip("抖动为负的概率（向下偏置），0.55~0.7 之间可取。")]
    [SerializeField, Range(0f, 1f)] private float ambientJitterNegativeBias = 0.6f;
    [SerializeField] private bool ambientEmitDeltaEvents = false;
    [SerializeField] private bool ambientCanDeplete = false;

    [Header("Idle Decay")]
    [Tooltip("多少秒内没有 Perfect/Good 视为“无高质量输入”的空窗")]
    [SerializeField] private float idleGraceSeconds = 1.8f;
    [Tooltip("空窗期间的额外掉粉（百分比/分钟，按当前人数计算）")]
    [SerializeField, Range(0f, 200f)] private float idleLossPercentPerMinute = 14f;
    [Tooltip("Idle 掉粉的单 tick 绝对步长上限，避免巨幅变化")]
    [SerializeField, Min(1)] private int idleMaxStepPerTick = 18;

    // ===== 表现动量 & 进度（仅做很轻的放大）=====
    [Header("Performance Momentum")]
    [SerializeField] private float perfectBump = +0.08f;
    [SerializeField] private float goodBump    = +0.03f;
    [SerializeField] private float missBump    = -0.12f;
    [Tooltip("动量每秒向0衰减")]
    [SerializeField] private float momentumDecayPerSec = 0.35f;
    [Tooltip("动量对“环境基线”的轻微上限倍率（1~1.05）")]
    [SerializeField, Range(1f, 1.2f)] private float ambientMomentumBoostMax = 1.05f;

    [Header("Progress Boost (very gentle)")]
    [Tooltip("进度带来的很轻的加成曲线（建议终点≤1.10）。")]
    [SerializeField] private AnimationCurve progressBoost = new AnimationCurve(
        new Keyframe(0f, 1.00f),
        new Keyframe(0.25f, 1.02f),
        new Keyframe(0.50f, 1.04f),
        new Keyframe(0.75f, 1.07f),
        new Keyframe(1.00f, 1.10f)
    );

    [Tooltip("可选引用 LevelRunner，读取 Progress01；为空则按0处理")]
    [SerializeField] private LevelRunner levelRunner;

    // ===== 统一倍率（外部系统拉频率用；不影响环境步进大小）=====
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

    // ===== 罕见“爆火”窗口（很小幅度）=====
    [Header("Viral Spike (tiny surge)")]
    [SerializeField, Range(0f, 1f)] private float hypeBaseChancePerMinute = 0.010f;
    [SerializeField] private float hypeExtraChancePerPerfect = 0.0003f;
    [SerializeField] private Vector2 hypeDurationRange = new Vector2(6f, 12f);
    [Tooltip("爆火时仅用于 GetUnifiedRateMultiplier 的倍率")]
    [SerializeField] private float hypeRateMultiplier = 1.6f;
    [Tooltip("瞬时跃升的百分比区间（相对当前），非常小，0.1%~0.3%")]
    [SerializeField] private Vector2 hypeInstantSurgePercent = new Vector2(0.10f, 0.30f);

    // ===== 趋势箭头 UI =====
    [Header("Trend Arrow UI")]
    [SerializeField] private Image  trendArrowImage;
    [SerializeField] private Sprite trendUpSprite;
    [SerializeField] private Sprite trendDownSprite;
    [SerializeField, Min(0f)] private float trendMinUpdateInterval = 0.5f;
    [SerializeField, Min(0)] private int   trendDeadzone = 3;

    // ===== 运行时状态 =====
    public int Current { get; private set; }
    public bool IsDepleted { get; private set; }

    // —— 兼容旧 API（如果有旧代码还在用）——
    [Obsolete("Use touchLossPercentRange and LoseRandomPercentInRange instead.")]
    [SerializeField] private Vector2Int touchLossRange = new Vector2Int(200, 250);
    [Obsolete("Use DefaultTouchLossPercentRange instead.")]
    public Vector2Int DefaultTouchLossRange => touchLossRange;

    // 新的默认百分比区间（外部可读）
    public Vector2 DefaultTouchLossPercentRange => touchLossPercentRange;

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

    // 趋势箭头
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

    // ===== 命中事件：固定加分 / 百分比扣分 =====
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
        LosePercent(missLossPercent);
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

    // ===== 每次“可视批量更新”：高频小步 =====
    void AmbientTick()
    {
        int before = Current;

        // 这次 tick 的持续时长
        float dt = RandRange(ambientUpdateEvery);

        // 1) 很小的基线增长（百分比/分钟 → 每秒），并做极轻的动量/进度上限放大
        float basePerSec = Current * (ambientTiltPercentPerMinute / 100f) / 60f;
        float perfMulAmbient = Mathf.Lerp(1f, ambientMomentumBoostMax, Mathf.Clamp01(Mathf.Max(0f, _momentum)));
        float progMulAmbient = progressBoost.Evaluate(Progress01());
        float perSec = basePerSec * perfMulAmbient * progMulAmbient;

        // 累积到整数并加上“单 tick 绝对步长上限”
        _accTilt += perSec * dt;
        int inc = Mathf.FloorToInt(_accTilt);
        if (inc != 0)
        {
            // 限制单 tick 最大步长（正负都限），看起来“常跳但不大”
            inc = Mathf.Clamp(inc, -ambientMaxStepPerTick, +ambientMaxStepPerTick);
            _accTilt -= inc;
            Current = Mathf.Max(0, Current + inc);
        }

        // 2) 抖动（绝对小整数步），带轻微向下偏置
        int jMin = Mathf.Min(ambientJitterStepAbsRange.x, ambientJitterStepAbsRange.y);
        int jMax = Mathf.Max(ambientJitterStepAbsRange.x, ambientJitterStepAbsRange.y);
        int mag  = UnityEngine.Random.Range(jMin, jMax + 1);
        if (mag != 0)
        {
            bool neg = UnityEngine.Random.value < ambientJitterNegativeBias;
            int jitter = neg ? -mag : +mag;
            if (!ambientCanDeplete) jitter = Mathf.Max(jitter, -(Current - 1));
            Current = Mathf.Max(0, Current + jitter);
            if (ambientEmitDeltaEvents && jitter != 0) OnDeltaApplied?.Invoke(jitter);
        }

        // 3) Idle 掉粉（百分比 → 单 tick 上限），避免巨幅跳动
        if (Time.unscaledTime - _lastGoodOrPerfectTime > idleGraceSeconds && Current > 0)
        {
            float idlePerSec = Current * (idleLossPercentPerMinute / 100f) / 60f;
            int idleLoss = Mathf.Max(0, Mathf.FloorToInt(idlePerSec * dt));
            if (idleLoss > 0)
            {
                idleLoss = Mathf.Min(idleLoss, idleMaxStepPerTick); // ★ 单 tick 上限
                Current = Mathf.Max(0, Current - idleLoss);
            }
        }

        // 4) 罕见“爆火”：很小的瞬时跃升（0.1%~0.3%），偶尔咚一下
        TryTriggerHype(dt);

        // 事件派发 / 趋势箭头
        int delta = Current - before;
        OnViewersChanged?.Invoke(Current);
        if (ambientEmitDeltaEvents && delta != 0) OnDeltaApplied?.Invoke(delta);
        UpdateTrendIndicatorThrottled(before, Current);

        if (Current <= 0 && !IsDepleted)
        {
            if (!ambientCanDeplete) Current = 1; // 保护：环境不致死
            else { IsDepleted = true; OnDepleted?.Invoke(); }
        }
    }

    // ===== 爆火触发（小幅）=====
    void TryTriggerHype(float dtSeconds)
    {
        float pBase = hypeBaseChancePerMinute / 60f * dtSeconds;
        float pPerf = _perfectsSinceLastHype * hypeExtraChancePerPerfect;
        float p     = Mathf.Clamp01(pBase + pPerf);

        if (!_hypeActive && UnityEngine.Random.value < p)
        {
            _hypeActive = true;
            _hypeUntilU = Time.unscaledTime + UnityEngine.Random.Range(hypeDurationRange.x, hypeDurationRange.y);
            _perfectsSinceLastHype = 0;

            // 瞬时小幅跃升（0.1%~0.3%）
            if (Current > 0)
            {
                float a = Mathf.Min(hypeInstantSurgePercent.x, hypeInstantSurgePercent.y) * 0.01f;
                float b = Mathf.Max(hypeInstantSurgePercent.x, hypeInstantSurgePercent.y) * 0.01f;
                int surge = Mathf.Max(1, Mathf.RoundToInt(Current * UnityEngine.Random.Range(a, b)));
                Current += surge;
                OnDeltaApplied?.Invoke(+surge);
                OnViewersChanged?.Invoke(Current);
            }
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
        float perfMul   = 1f + Mathf.Max(0f, _momentum) * 0.15f; // 给统一倍率一点动量影响，但很轻
        float progMul   = 1f; // 这里不再叠太多影响，避免系统间相互放大
        float hypeMul   = _hypeActive ? hypeRateMultiplier : 1f;
        return Mathf.Max(0f, viewerMul * perfMul * progMul * hypeMul);
    }

    // ===== 外部 API =====
    public void ResetToStart()
    {
        IsDepleted = false;
        Current = Mathf.Max(0, startViewers);

        _accTilt = 0f;
        _momentum = 0f;
        _hypeActive = false;
        _perfectsSinceLastHype = 0;
        _lastGoodOrPerfectTime = Time.unscaledTime;

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

    // —— 敌人碰撞：按百分比随机扣 —— //
    public void LoseRandomPercentInRange(Vector2 percentRange)
    {
        float a = Mathf.Min(percentRange.x, percentRange.y);
        float b = Mathf.Max(percentRange.x, percentRange.y);
        float pct = UnityEngine.Random.Range(a, b);
        LosePercent(pct);
    }

    public void GainPercent(float pct)
    {
        if (IsDepleted) return;
        if (pct <= 0f) return;

        // 给一个最小台阶，避免小规模时“增幅=0”
        int inc = DeltaFromPercent(pct, Current);
        if (inc <= 0) inc = 1;

        Current += inc;
        OnViewersChanged?.Invoke(Current);
        OnDeltaApplied?.Invoke(+inc);
    }

    public void LosePercent(float pct)
    {
        if (IsDepleted || Current <= 0) return;
        if (pct <= 0f) return;

        int dec = DeltaFromPercent(pct, Current);
        if (dec <= 0) dec = 1;

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

    // —— 兼容旧 API（建议迁移到百分比） —— //
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

    // ===== 工具：把百分比换算成整数台阶 =====
    static int DeltaFromPercent(float pct, int baseValue)
    {
        if (pct <= 0f || baseValue <= 0) return 0;
        return Mathf.FloorToInt(baseValue * (pct / 100f));
    }
}
