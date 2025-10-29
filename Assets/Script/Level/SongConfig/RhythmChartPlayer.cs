using System.Collections.Generic;
using UnityEngine;

public class RhythmChartPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmChart   chart;
    [SerializeField] private PatternSystem pattern;
    [SerializeField] private AudioSource   music;     // 仅播放BGM；计时走DSP

    [Header("Scheduling")]
    [Tooltip("若 < 0 则使用 chart.defaultLeadTimeSec")]
    [SerializeField] private float leadTimeSecOverride = -1f;

    [Header("Music Delay")]
    [SerializeField] private float musicDelaySec = 0f;
    [SerializeField] private bool  autoPlayMusic = true;

    [Header("End Condition")]
    [SerializeField] private float tailSeconds = 1.0f;
    [SerializeField] private bool  stopMusicOnEnd = false;

    public System.Action OnPlaybackEnded;

    List<RhythmChart.ChartEvent> timeline;
    int    cursor;
    bool   running;

    float  lead;                // 可视飞行时间
    double chartStartDsp;       // 谱面起点（DSP）
    double musicStartDsp;       // BGM 起播（DSP）
    double lastEventTime;       // 最后一颗音符命中时刻（tSec）

    void Awake()
    {
        if (!pattern) pattern = FindFirstObjectByType<PatternSystem>(FindObjectsInactive.Include);
        if (pattern) pattern.EnableChartMode(true);
    }

    void Start()
    {
        if (!chart || !pattern)
        {
            running = false;
            Debug.LogError("[RhythmChartPlayer] Missing chart or pattern.", this);
            return;
        }

        timeline = chart.BuildTimeline();
        cursor   = 0;
        lead     = (leadTimeSecOverride > 0f ? leadTimeSecOverride : chart.defaultLeadTimeSec);

        lastEventTime = (timeline.Count > 0) ? timeline[timeline.Count - 1].tSec : 0.0;

        // DSP 对齐
        double dspNow   = AudioSettings.dspTime;
        chartStartDsp   = dspNow;
        musicStartDsp   = chartStartDsp + Mathf.Max(0f, musicDelaySec);

        if (autoPlayMusic && music && music.clip)
        {
            music.Stop();
            music.PlayScheduled(musicStartDsp);
        }

        pattern.ResetForNewLevel();
        running = true;
    }

    void Update()
    {
        if (!running) return;

        // 谱面时钟（DSP）
        double chartNow = AudioSettings.dspTime - chartStartDsp;
        if (chartNow < 0.0) chartNow = 0.0;

        // 投喂事件
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

        // 驱动可视
        pattern.SetChartNow(chartNow);

        // 结束：所有事件投喂完 + 尾巴
        if (cursor >= (timeline?.Count ?? 0) && chartNow >= (lastEventTime + tailSeconds))
        {
            running = false;
            if (stopMusicOnEnd && music) music.Stop();
            OnPlaybackEnded?.Invoke();
        }
    }

    public void StopPlayback()
    {
        running = false;
        if (stopMusicOnEnd && music) music.Stop();
    }

    public void Restart()
    {
        Start(); // 简易重启
    }
}
