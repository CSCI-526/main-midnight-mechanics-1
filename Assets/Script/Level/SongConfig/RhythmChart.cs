using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Manual Chart", fileName = "NewRhythmChart")]
public class RhythmChart : ScriptableObject
{
    [Header("Tempo")]
    public float bpm = 120f;
    public int   beatsPerBar = 4;

    [Header("Timing Offset")]
    public double songOffsetSec = 0.0;      // 谱面整体偏移（微调）

    [Header("Note Flight")]
    public float defaultLeadTimeSec = 1.20f; // 从屏外到命中中心的飞行时间

    public enum NoteKind { Tap, Double }
    public enum SlotKind { Empty, Tap, Double }

    [System.Serializable]
    public struct ChartEvent
    {
        public NoteKind kind;
        public double   tSec;   // 命中时刻（秒）
    }

    [System.Serializable]
    public struct Bar
    {
        public int subdivision;             // 默认 8
        public List<SlotKind> slots;        // 长度 == subdivision
    }

    public List<Bar> bars = new List<Bar>();

    public double BeatToSeconds(double beat) => (60.0 / Mathf.Max(1f, bpm)) * beat;

    public List<ChartEvent> BuildTimeline()
    {
        var list = new List<ChartEvent>(256);

        for (int b = 0; b < bars.Count; b++)
        {
            var bar = bars[b];
            int sub = Mathf.Max(1, bar.subdivision);
            if (bar.slots == null || bar.slots.Count == 0) continue;

            int count = Mathf.Min(sub, bar.slots.Count);
            double barStartBeat = b * (double)beatsPerBar;
            double sliceBeatLen = (double)beatsPerBar / sub;

            for (int i = 0; i < count; i++)
            {
                var slot = bar.slots[i];
                if (slot == SlotKind.Empty) continue;

                double beat = barStartBeat + i * sliceBeatLen;
                double tSec = BeatToSeconds(beat) + songOffsetSec;

                list.Add(new ChartEvent {
                    kind = (slot == SlotKind.Tap) ? NoteKind.Tap : NoteKind.Double,
                    tSec = tSec
                });
            }
        }

        list.Sort((a, b) => a.tSec.CompareTo(b.tSec));
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
        var bar = new Bar { subdivision = 8, slots = new List<SlotKind>(8) };
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
