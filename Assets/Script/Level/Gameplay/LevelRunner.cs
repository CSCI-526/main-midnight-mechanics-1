using System.Collections.Generic;
using UnityEngine;

public class LevelRunner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private AudioSource   music;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private EnemySpawner  spawner;

    public System.Action OnLevelEnded;
    public System.Action OnLevelApplied;

    public LevelConfig Current { get; private set; }
    public float LevelDuration { get; private set; }
    public float ElapsedRealtime { get; private set; }
    public float Progress01 => LevelDuration > 0f ? Mathf.Clamp01(ElapsedRealtime / LevelDuration) : 0f;

    List<RhythmChart.ChartEvent> timeline;
    int   cursor;
    bool  running;

    float  lead;
    double chartNow;
    bool   usingAudioTime;
    float  audioDelayLeft;
    double audioSyncOffset;

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

        pattern.ResetForNewLevel();
        pattern.EnableChartMode(true);

        timeline = Current.chart.BuildTimeline();
        cursor   = 0;

        lead = Mathf.Max(0.01f, Current.chart.defaultLeadTimeSec);

        LevelDuration   = Mathf.Max(1f, Current.levelDurationSeconds);
        ElapsedRealtime = 0f;

        chartNow        = 0.0;
        usingAudioTime  = false;
        audioDelayLeft  = Mathf.Max(0f, Current.bgmDelaySec);
        audioSyncOffset = 0.0;

        if (music)
        {
            music.Stop();
            music.clip         = Current.bgm;
            music.playOnAwake  = false;
            music.loop         = false;
            music.spatialBlend = 0f;
        }

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

        ElapsedRealtime = Mathf.Min(ElapsedRealtime + Time.deltaTime, LevelDuration);

        if (music && !usingAudioTime)
        {
            if (audioDelayLeft > 0f)
            {
                audioDelayLeft -= Time.deltaTime;
                if (audioDelayLeft < 0f) audioDelayLeft = 0f;
            }
            else
            {
                if (music.clip && !music.isPlaying) music.Play();
                usingAudioTime  = true;
                audioSyncOffset = chartNow;
            }
        }

        if (usingAudioTime && music && music.clip && music.clip.frequency > 0)
        {
            chartNow = (double)music.timeSamples / music.clip.frequency + audioSyncOffset;
        }
        else
        {
            chartNow += Time.deltaTime;
        }

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

        pattern.SetChartNow(chartNow);

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
