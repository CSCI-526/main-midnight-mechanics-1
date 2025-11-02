// LevelRunner.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class LevelRunner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AudioSource   music;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private EnemySpawner  spawner;

    [Header("Game Over")]
    [SerializeField] private ViewerSystem viewers;
    [SerializeField] private GameOverUI   gameOverUI;
    [Tooltip("返回关卡选择时要加载的场景名；留空则只 AbortLevel()")]
    [SerializeField] private string levelSelectSceneName = "";

    [Header("Clock (Sample-Time & Drift Correction)")]
    [SerializeField] private bool  useSampleClock = true;
    [SerializeField, Range(0.0005f, 0.02f)] private float driftAlpha = 0.002f;
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

    float  lead;
    double chartStartDsp;
    double musicStartDsp;
    bool   musicScheduled;

    // —— 漂移跟踪 ——
    double driftLPF;
    double nextDriftLogDsp;

    void OnEnable()
    {
        if (!viewers) viewers = FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
        if (!pattern) pattern = FindFirstObjectByType<PatternSystem>(FindObjectsInactive.Include);
        if (!spawner) spawner = FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (!gameOverUI) gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);

        if (viewers) viewers.OnDepleted += HandleGameOver;
    }

    void OnDisable()
    {
        if (viewers) viewers.OnDepleted -= HandleGameOver;
    }

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

        // 生成时间轴
        timeline = Current.chart.BuildTimeline();
        cursor   = 0;

        // 领跑时间
        lead = Mathf.Max(0.01f, Current.chart.defaultLeadTimeSec);

        // 关卡时长
        LevelDuration   = Mathf.Max(1f, Current.levelDurationSeconds);
        ElapsedRealtime = 0f;

        // —— DSP 预卷（lead 秒） ——
        double dspNow        = AudioSettings.dspTime;
        double extraSilence  = Mathf.Max(0f, Current.bgmDelaySec);
        chartStartDsp        = dspNow + lead + extraSilence;
        musicStartDsp        = chartStartDsp;
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
            music.PlayScheduled(musicStartDsp);
            musicScheduled = true;
        }

        // 刷怪窗口
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

        // 观众归零：双保险
        if (viewers && viewers.IsDepleted) return;

        // 实时间推进
        ElapsedRealtime = Mathf.Min(ElapsedRealtime + Time.deltaTime, LevelDuration);

        // —— 时钟 & 漂移测量 ——
        double dspNow   = AudioSettings.dspTime;
        double expected = dspNow - musicStartDsp;

        if (useSampleClock && music && music.clip && music.timeSamples > 0)
        {
            double actual   = (double)music.timeSamples / music.clip.frequency;
            double measured = expected - actual; // 正：DSP超前(音乐偏慢)
            driftLPF += (measured - driftLPF) * driftAlpha;

            if (debugLogDrift && dspNow >= nextDriftLogDsp)
            {
                double driftMs = driftLPF * 1000.0;
                Debug.Log($"[LevelRunner] driftLPF = {driftMs:F1} ms (alpha={driftAlpha})");
                nextDriftLogDsp = dspNow + debugLogIntervalSec;
            }
        }

        double chartNow = (dspNow - chartStartDsp) - (useSampleClock ? driftLPF : 0.0);

        // —— 投喂事件 ——
        while (cursor < (timeline?.Count ?? 0))
        {
            var ev = timeline[cursor];
            if (ev.tSec <= chartNow + lead)
            {
                if (ev.kind == RhythmChart.NoteKind.Double)
                    pattern.EnqueueDouble(ev.tSec, lead);
                else
                    pattern.EnqueueTap(ev.tSec, lead);
                cursor++;
            }
            else break;
        }

        // 驱动 Pattern
        pattern.SetChartNow(chartNow);

        // —— 关卡自然结束 ——
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
        // ★ 改为按基类统一清理所有弹体
        var projs = FindObjectsByType<ProjectileBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < projs.Length; i++) if (projs[i]) Destroy(projs[i].gameObject);

        if (pattern) pattern.ResetForNewLevel();
    }

    void CleanLevelState()
    {
        if (spawner) spawner.StopAndReset();
        Enemy.KillAll();
        var projs = FindObjectsByType<ProjectileBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < projs.Length; i++) if (projs[i]) Destroy(projs[i].gameObject);
        if (pattern) pattern.ResetForNewLevel();
    }

    // —— 观众归零 → GameOver 流程 ——
    void HandleGameOver()
    {
        if (!running) return;
        running = false;

        if (music) music.Stop();
        if (spawner) spawner.StopAndReset();
        if (pattern) pattern.EnableChartMode(false);

        Enemy.KillAll();
        var projs = FindObjectsByType<ProjectileBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < projs.Length; i++) if (projs[i]) Destroy(projs[i].gameObject);

        if (gameOverUI)
        {
            gameOverUI.Show(
                onBackToSelect: () =>
                {
                    Time.timeScale = 1f;
                    if (!string.IsNullOrEmpty(levelSelectSceneName))
                        SceneManager.LoadScene(levelSelectSceneName);
                    else
                        AbortLevel();
                },
                onRetry: () =>
                {
                    Time.timeScale = 1f;
                    AbortLevel();
                    if (viewers) viewers.ResetToStart();
                    Apply(Current);
                }
            );
        }
        else
        {
            Debug.LogWarning("[LevelRunner] GameOverUI 未设置：将仅执行清场，不显示面板。");
        }
    }
}
