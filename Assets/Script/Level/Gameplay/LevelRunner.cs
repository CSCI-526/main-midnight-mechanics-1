using System.Collections.Generic;
using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AudioSource   music;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private EnemySpawner  spawner;

    [Header("Timing Adjust")]
    [SerializeField] private float  leadTimeSecOverride = -1f;
    [SerializeField] private double globalOffsetSec     = 0.0;

    public System.Action OnLevelEnded;
    public System.Action OnLevelApplied;

    public LevelConfig Current { get; private set; }
    public float LevelDuration { get; private set; }
    public float ElapsedRealtime { get; private set; }
    public float Progress01 => LevelDuration > 0f ? Mathf.Clamp01(ElapsedRealtime / LevelDuration) : 0f;

    RhythmChart chart;
    List<RhythmChart.ChartEvent> timeline;
    int   cursor;
    bool  running;
    float leadTimeSec;

    void Awake()
    {
        if (!pattern) pattern = FindFirstObjectByType<PatternSystem>(FindObjectsInactive.Include);
    }

    public void Apply(LevelConfig c)
    {
        CleanLevelState();

        Current = c;
        if (!pattern) { Debug.LogError("[LevelRunner] PatternSystem is null."); return; }
        if (!c || !c.chart) { Debug.LogError("[LevelRunner] LevelConfig.chart is null."); return; }

        chart = c.chart;
        pattern.ResetForNewLevel();
        pattern.EnableChartMode(true);

        timeline   = chart.BuildTimeline();
        cursor     = 0;
        leadTimeSec = (leadTimeSecOverride > 0f ? leadTimeSecOverride : chart.defaultLeadTimeSec);

        LevelDuration   = chart.GetLevelDurationSec();
        ElapsedRealtime = 0f;

        if (music)
        {
            music.Stop();
            music.playOnAwake  = false;
            music.loop         = false;
            music.spatialBlend = 0f;
            music.clip         = chart.clip;
            if (music.clip) music.Play();
        }

        if (spawner)
        {
            spawner.ConfigureWindow(LevelDuration, c.spawnStartDelay, c.spawnStopEarly);
            if (c.enemyPrefab) spawner.SetEnemyPrefab(c.enemyPrefab);
            if (c.spawnInterval > 0f) spawner.SetSpawnInterval(c.spawnInterval);
        }

        running = true;
        OnLevelApplied?.Invoke();
    }

    void Update()
    {
        if (!running) return;

        ElapsedRealtime = Mathf.Min(ElapsedRealtime + Time.deltaTime, LevelDuration);
        double now = GetSongTimeSec() + globalOffsetSec;

        while (cursor < (timeline?.Count ?? 0))
        {
            var ev = timeline[cursor];
            if (ev.tSec <= now + leadTimeSec)
            {
                if (ev.kind == RhythmChart.NoteKind.Tap)
                    pattern.EnqueueTap(ev.tSec, leadTimeSec);
                else
                    pattern.EnqueueDouble(ev.tSec, leadTimeSec);

                cursor++;
            }
            else break;
        }

        pattern.SetChartNow(now);

        if (now >= (double)LevelDuration)
        {
            running = false;
            OnLevelEnded?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.N))
            OnLevelEnded?.Invoke();
    }

    public void AbortLevel()
    {
        running = false;
        if (music) music.Stop();
        if (spawner) spawner.StopAndReset();
        Enemy.KillAll();
        var bullets = FindObjectsByType<Bullet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var b in bullets) if (b) Destroy(b.gameObject);
        if (pattern) pattern.ResetForNewLevel();
    }

    void CleanLevelState()
    {
        if (spawner) spawner.StopAndReset();
        Enemy.KillAll();
        var bullets = FindObjectsByType<Bullet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var b in bullets) if (b) Destroy(b.gameObject);
        if (pattern) pattern.ResetForNewLevel();
    }

    double GetSongTimeSec()
    {
        if (music && music.clip && music.clip.frequency > 0)
            return (double)music.timeSamples / music.clip.frequency;
        return Time.timeSinceLevelLoadAsDouble;
    }
}
