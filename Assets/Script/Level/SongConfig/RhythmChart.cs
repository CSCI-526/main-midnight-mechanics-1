using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Manual Chart", fileName = "NewRhythmChart")]
public class RhythmChart : ScriptableObject
{
    [Header("Song")]
    public AudioClip clip;
    public float bpm = 120f;
    public int   beatsPerBar = 4;        // 4/4 -> 4
    public double songOffsetSec = 0.0;   // 整体偏移

    [Header("Defaults")]
    public float defaultLeadTimeSec = 1.20f; // 从屏外到命中中心的默认飞行时间

    [Header("Level Duration")]
    public bool  useClipLengthAsLevelDuration = true;
    public float manualLevelDurationSec = 180f;

    // —— 事件类型（供运行时使用） ——
    public enum NoteKind { Tap, Double }

    // —— 编辑槽位（Empty / 单击 / 双击） ——
    public enum SlotKind { Empty, Tap, Double }

    [System.Serializable]
    public struct ChartEvent
    {
        public NoteKind kind;
        public double   tSec;  // 命中时刻（秒）
    }

    [System.Serializable]
    public struct Bar
    {
        public int subdivision;           // 切分份数（默认 8）
        public List<SlotKind> slots;      // 长度 == subdivision，每格 Empty/Tap/Double
    }

    public List<Bar> bars = new List<Bar>();

    // —— 工具 —— 
    public double BeatToSeconds(double beat) => (60.0 / Mathf.Max(1f, bpm)) * beat;

    public float GetLevelDurationSec()
    {
        if (useClipLengthAsLevelDuration && clip) return clip.length;
        return Mathf.Max(1f, manualLevelDurationSec);
    }

    // —— 核心：把 Bar/Slot 转成时间轴事件 ——
    public List<ChartEvent> BuildTimeline()
    {
        var list = new List<ChartEvent>(256);

        for (int b = 0; b < bars.Count; b++)
        {
            var bar = bars[b];
            int sub = Mathf.Max(1, bar.subdivision);
            if (bar.slots == null || bar.slots.Count == 0) continue;

            // 防御：长度不一致也按当前有效长度处理
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

    // —— 自动维护：新增 Bar 默认 subdivision = 8；slots 与 subdivision 同步 —— 
    void OnValidate()
    {
        if (bars == null) return;

        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];

            // 新 bar 或非法值 -> 默认 8
            if (bar.subdivision <= 0) bar.subdivision = 8;

            if (bar.slots == null) bar.slots = new List<SlotKind>(bar.subdivision);

            // 同步长度：增 -> 填 Empty；减 -> 截断
            if (bar.slots.Count < bar.subdivision)
            {
                int add = bar.subdivision - bar.slots.Count;
                for (int k = 0; k < add; k++) bar.slots.Add(SlotKind.Empty);
            }
            else if (bar.slots.Count > bar.subdivision)
            {
                bar.slots.RemoveRange(bar.subdivision, bar.slots.Count - bar.subdivision);
            }

            bars[i] = bar; // 结构体回写
        }
    }

#if UNITY_EDITOR
    // 右键菜单：快速加一个空白 8 分小节
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
