using System.Collections.Generic;
using UnityEngine;

public class RhythmChartPlayer : MonoBehaviour
{
    [Header("Refs")]
    public LevelConfig level;       // 从这里拿 chart / bgm / duration / bgmDelay
    public AudioSource music;       // 可空：则用游戏时钟
    public PatternSystem pattern;
    public LevelRunner levelRunner; // 可空：用于收尾回调

    [Header("Playback")]
    public bool autoPlayMusic = true;
    public bool endLevelWhenSongEnds = true;

    [Header("Scheduling")]
    [Tooltip("若 < 0 则使用 level.chart.defaultLeadTimeSec")]
    public float leadTimeSecOverride = -1f;

    [Header("Latency")]
    [Tooltip("整体偏移(秒)，>0=更晚，<0=更早")]
    public double globalOffsetSec = 0.0;

    List<RhythmChart.ChartEvent> timeline;
    RhythmChart chart;
    int cursor;
    bool running;
    float lead;
    double levelEndTime;

    void Awake()
    {
#if UNITY_6000_0_OR_NEWER
        if (!pattern) pattern = FindFirstObjectByType<PatternSystem>(FindObjectsInactive.Include);
#else
        if (!pattern) pattern = FindObjectOfType<PatternSystem>();
#endif
        if (pattern) pattern.EnableChartMode(true);
    }

    void Start()
    {
        if (!level || !level.chart || !pattern) { running = false; return; }

        chart       = level.chart;
        timeline    = chart.BuildTimeline();
        cursor      = 0;
        lead        = (leadTimeSecOverride > 0f ? leadTimeSecOverride : chart.defaultLeadTimeSec);
        levelEndTime= Mathf.Max(1f, level.levelDurationSeconds);

        if (autoPlayMusic && level.bgm && music)
        {
            music.Stop();
            music.clip = level.bgm;
            music.time = 0f;
            // 仅延迟音乐，不改变谱面推进
            music.PlayDelayed(Mathf.Max(0f, level.bgmDelaySec));
        }

        running = true;
    }

    void Update()
    {
        if (!running) return;

        double now = GetSongTimeSec() + globalOffsetSec;

        // 投喂事件
        while (cursor < (timeline?.Count ?? 0))
        {
            var ev = timeline[cursor];
            if (ev.tSec <= now + lead)
            {
                if (ev.kind == RhythmChart.NoteKind.Tap)
                    pattern.EnqueueTap(ev.tSec, lead);
                else
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
        // 有音乐就用音频时钟；PlayDelayed 不影响 timeSamples 起点
        if (music && music.clip && music.clip.frequency > 0)
            return (double)music.timeSamples / music.clip.frequency;

        // 没有音乐就用游戏时钟（仅测试）
        return Time.timeSinceLevelLoadAsDouble;
    }
}
