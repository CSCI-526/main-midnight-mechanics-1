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
    
    [Header("External Pause")]                     // ★ 新增
    [SerializeField] private bool externalPause;

    public System.Action OnLevelEnded;
    public System.Action OnLevelApplied;

    public LevelConfig Current { get; private set; }
    public float LevelDuration { get; private set; }
    public float ElapsedRealtime { get; private set; }
    public float Progress01 => LevelDuration > 0f ? Mathf.Clamp01(ElapsedRealtime / LevelDuration) : 0f;

    // —— 时间轴 —— 
    List<RhythmChart.ChartEvent> timeline;           // Tap / Double
    List<RhythmChart.BurstRange> burstTimeline;      // Burst 区间
    int cursor;
    int cursorBurst;

    // —— 运行时状态 —— 
    bool   running;
    float  lead;                 // 可视飞行时间
    double chartStartDsp;        // 谱面起点（DSP）
    double musicStartDsp;        // 音乐起点（DSP）

    // —— 漂移跟踪（用音乐采样钟做相位对齐）——
    double driftLPF;
    double nextDriftLogDsp;

    // —— 暂停补偿：把暂停的 DSP 时长扣掉，防止节拍跳跃 —— 
    bool   wasPaused;
    double pauseBeginDsp;
    double pausedAccumDsp;

    void OnEnable()
    {
        if (!viewers)    viewers    = FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
        if (!pattern)    pattern    = FindFirstObjectByType<PatternSystem>(FindObjectsInactive.Include);
        if (!spawner)    spawner    = FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (!gameOverUI) gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);

        if (viewers) viewers.OnDepleted += HandleGameOver;
        if (music && GlobalAudio.I && GlobalAudio.I.MusicGroup)
            music.outputAudioMixerGroup = GlobalAudio.I.MusicGroup;
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

        // ANALYTICS: Pass level name
        //if (pattern) pattern.SetCurrentLevelName(Current.levelName);

        // 生成时间轴（含 Burst）
        timeline      = Current.chart.BuildTimeline();
        burstTimeline = Current.chart.BuildBurstTimeline();
        cursor        = 0;
        cursorBurst   = 0;

        // 可视飞行时间
        lead = Mathf.Max(0.01f, Current.chart.defaultLeadTimeSec);

        // 关卡时长
        LevelDuration   = Mathf.Max(1f, Current.levelDurationSeconds);
        ElapsedRealtime = 0f;

        // —— DSP 预卷：lead + bgmDelaySec —— 
        double dspNow       = AudioSettings.dspTime;
        double extraSilence = Mathf.Max(0f, Current.bgmDelaySec);
        chartStartDsp       = dspNow + lead + extraSilence;
        musicStartDsp       = chartStartDsp;

        // —— 漂移复位 —— 
        driftLPF        = 0.0;
        nextDriftLogDsp = dspNow + debugLogIntervalSec;

        // —— 暂停补偿复位 —— 
        wasPaused      = false;
        pauseBeginDsp  = 0.0;
        pausedAccumDsp = 0.0;

        // 安排音乐
        if (music && Current.bgm)
        {
            music.Stop();
            music.clip         = Current.bgm;
            music.playOnAwake  = false;
            music.loop         = false;
            music.spatialBlend = 0f;
            music.PlayScheduled(musicStartDsp);
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
        
        if (externalPause) return;

        // —— 暂停冻结：完全停止推进时钟 & 投喂 —— 
        if (GamePause.IsPaused)
        {
            if (!wasPaused)
            {
                wasPaused     = true;
                pauseBeginDsp = AudioSettings.dspTime;
            }
            return;
        }
        else if (wasPaused)
        {
            // 恢复：把暂停期间的 DSP 时长记账，后续从时钟里扣掉
            pausedAccumDsp += AudioSettings.dspTime - pauseBeginDsp;
            wasPaused = false;
        }

        // 观众归零：双保险
        if (viewers && viewers.IsDepleted) return;

        // 实时间推进（用于关卡整体时长）
        ElapsedRealtime = Mathf.Min(ElapsedRealtime + Time.deltaTime, LevelDuration);

        // —— 时钟 & 漂移 —— 
        double dspNow   = AudioSettings.dspTime;
        double expected = (dspNow - musicStartDsp) - pausedAccumDsp;

        if (useSampleClock && music && music.clip && music.timeSamples > 0)
        {
            double actual   = (double)music.timeSamples / music.clip.frequency;
            double measured = expected - actual;   // 正：DSP 超前（音乐偏慢）
            driftLPF += (measured - driftLPF) * driftAlpha;

            if (debugLogDrift && dspNow >= nextDriftLogDsp)
            {
                double driftMs = driftLPF * 1000.0;
                Debug.Log($"[LevelRunner] driftLPF = {driftMs:F1} ms (alpha={driftAlpha})");
                nextDriftLogDsp = dspNow + debugLogIntervalSec;
            }
        }

        double chartNow = (dspNow - chartStartDsp - pausedAccumDsp) - (useSampleClock ? driftLPF : 0.0);

        // —— 投喂 Tap/Double —— 
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

        // —— 投喂 Burst —— 
        while (cursorBurst < (burstTimeline?.Count ?? 0))
        {
            var span = burstTimeline[cursorBurst];
            if (span.startSec <= chartNow + lead)
            {
                pattern.EnqueueBurst(span.startSec, span.endSec, lead);
                cursorBurst++;
            }
            else break;
        }

        // 驱动可视
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
    
    public void SetExternalPause(bool on)
    {
        externalPause = on;
        if (music)
        {
            if (on) music.Pause();
            else    music.UnPause();
        }
    }
}
