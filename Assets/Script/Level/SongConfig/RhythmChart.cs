using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Manual Chart", fileName = "NewRhythmChart")]
public class RhythmChart : ScriptableObject
{
    [Header("Tempo (Grid)")]
    [Min(1f)] public float bpm = 120f;
    [Min(1)]  public int   beatsPerBar = 4;

    [Header("Timing Offset (global)")]
    [Tooltip("整张谱面整体平移（秒）。>0=更晚；<0=更早；与BGM无关。")]
    public double songOffsetSec = 0.0;

    [Header("Note Flight (visual only)")]
    [Tooltip("从屏外到命中中心的可视飞行时间（秒），仅用于计算入场时机，不影响BGM。")]
    [Min(0.01f)] public float defaultLeadTimeSec = 1.20f;

    public enum NoteKind { Tap, Double }
    public enum SlotKind { Empty, Tap, Double, Burst }  // ★ 增加 Burst（用作B起点/终点标记）

    [System.Serializable]
    public struct ChartEvent { public NoteKind kind; public double tSec; }

    [System.Serializable]
    public struct Bar
    {
        [Tooltip("该小节被切成多少份（默认8）")]
        public int subdivision;

        [Tooltip("长度==subdivision；每格一个槽：Empty/Tap/Double/Burst（B 用来标记 Burst 起点/终点）")]
        public List<SlotKind> slots;

        [Tooltip("进入本小节前插入的延时（秒），正=推迟，负=提前；对本小节及之后累加生效")]
        public float preDelaySec;
    }

    [Tooltip("从第0小节开始配置。默认每小节8分。在格子上循环点击：Empty → Tap → Double → Burst(B)。Burst 成对出现定义一个连打区间，可跨小节。")]
    public List<Bar> bars = new List<Bar>();

    // 为避免命名元组在某些编译设置下丢字段，Burst 时间区间用结构体
    [System.Serializable]
    public struct BurstRange
    {
        public double startSec;
        public double endSec;
        public BurstRange(double s, double e) { startSec = s; endSec = e; }
    }

    // 拍 -> 秒
    public double BeatToSeconds(double beat) => (60.0 / Mathf.Max(1f, bpm)) * beat;

    /// 生成 Tap/Double 时间轴（忽略 Burst 格子）
    public List<ChartEvent> BuildTimeline()
    {
        var list = new List<ChartEvent>(256);
        if (bars == null || bars.Count == 0) return list;

        double secPerBeat     = 60.0 / Mathf.Max(1f, bpm);
        double barLenSecBase  = beatsPerBar * secPerBeat;
        double cumulativeDelay = 0.0;

        for (int b = 0; b < bars.Count; b++)
        {
            var bar = bars[b];
            int sub = Mathf.Max(1, bar.subdivision);

            cumulativeDelay += (double)bar.preDelaySec;
            double barStartSec = BeatToSeconds(b * (double)beatsPerBar) + songOffsetSec + cumulativeDelay;

            if (bar.slots == null || bar.slots.Count == 0) continue;
            int count = Mathf.Min(sub, bar.slots.Count);

            double sliceSec = barLenSecBase / sub;
            for (int i = 0; i < count; i++)
            {
                var slot = bar.slots[i];
                if (slot == SlotKind.Tap || slot == SlotKind.Double)
                {
                    double tSec = barStartSec + i * sliceSec;
                    list.Add(new ChartEvent {
                        kind = (slot == SlotKind.Tap) ? NoteKind.Tap : NoteKind.Double,
                        tSec = tSec
                    });
                }
            }
        }
        return list;
    }

    /// 生成 Burst 时间轴：扫描所有 B 格子（按时间顺序），两两配对为一个区间
    public List<BurstRange> BuildBurstTimeline()
    {
        var list = new List<BurstRange>();
        if (bars == null || bars.Count == 0) return list;

        // 预计算每个 bar 的起点（含 preDelay & 全局 offset）
        var barStartSec = new double[bars.Count];
        double cumulativeDelay = 0.0;
        for (int b = 0; b < bars.Count; b++)
        {
            cumulativeDelay += (double)bars[b].preDelaySec;
            barStartSec[b] = BeatToSeconds(b * (double)beatsPerBar) + songOffsetSec + cumulativeDelay;
        }

        double secPerBeat    = 60.0 / Mathf.Max(1f, bpm);
        double barLenBaseSec = beatsPerBar * secPerBeat;

        double SlotTime(int b, int i)
        {
            var bar = bars[Mathf.Clamp(b, 0, bars.Count - 1)];
            int sub = Mathf.Max(1, bar.subdivision);
            double slice = barLenBaseSec / sub;
            return barStartSec[b] + i * slice;
        }

        // 收集所有 B 标记（时间）
        var bTimes = new List<double>(32);
        for (int b = 0; b < bars.Count; b++)
        {
            var bar = bars[b];
            if (bar.slots == null) continue;
            int sub = Mathf.Max(1, bar.subdivision);
            int count = Mathf.Min(sub, bar.slots.Count);
            for (int i = 0; i < count; i++)
            {
                if (bar.slots[i] == SlotKind.Burst)
                    bTimes.Add(SlotTime(b, i));
            }
        }

        // 两两配对
        bTimes.Sort();
        for (int k = 0; k + 1 < bTimes.Count; k += 2)
        {
            double t0 = bTimes[k];
            double t1 = bTimes[k + 1];
            if (t1 < t0) { var tmp = t0; t0 = t1; t1 = tmp; }
            list.Add(new BurstRange(t0, t1));
        }
        return list;
    }

    void OnValidate()
    {
        if (bars == null) return;
        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            if (bar.subdivision <= 0) bar.subdivision = 8;
            if (bar.slots == null) bar.slots = new List<SlotKind>(bar.subdivision);

            // 同步长度
            if (bar.slots.Count < bar.subdivision)
            {
                int add = bar.subdivision - bar.slots.Count;
                for (int k = 0; k < add; k++) bar.slots.Add(SlotKind.Empty);
            }
            else if (bar.slots.Count > bar.subdivision)
            {
                bar.slots.RemoveRange(bar.subdivision, bar.slots.Count - bar.subdivision);
            }
            bars[i] = bar;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Add Empty Bar (x8)")]
    void AddEmptyBar8_Menu()
    {
        var bar = new Bar { subdivision = 8, slots = new List<SlotKind>(8), preDelaySec = 0f };
        for (int i = 0; i < 8; i++) bar.slots.Add(SlotKind.Empty);
        bars.Add(bar);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Clear Bars")]
    void ClearBars_Menu()
    {
        bars.Clear();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
