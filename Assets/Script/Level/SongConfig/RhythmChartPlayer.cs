using System.Collections.Generic;
using UnityEngine;

public class RhythmChartPlayer : MonoBehaviour
{
    [Header("Refs")]
    public RhythmChart chart;
    public AudioSource music;             // 可空：只用游戏时钟
    public PatternSystem pattern;
    public LevelRunner levelRunner;       // 可空：用于收尾

    [Header("Playback")]
    public bool autoPlayMusic = true;
    public bool endLevelWhenSongEnds = true;

    [Header("Scheduling")]
    [Tooltip("若 < 0 则使用 chart.defaultLeadTimeSec")]
    public float leadTimeSecOverride = -1f;

    [Header("Latency")]
    [Tooltip("整体偏移(秒)，>0=更晚，<0=更早")]
    public double globalOffsetSec = 0.0;

    List<RhythmChart.ChartEvent> timeline;
    int cursor;
    bool running;
    float lead;
    double levelEndTime;

    void Awake()
    {
        if (!pattern) pattern = FindFirstObjectByType<PatternSystem>(FindObjectsInactive.Include);
        if (pattern) pattern.EnableChartMode(true);
    }

    void Start()
    {
        if (!chart || !pattern) { running = false; return; }

        timeline = chart.BuildTimeline();
        cursor   = 0;
        lead     = (leadTimeSecOverride > 0f ? leadTimeSecOverride : chart.defaultLeadTimeSec);
        levelEndTime = chart.GetLevelDurationSec();

        if (autoPlayMusic && chart.clip && music)
        {
            music.Stop();
            music.clip = chart.clip;
            music.time = 0f;
            music.Play();
        }
        running = true;
    }

    void Update()
    {
        if (!running) return;

        double now = GetSongTimeSec() + globalOffsetSec;

        // 投喂事件（仅 Tap / Double）
        while (cursor < (timeline?.Count ?? 0))
        {
            var ev = timeline[cursor];
            if (ev.tSec <= now + lead)
            {
                if (ev.kind == RhythmChart.NoteKind.Tap)
                    pattern.EnqueueTap(ev.tSec, lead);
                else // Double
                    pattern.EnqueueDouble(ev.tSec, lead);

                cursor++;
            }
            else break;
        }

        pattern.SetChartNow(now);

        if (endLevelWhenSongEnds && now >= levelEndTime)
        {
            running = false;
            if (levelRunner) levelRunner.OnLevelEnded?.Invoke();
        }
    }

    double GetSongTimeSec()
    {
        if (music && music.clip && music.clip.frequency > 0)
            return (double)music.timeSamples / music.clip.frequency;
        return Time.timeSinceLevelLoadAsDouble;
    }
}
