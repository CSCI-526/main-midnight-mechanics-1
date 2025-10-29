using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Manual Chart", fileName = "NewRhythmChart")]
public class RhythmChart : ScriptableObject
{
    [Header("Tempo (Grid)")]
    [Min(1f)] public float bpm = 120f;
    [Min(1)]  public int   beatsPerBar = 4;

    [Header("Timing Offset (global)")]
    [Tooltip("整张谱面整体平移（秒）。>0=更晚，<0=更早；与BGM无关。")]
    public double songOffsetSec = 0.0;

    [Header("Note Flight (visual only)")]
    [Tooltip("从屏外到命中中心的可视飞行时间（秒），仅用于计算入场时机，不影响BGM。")]
    [Min(0.01f)] public float defaultLeadTimeSec = 1.20f;

    public enum NoteKind { Tap, Double }
    public enum SlotKind { Empty, Tap, Double }

    [System.Serializable]
    public struct ChartEvent { public NoteKind kind; public double tSec; }

    [System.Serializable]
    public struct Bar
    {
        [Tooltip("该小节被切成多少份（默认8）")]
        public int subdivision;
        [Tooltip("长度==subdivision；每格一个槽（Empty/Tap/Double）")]
        public List<SlotKind> slots;

        // ✅ 关键：进入本小节前插入的延时（累加到之后所有小节）
        [Tooltip("进入本小节前插入的延时（秒），正=推迟，负=提前；对本小节及之后累加生效")]
        public float preDelaySec;
    }

    [Tooltip("从第0小节开始配置。建议默认每小节8分，然后直接点槽位：Empty / Tap / Double。")]
    public List<Bar> bars = new List<Bar>();

    // 拍 -> 秒
    public double BeatToSeconds(double beat) => (60.0 / Mathf.Max(1f, bpm)) * beat;

    /// 生成时间轴（固定 BPM + 全局 offset + 每小节累加的 preDelay）
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

            // 进入本小节前，先把该小节的延时累加进去（影响本小节及之后）
            cumulativeDelay += (double)bar.preDelaySec;

            // 本小节起点（秒）
            double barStartSec = BeatToSeconds(b * (double)beatsPerBar) + songOffsetSec + cumulativeDelay;

            if (bar.slots == null || bar.slots.Count == 0) continue;

            int count = Mathf.Min(sub, bar.slots.Count);
            double sliceSec = barLenSecBase / sub;

            for (int i = 0; i < count; i++)
            {
                var slot = bar.slots[i];
                if (slot == SlotKind.Empty) continue;

                double tSec = barStartSec + i * sliceSec;
                list.Add(new ChartEvent {
                    kind = (slot == SlotKind.Tap) ? NoteKind.Tap : NoteKind.Double,
                    tSec = tSec
                });
            }
        }
        // 已按时间顺序；可按需再 sort
        // list.Sort((a,b)=>a.tSec.CompareTo(b.tSec));
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
