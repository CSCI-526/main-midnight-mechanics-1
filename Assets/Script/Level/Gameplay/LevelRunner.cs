using System.Collections.Generic;
using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AudioSource   music;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private EnemySpawner  spawner;
    
    [Header("Clock (Sample-Time & Drift Correction)")]
    [SerializeField] private bool  useSampleClock = true;                 // 打开后启用“样本为王”
    [SerializeField, Range(0.0005f, 0.02f)]
    private float driftAlpha = 0.002f;                                    // LPF系数，越小越稳
    [SerializeField] private bool  debugLogDrift = false;
    [SerializeField, Min(0.05f)] private float debugLogIntervalSec = 0.5f;

    public System.Action OnLevelEnded;
    public System.Action OnLevelApplied;

    public LevelConfig Current { get; private set; }
    public float LevelDuration { get; private set; }
    public float ElapsedRealtime { get; private set; }
    public float Progress01 => LevelDuration > 0f ? Mathf.Clamp01(ElapsedRealtime / LevelDuration) : 0f;

    // —— 内部状态 ——
    List<RhythmChart.ChartEvent> timeline;
    int   cursor;
    bool  running;

    float  lead;                 // 从屏外到命中中心的飞行时间（取 chart.defaultLeadTimeSec）
    double chartStartDsp;        // 谱面时钟起点（DSP）
    double musicStartDsp;        // BGM 计划开始的 DSP 时刻（与 chart 对齐）
    bool   musicScheduled;

    // —— 漂移跟踪 ——
    double driftLPF;             // 低通后的漂移（秒）
    double nextDriftLogDsp;      // 下次打印的 dsp 时刻

    public void Apply(LevelConfig level)
    {
        CleanLevelState();

        Current = level;
        if (!Current || !Current.chart)
        {
            Debug.LogError("[LevelRunner] Level 或其 Chart 为空。");
            return;
        }
        if (!pattern) pattern = FindFirstObjectByType<PatternSystem>(FindObjectsInactive.Include);

        // Pattern 初始化
        pattern.ResetForNewLevel();
        pattern.EnableChartMode(true);

        // 生成时间轴（chart 的 tSec 已含 songOffsetSec）
        timeline = Current.chart.BuildTimeline();
        cursor   = 0;

        // —— 飞行时间（领跑） ——
        lead = Mathf.Max(0.01f, Current.chart.defaultLeadTimeSec);

        // 关卡时长（按配置）
        LevelDuration   = Mathf.Max(1f, Current.levelDurationSeconds);
        ElapsedRealtime = 0f;

        // —— DSP 预卷（lead 秒） ——
        double dspNow        = AudioSettings.dspTime;
        double extraSilence  = Mathf.Max(0f, Current.bgmDelaySec); // 如果你想比预卷再多一点静默
        chartStartDsp        = dspNow + lead + extraSilence;       // 让 chartNow 在音乐开播前是负的
        musicStartDsp        = chartStartDsp;                      // 音乐与 chart 零点对齐
        musicScheduled       = false;

        // —— 漂移复位 ——
        driftLPF        = 0.0;
        nextDriftLogDsp = dspNow + debugLogIntervalSec;

        if (music && Current.bgm)
        {
            music.Stop();
            music.clip         = Current.bgm;
            music.playOnAwake  = false;
            music.loop         = false;
            music.spatialBlend = 0f;
            music.PlayScheduled(musicStartDsp);                    // 样本级预约
            musicScheduled = true;
        }

        // 刷怪窗口按关卡配置
        if (spawner)
        {
            spawner.ApplyFromLevel(Current);
            spawner.ConfigureWindow(LevelDuration, Current.spawnStartDelay, Current.spawnStopEarly);
        }

        running = true;
        OnLevelApplied?.Invoke();
    }

    void Update()
    {
        if (!running) return;

        // 关卡实时间推进
        ElapsedRealtime = Mathf.Min(ElapsedRealtime + Time.deltaTime, LevelDuration);

        // —— 时钟 & 漂移测量 ——
        double dspNow   = AudioSettings.dspTime;
        double expected = dspNow - musicStartDsp; // 期望：按DSP推测“音乐该播到”哪里（可能为负）

        if (useSampleClock && music && music.clip && music.timeSamples > 0)
        {
            // 实际：样本时钟（音乐真实播到的地方）
            double actual   = (double)music.timeSamples / music.clip.frequency;
            double measured = expected - actual;                 // 正：DSP超前(音乐偏慢)

            // 慢速LPF，避免 jitter
            driftLPF += (measured - driftLPF) * driftAlpha;

            // 可选：节流打印
            if (debugLogDrift && dspNow >= nextDriftLogDsp)
            {
                double driftMs = driftLPF * 1000.0;
                Debug.Log($"[LevelRunner] driftLPF = {driftMs:F1} ms (alpha={driftAlpha})");
                nextDriftLogDsp = dspNow + debugLogIntervalSec;
            }
        }

        // —— 谱面时钟（用“DSP – 漂移LPF”稳定靠拢样本时钟）——
        double chartNow = (dspNow - chartStartDsp) - (useSampleClock ? driftLPF : 0.0);
        // 关键点：预卷阶段（expected<0）仍可得到负 chartNow；音乐开播后逐步校正到样本时钟

        // —— 投喂事件（Tap/Double）——
        while (cursor < (timeline?.Count ?? 0))
        {
            var ev = timeline[cursor];
            if (ev.tSec <= chartNow + lead)                        // 等价于 chartNow >= ev.tSec - lead
            {
                if (ev.kind == RhythmChart.NoteKind.Double)
                    pattern.EnqueueDouble(ev.tSec, lead);
                else
                    pattern.EnqueueTap(ev.tSec, lead);
                cursor++;
            }
            else break;
        }

        // 驱动 Pattern 的“现在时间”
        pattern.SetChartNow(chartNow);

        // —— 关卡结束（按 LevelDuration）——
        if (ElapsedRealtime >= LevelDuration)
        {
            running = false;
            OnLevelEnded?.Invoke();
        }
    }

    public void AbortLevel()
    {
        running = false;
        if (music) music.Stop();

        if (spawner) spawner.StopAndReset();
        Enemy.KillAll();
        var bullets = FindObjectsOfType<Bullet>();
        foreach (var b in bullets) if (b) Destroy(b.gameObject);

        if (pattern) pattern.ResetForNewLevel();
    }

    void CleanLevelState()
    {
        if (spawner) spawner.StopAndReset();
        Enemy.KillAll();
        var bullets = FindObjectsOfType<Bullet>();
        foreach (var b in bullets) if (b) Destroy(b.gameObject);
        if (pattern) pattern.ResetForNewLevel();
    }
}
