#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RhythmChart))]
public class RhythmChartEditor : Editor
{
    // 尺寸常量（纯数值，不会触发 EditorStyles）
    private const float RowPadV = 4f;
    private const float ColGap  = 6f;
    private const float SmallBtnW = 36f;
    private const float IconBtnW  = 22f;
    private const float SubLblW   = 30f; // "Sub"
    private const float PreLblW   = 66f; // "Delay ms"
    private const float IntW      = 50f; // Subdivision 宽
    private const float MsW       = 70f; // preDelay 宽
    private const float BarTagW   = 58f; // "Bar N"

    public override void OnInspectorGUI()
    {
        var chart = (RhythmChart)target;

        // 在这里（GUI期间）安全获取样式
        var mini = EditorStyles.miniLabel;

        // ===== 顶部参数 =====
        EditorGUILayout.LabelField("Tempo (Grid)", EditorStyles.boldLabel);
        chart.bpm         = Mathf.Max(1f, EditorGUILayout.FloatField("BPM", chart.bpm));
        chart.beatsPerBar = Mathf.Max(1,  EditorGUILayout.IntField  ("Beats Per Bar", chart.beatsPerBar));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Timing Offset (global)", EditorStyles.boldLabel);
        chart.songOffsetSec = EditorGUILayout.DoubleField(new GUIContent("Song Offset (sec)", "正=整体更晚；负=整体更早"), chart.songOffsetSec);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Note Flight (visual)", EditorStyles.boldLabel);
        chart.defaultLeadTimeSec = Mathf.Max(0.01f, EditorGUILayout.FloatField("Default Lead (sec)", chart.defaultLeadTimeSec));

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Bars", EditorStyles.boldLabel);

        // 顶部操作
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Bar (x8)", GUILayout.Width(120))) AddBar(chart, 8);
        GUILayout.Space(6);
        if (GUILayout.Button("Remove Last", GUILayout.Width(100)))
        {
            if (chart.bars != null && chart.bars.Count > 0)
            {
                Undo.RecordObject(chart, "Remove Last Bar");
                chart.bars.RemoveAt(chart.bars.Count - 1);
                EditorUtility.SetDirty(chart);
            }
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear All", GUILayout.Width(90)))
        {
            if (EditorUtility.DisplayDialog("Clear Bars", "Clear all bars?", "Yes", "No"))
            {
                Undo.RecordObject(chart, "Clear Bars");
                chart.bars.Clear();
                EditorUtility.SetDirty(chart);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // ===== Bars 列表 =====
        if (chart.bars != null)
        {
            for (int b = 0; b < chart.bars.Count; b++)
            {
                var bar = chart.bars[b];

                EditorGUILayout.BeginVertical("box");
                GUILayout.Space(RowPadV);

                // —— 第一行：紧凑参数（Bar标签 / Sub / preDelay / 快调 / 删除）——
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label($"Bar {b}", GUILayout.Width(BarTagW));

                GUILayout.Space(ColGap);
                GUILayout.Label("Sub", mini, GUILayout.Width(SubLblW));
                int newSub = Mathf.Max(1, EditorGUILayout.IntField(bar.subdivision, GUILayout.Width(IntW)));
                if (newSub != bar.subdivision)
                {
                    Undo.RecordObject(chart, "Change Subdivision");
                    bar.subdivision = newSub;
                    EnsureLength(ref bar);
                }

                GUILayout.Space(ColGap);
                GUILayout.Label("Delay ms", mini, GUILayout.Width(PreLblW));
                float preDelayMs = bar.preDelaySec * 1000f;
                preDelayMs = EditorGUILayout.FloatField(preDelayMs, GUILayout.Width(MsW));
                bar.preDelaySec = preDelayMs / 1000f;

                GUILayout.Space(ColGap);
                if (GUILayout.Button("-10", GUILayout.Width(SmallBtnW))) bar.preDelaySec -= 0.010f;
                if (GUILayout.Button("+10", GUILayout.Width(SmallBtnW))) bar.preDelaySec += 0.010f;

                GUILayout.FlexibleSpace();
                if (DrawIconButton("TreeEditor.Trash", "Delete this bar", IconBtnW))
                {
                    if (EditorUtility.DisplayDialog("Delete Bar", $"Delete Bar {b}?", "Delete", "Cancel"))
                    {
                        Undo.RecordObject(chart, "Delete Bar");
                        chart.bars.RemoveAt(b);
                        EditorUtility.SetDirty(chart);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break; // 列表改变，跳出循环以避免索引错位
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(RowPadV);

                // —— 第二行：槽位按钮 —— //
                EnsureLength(ref bar);
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < bar.slots.Count; i++)
                {
                    var s = bar.slots[i];
                    string label = s == RhythmChart.SlotKind.Empty ? "·" : (s == RhythmChart.SlotKind.Tap ? "T" : "D");
                    if (GUILayout.Button(new GUIContent(label, "Click to cycle: Empty → Tap → Double"), GUILayout.Width(26)))
                    {
                        Undo.RecordObject(chart, "Edit Slot");
                        bar.slots[i] = Cycle(s);
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(RowPadV);
                EditorGUILayout.EndVertical();

                // 写回
                chart.bars[b] = bar;
            }
        }

        if (GUI.changed) EditorUtility.SetDirty(chart);
    }

    // —— 工具函数 —— //
    private static RhythmChart.SlotKind Cycle(RhythmChart.SlotKind s)
    {
        if (s == RhythmChart.SlotKind.Empty) return RhythmChart.SlotKind.Tap;
        if (s == RhythmChart.SlotKind.Tap)   return RhythmChart.SlotKind.Double;
        return RhythmChart.SlotKind.Empty;
    }

    private static void AddBar(RhythmChart chart, int subdivision)
    {
        Undo.RecordObject(chart, "Add Bar");
        var bar = new RhythmChart.Bar
        {
            subdivision = Mathf.Max(1, subdivision),
            slots       = new System.Collections.Generic.List<RhythmChart.SlotKind>(subdivision),
            preDelaySec = 0f
        };
        for (int i = 0; i < subdivision; i++) bar.slots.Add(RhythmChart.SlotKind.Empty);
        chart.bars.Add(bar);
        EditorUtility.SetDirty(chart);
    }

    private static void EnsureLength(ref RhythmChart.Bar bar)
    {
        if (bar.slots == null) bar.slots = new System.Collections.Generic.List<RhythmChart.SlotKind>(bar.subdivision);
        if (bar.slots.Count < bar.subdivision)
        {
            int add = bar.subdivision - bar.slots.Count;
            for (int k = 0; k < add; k++) bar.slots.Add(RhythmChart.SlotKind.Empty);
        }
        else if (bar.slots.Count > bar.subdivision)
        {
            bar.slots.RemoveRange(bar.subdivision, bar.slots.Count - bar.subdivision);
        }
    }

    private static bool DrawIconButton(string iconName, string tooltip, float width)
    {
        var c = EditorGUIUtility.IconContent(iconName);
        if (c == null || c.image == null)
            return GUILayout.Button(new GUIContent("X", tooltip), GUILayout.Width(width));
        return GUILayout.Button(new GUIContent(c.image, tooltip),
                                GUILayout.Width(width),
                                GUILayout.Height(EditorGUIUtility.singleLineHeight + 2));
    }
}
#endif
