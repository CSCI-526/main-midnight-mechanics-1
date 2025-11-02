#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RhythmChart))]
public class RhythmChartEditor : Editor
{
    private const float RowPadV   = 4f;
    private const float ColGap    = 6f;
    private const float SmallBtnW = 36f;
    private const float IconBtnW  = 22f;
    private const float SubLblW   = 30f;
    private const float PreLblW   = 66f;
    private const float IntW      = 50f;
    private const float MsW       = 70f;
    private const float BarTagW   = 58f;

    public override void OnInspectorGUI()
    {
        var chart = (RhythmChart)target;
        var mini = EditorStyles.miniLabel;

        // Tempo
        EditorGUILayout.LabelField("Tempo (Grid)", EditorStyles.boldLabel);
        chart.bpm         = Mathf.Max(1f, EditorGUILayout.FloatField("BPM", chart.bpm));
        chart.beatsPerBar = Mathf.Max(1,  EditorGUILayout.IntField  ("Beats Per Bar", chart.beatsPerBar));

        EditorGUILayout.Space(4);
        // Offset
        EditorGUILayout.LabelField("Timing Offset (global)", EditorStyles.boldLabel);
        chart.songOffsetSec = EditorGUILayout.DoubleField(new GUIContent("Song Offset (sec)", "正=整体更晚；负=整体更早"), chart.songOffsetSec);

        EditorGUILayout.Space(4);
        // Visual
        EditorGUILayout.LabelField("Note Flight (visual)", EditorStyles.boldLabel);
        chart.defaultLeadTimeSec = Mathf.Max(0.01f, EditorGUILayout.FloatField("Default Lead (sec)", chart.defaultLeadTimeSec));

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Bars", EditorStyles.boldLabel);

        // Top ops
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

        // Bars
        if (chart.bars != null)
        {
            for (int b = 0; b < chart.bars.Count; b++)
            {
                var bar = chart.bars[b];

                EditorGUILayout.BeginVertical("box");
                GUILayout.Space(RowPadV);

                // Row 1: quick params
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
                        break;
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(RowPadV);

                // Row 2: slots (cycle: Empty → Tap → Double → Burst)
                EnsureLength(ref bar);
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < bar.slots.Count; i++)
                {
                    var s = bar.slots[i];
                    string label = s switch
                    {
                        RhythmChart.SlotKind.Tap    => "T",
                        RhythmChart.SlotKind.Double => "D",
                        RhythmChart.SlotKind.Burst  => "B",
                        _                           => "·"
                    };
                    var tip = "Click to cycle: Empty → Tap → Double → Burst(B). 两个 B 依次标记一个 Burst 区间（可跨小节）。";
                    if (GUILayout.Button(new GUIContent(label, tip), GUILayout.Width(26)))
                    {
                        Undo.RecordObject(chart, "Edit Slot");
                        bar.slots[i] = Cycle(s);
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(RowPadV);
                EditorGUILayout.EndVertical();

                chart.bars[b] = bar;
            }
        }

        if (GUI.changed) EditorUtility.SetDirty(chart);
    }

    private static RhythmChart.SlotKind Cycle(RhythmChart.SlotKind s)
    {
        return s switch
        {
            RhythmChart.SlotKind.Empty  => RhythmChart.SlotKind.Tap,
            RhythmChart.SlotKind.Tap    => RhythmChart.SlotKind.Double,
            RhythmChart.SlotKind.Double => RhythmChart.SlotKind.Burst,
            _                           => RhythmChart.SlotKind.Empty,
        };
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
