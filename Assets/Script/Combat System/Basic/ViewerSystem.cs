using System;
using UnityEngine;

public enum NoteJudgement { Perfect, Good, Miss }

/// <summary>
/// 统一管理“观众数”。支持：命中/触碰引起的变化；以及“环境漂移”（数字滚动 + 上升趋势）。
/// </summary>
public class ViewerSystem : MonoBehaviour
{
    [Header("Starting Viewers")]
    [SerializeField] private int startViewers = 1000;

    [Header("On Enemy Touch (global default)")]
    [Tooltip("敌人触碰玩家时掉粉区间（闭区间随机）")]
    [SerializeField] private Vector2Int touchLossRange = new Vector2Int(200, 250);

    [Header("Judgement Deltas")]
    [SerializeField] private int perfectGain = 20;
    [SerializeField] private int goodGain    = 10;
    [SerializeField] private int missLoss    = 10;

    // ===== Ambient Drift（动态滚动 + 上升趋势）=====
    [Header("Ambient Drift (auto jitter + tilt)")]
    [Tooltip("是否开启环境漂移（数字持续小幅涨跌 + 总体上升）")]
    [SerializeField] private bool ambientEnabled = true;

    [Tooltip("趋势：每分钟的期望增幅百分比（相对当前值）。例：5 = 每分钟约 +5%。")]
    [SerializeField, Range(0f, 100f)]
    private float ambientTiltPercentPerMinute = 5f;

    [Tooltip("抖动频率（次/秒）。越高越“转”，但也更耗性能。")]
    [SerializeField, Min(0.1f)] private float ambientJitterHz = 10f;

    [Tooltip("每次抖动的步长范围（含端点）。默认 0~3，后几位会不停跳动。")]
    [SerializeField] private Vector2Int ambientJitterStepRange = new Vector2Int(0, 3);

    [Tooltip("抖动是否也触发 OnDeltaApplied（HUD 的+20/-250弹字）。默认关闭，避免刷屏。")]
    [SerializeField] private bool ambientEmitDeltaEvents = false;

    [Tooltip("环境漂移是否允许把观众降到 0 并触发 GameOver。默认不允许（避免莫名其妙失败）。")]
    [SerializeField] private bool ambientCanDeplete = false;

    public int Current { get; private set; }
    public bool IsDepleted { get; private set; }
    public Vector2Int DefaultTouchLossRange => touchLossRange;

    /// <summary>观众数变化事件（当前值）</summary>
    public event Action<int> OnViewersChanged;
    /// <summary>本次变化的增量（用于HUD显示+20/-250等）</summary>
    public event Action<int> OnDeltaApplied;
    /// <summary>观众数归零（游戏结束）</summary>
    public event Action OnDepleted;

    // Ambient内部计时
    float _jitterTimer;
    float _driftAccumulator;

    void Awake() => ResetToStart();

    void Update()
    {
        if (!ambientEnabled) return;
        if (IsDepleted) return;              // GameOver后不再漂移
        if (Time.timeScale == 0f) return;    // 暂停时不漂移

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // === 1) 趋势：每分钟 +X%（按当前值） ===
        // 期望每秒增长： Current * (tilt% / 60)
        float perSec = Current * (ambientTiltPercentPerMinute / 100f) / 60f;
        _driftAccumulator += perSec * dt;
        if (_driftAccumulator >= 1f)
        {
            int inc = Mathf.FloorToInt(_driftAccumulator); // 累积到1就加1（可能一次加多）
            _driftAccumulator -= inc;
            ApplyAmbientDelta(+inc);
        }

        // === 2) 抖动：对称的小幅上下跳动（均值为0） ===
        _jitterTimer += dt;
        float period = 1f / ambientJitterHz;
        while (_jitterTimer >= period)
        {
            _jitterTimer -= period;

            int magMin = Mathf.Min(ambientJitterStepRange.x, ambientJitterStepRange.y);
            int magMax = Mathf.Max(ambientJitterStepRange.x, ambientJitterStepRange.y);
            int mag    = UnityEngine.Random.Range(magMin, magMax + 1);
            if (mag == 0) continue;

            int sign = (UnityEngine.Random.value < 0.5f) ? -1 : +1;
            int delta = sign * mag;

            // 环境抖动默认不允许把观众数降到0
            if (!ambientCanDeplete && (Current + delta) <= 0)
            {
                // 保底到1
                delta = -(Current - 1);
                if (delta == 0) continue;
            }

            ApplyAmbientDelta(delta);
        }
    }

    // ===== 外部 API =====
    public void ResetToStart()
    {
        IsDepleted = false;
        Current = Mathf.Max(0, startViewers);
        _driftAccumulator = 0f;
        _jitterTimer = 0f;
        RaiseChanged(0);
    }

    public void ApplyJudgement(NoteJudgement j)
    {
        switch (j)
        {
            case NoteJudgement.Perfect: Gain(perfectGain); break;
            case NoteJudgement.Good:    Gain(goodGain);    break;
            case NoteJudgement.Miss:    Lose(missLoss);    break;
        }
    }

    public void LoseRandomInRange(Vector2Int range)
    {
        int a = Mathf.Min(range.x, range.y);
        int b = Mathf.Max(range.x, range.y);
        int dmg = UnityEngine.Random.Range(a, b + 1); // 含上界
        Lose(dmg);
    }

    public void Gain(int amount)
    {
        if (IsDepleted) return;
        int inc = Mathf.Max(0, amount);
        if (inc == 0) return;
        Current += inc;
        RaiseChanged(+inc);
    }

    public void Lose(int amount)
    {
        if (IsDepleted) return;
        int dec = Mathf.Max(0, amount);
        if (dec == 0) return;

        int before = Current;
        Current = Mathf.Max(0, Current - dec);
        RaiseChanged(-(before - Current));

        if (Current <= 0 && !IsDepleted)
        {
            IsDepleted = true;
            Debug.LogWarning("[Viewers] Depleted → GAME OVER");
            OnDepleted?.Invoke();
        }
    }

    // ===== 内部工具 =====
    void ApplyAmbientDelta(int delta)
    {
        if (delta == 0) return;

        int before = Current;
        int after  = before + delta;

        if (!ambientCanDeplete && after <= 0)
            after = 1; // 不允许抖动把观众减到0

        after = Mathf.Max(0, after);
        if (after == before) return;

        Current = after;

        // 环境漂移：始终刷新数值，但默认不发弹字（可开关）
        OnViewersChanged?.Invoke(Current);
        if (ambientEmitDeltaEvents) OnDeltaApplied?.Invoke(delta);

        // 环境漂移也能触发死亡？默认不触发；如需可打开 ambientCanDeplete
        if (ambientCanDeplete && Current <= 0 && !IsDepleted)
        {
            IsDepleted = true;
            Debug.LogWarning("[Viewers] Depleted by Ambient → GAME OVER");
            OnDepleted?.Invoke();
        }
    }

    void RaiseChanged(int delta)
    {
        OnViewersChanged?.Invoke(Current);
        if (delta != 0) OnDeltaApplied?.Invoke(delta);
    }
}
