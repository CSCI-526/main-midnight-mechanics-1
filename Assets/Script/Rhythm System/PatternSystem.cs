using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PatternSystem : MonoBehaviour
{
    [Header("Track & Zones")]
    [SerializeField] private RectTransform trackRect;
    [SerializeField] private RectTransform zonePerfect;
    [SerializeField] private RectTransform zoneGood;
    [SerializeField] private RectTransform zoneMiss;

    [Header("Note Row")]
    [SerializeField] private RectTransform patternRow;

    [Header("Prefabs")]
    [SerializeField] private PatternCell tapPrefab;
    [SerializeField] private PatternCell doublePrefab;
    [SerializeField] private PatternCell burstPrefab; // 9-sliced Image，自动拉伸

    [Header("Pool Prewarm")]
    [SerializeField] private int prewarmTap = 12;
    [SerializeField] private int prewarmDouble = 6;
    [SerializeField] private int prewarmBurst = 2;

    [Header("Visual Spawn")]
    [SerializeField] private float spawnLeftPaddingPx = 40f;

    [Header("Judge FX (Scale)")]
    [SerializeField] private float hitScaleFactor = 1.2f;
    [SerializeField] private float scaleSeconds = 0.10f;

    [Header("Cell Tint (Overlay) — 叠色更深")]
    [SerializeField] private bool  useOverlayTint = true;
    [SerializeField] private Color tintPerfect = new Color(0.25f, 1.00f, 0.35f, 1f);
    [SerializeField] private Color tintGood    = new Color(0.35f, 0.70f, 1.00f, 1f);
    [SerializeField] private Color tintMiss    = new Color(1.00f, 0.30f, 0.30f, 1f);
    [SerializeField, Range(0f,1f)] private float tintAlpha = 0.85f;
    [SerializeField] private float tintFadeIn  = 0.06f;
    [SerializeField] private float tintHold    = 0.06f;
    [SerializeField] private float tintFadeOut = 0.22f;

    [Header("Burst Hit Tint")]
    [SerializeField] private Color burstHitTint = new Color(1f, 1f, 1f, 1f);
    [SerializeField, Range(0f,1f)] private float burstTintAlpha = 0.65f;
    [SerializeField] private float burstTintIn  = 0.05f;
    [SerializeField] private float burstTintHold= 0.03f;
    [SerializeField] private float burstTintOut = 0.18f;

    [Header("Double Settings")]
    [SerializeField] private float doubleSecondGapSec = 0.08f;

    [Header("Debug UI (TMP)")]
    [SerializeField] private TMP_Text judgeLabel;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip   keyPressSfx;
    [SerializeField, Range(0f,1f)] private float keyPressSfxVolume = 1f;
    [SerializeField, Range(0.1f,4f)] private float doubleSfxMultiplier = 2f; // Double更响

    [Header("Rhythm Bar Flash")]
    [SerializeField] private Image rhythmBar;
    [SerializeField] private Color perfectFlashColor = new Color(0.60f, 1.00f, 0.60f, 1f);
    [SerializeField] private Color goodFlashColor    = new Color(0.60f, 0.80f, 1.00f, 1f); // 不再使用，仅保留字段
    [SerializeField] private Color missFlashColor    = new Color(1.00f, 0.40f, 0.40f, 1f);
    [SerializeField, Min(0f)] private float perfectFlashSeconds = 0.08f;
    [SerializeField, Min(0f)] private float goodFlashSeconds    = 0.08f; // 不再使用，仅保留字段
    [SerializeField, Min(0f)] private float missFlashSeconds    = 0.10f;

    [Header("Viewers")]
    [SerializeField] private ViewerSystem viewers; // 如空则自动查找

    [Header("Burst Safety")]
    [SerializeField] private bool forceBurstCenterAnchors = true; // 运行时把 Burst 的 X 锚点/枢轴强制居中

    // —— 外部驱动 ——
    bool   chartMode   = true;
    double chartNowSec = 0.0;

    enum JudgeKind { None, Perfect, Good, Miss }
    enum NoteType  { Tap, Double }

    struct Note
    {
        public NoteType type;
        public PatternCell widget;

        public double tStartSec;
        public float  leadTimeSec;
        public double departSec;

        public float  baseScale;

        public bool      judged;
        public JudgeKind judgedKind;

        // Double 专用
        public bool     dblAwaiting;  // 第一击后等待第二击
        public KeyCode  dblFirstKey;
        public float    dblExpireU;

        // 命中后动画
        public bool  animStarted;
        public float animT;
    }

    struct Burst
    {
        public PatternCell widget;
        public double startSec;
        public double endSec;
        public float  leadTimeSec;
        public double departStartSec;
        public double departEndSec;
        public float  baseScale;

        public bool   alive;
    }

    readonly List<Note>  _notes  = new();
    readonly List<Burst> _bursts = new();

    readonly Queue<PatternCell> _poolTap    = new();
    readonly Queue<PatternCell> _poolDouble = new();
    readonly Queue<PatternCell> _poolBurst  = new();

    float _innerHalf;
    float _lastTrackW = -1f;
    Vector3[] _tmpCorners;

    Color _barDefaultColor;
    float _barFlashTimeLeft = 0f;
    float _barFlashTotal    = 0f;
    Color _barFlashFrom;
    Color _barFlashTo;

    void Awake()
    {
        if (!patternRow) patternRow = transform as RectTransform;
        EnsurePatternRowMatchesTrack(true);
        RecalcInnerHalf();
        PrewarmPools();

        if (!tapPrefab || !doublePrefab || !burstPrefab)
        {
            Debug.LogError("[PatternSystem] Prefab 未设置（tap/double/burst）。", this);
            enabled = false; return;
        }
        if (!tapPrefab.ValidateSetup(true) || !doublePrefab.ValidateSetup(true) || !burstPrefab.ValidateSetup(true))
        {
            Debug.LogError("[PatternSystem] 某个 PatternCell Prefab 的必填项未设置（Rect/Icon）。", this);
            enabled = false; return;
        }
        if (!trackRect || !zonePerfect || !zoneGood || !zoneMiss)
        {
            Debug.LogError("[PatternSystem] 判定区（trackRect/perfect/good/miss）未设置。", this);
            enabled = false; return;
        }

        if (!viewers) viewers = Object.FindFirstObjectByType<ViewerSystem>(FindObjectsInactive.Include);
        if (rhythmBar) _barDefaultColor = rhythmBar.color;
    }

    void Update()
    {
        if (GamePause.IsPaused) return;
        if (!enabled || !chartMode) return;
        if (!ZonesReady()) return;

        EnsurePatternRowMatchesTrack();
        RecalcInnerHalf();

        var pressed = GetPlayableKeysDownThisFrame();
        float centerRowX = GetPerfectCenterXInRow();
        float spawnLeftX = GetSpawnLeftX();
        float missRightT = GetZoneRightXInTrack(zoneMiss);

        // 推进 Note 位置 & 右侧越界自动 Miss
        for (int i = 0; i < _notes.Count; i++)
        {
            var n = _notes[i];
            if (!n.widget || n.judged) continue;

            float tRaw = 0f;
            if (n.leadTimeSec > 0f)
                tRaw = (float)((chartNowSec - n.departSec) / n.leadTimeSec);
            float x = Mathf.LerpUnclamped(spawnLeftX, centerRowX, tRaw);
            SetX_Unclamped(n.widget.Rect, x);

            float cxTrack = GetRectCenterXInTrack(n.widget.Rect);
            if (cxTrack > missRightT && chartNowSec > n.tStartSec)
            {
                n.judged     = true;
                n.judgedKind = JudgeKind.Miss;

                // ★ Miss 也叠红色（更深）
                TintNote(n.widget, n.judgedKind);

                SetLabel("MISS");
                FlashBar(JudgeKind.Miss);
                ApplyViewerDelta(JudgeKind.Miss);
                HitJudge.RaiseMiss();

                n.animStarted = false;
                n.animT = 0f;
                _notes[i] = n;
            }
        }

        // 推进 Burst
        for (int i = _bursts.Count - 1; i >= 0; i--)
        {
            var b = _bursts[i];
            if (!b.alive || !b.widget) { _bursts.RemoveAt(i); continue; }

            float t0 = (float)((chartNowSec - b.departStartSec) / b.leadTimeSec);
            float t1 = (float)((chartNowSec - b.departEndSec)   / b.leadTimeSec);

            float x0 = Mathf.LerpUnclamped(spawnLeftX, centerRowX, t0);
            float x1 = Mathf.LerpUnclamped(spawnLeftX, centerRowX, t1);

            SetBurstRectBetween(b.widget.Rect, x0, x1);

            float xEndTrack = Mathf.Max(GetRectLeftXInTrack(b.widget.Rect), GetRectRightXInTrack(b.widget.Rect));
            if (xEndTrack > missRightT && chartNowSec > b.endSec)
            {
                ReturnBurst(b.widget);
                _bursts.RemoveAt(i);
            }
            else
            {
                _bursts[i] = b;
            }
        }

        // —— 输入判定 —— //
        for (int i = 0; i < pressed.Count; i++)
        {
            var key = pressed[i];
            bool consumed = false;

            int idxAwait = FindRightmostAwaitingDouble(out JudgeKind zoneKindAwait);
            if (idxAwait >= 0)
            {
                consumed = HandleKey_DoubleAt(idxAwait, key, zoneKindAwait);
            }
            else
            {
                int idx = PickRightmostOverall(out NoteType pickedType, out JudgeKind zoneKind);
                if (idx >= 0)
                {
                    if (pickedType == NoteType.Double)
                        consumed = HandleKey_DoubleAt(idx, key, zoneKind);
                    else
                        consumed = HandleKey_TapAt(idx, zoneKind);
                }
            }

            if (!consumed)
                HandleKey_Burst(key);
        }

        AnimateAndCullNotes();
        TickBarFlash();
    }

    // ===== 外部 API =====
    public void EnableChartMode(bool on) => chartMode = on;
    public void SetChartNow(double nowSec) => chartNowSec = nowSec;

    public void EnqueueTap(double hitTimeSec, float leadTimeSec)
    {
        var w = GetTapCell();
        w.transform.SetParent(patternRow, false);
        w.gameObject.SetActive(true);
        w.ResetVisual();
        SetX_Unclamped(w.Rect, GetSpawnLeftX());

        float lead = Mathf.Max(0.01f, leadTimeSec);

        _notes.Add(new Note
        {
            type        = NoteType.Tap,
            widget      = w,
            tStartSec   = hitTimeSec,
            leadTimeSec = lead,
            departSec   = hitTimeSec - lead,
            baseScale   = w.InitialScale,
            judged = false, judgedKind = JudgeKind.None,
            dblAwaiting = false, dblFirstKey = KeyCode.None, dblExpireU = 0f,
            animStarted = false, animT = 0f
        });
    }

    public void EnqueueDouble(double hitTimeSec, float leadTimeSec)
    {
        var w = GetDoubleCell();
        w.transform.SetParent(patternRow, false);
        w.gameObject.SetActive(true);
        w.ResetVisual();
        SetX_Unclamped(w.Rect, GetSpawnLeftX());

        float lead = Mathf.Max(0.01f, leadTimeSec);

        _notes.Add(new Note
        {
            type        = NoteType.Double,
            widget      = w,
            tStartSec   = hitTimeSec,
            leadTimeSec = lead,
            departSec   = hitTimeSec - lead,
            baseScale   = w.InitialScale,
            judged = false, judgedKind = JudgeKind.None,
            dblAwaiting = false, dblFirstKey = KeyCode.None, dblExpireU = 0f,
            animStarted = false, animT = 0f
        });
    }

    public void EnqueueBurst(double startSec, double endSec, float leadTimeSec)
    {
        if (endSec < startSec) { var t = startSec; startSec = endSec; endSec = t; }

        var w = GetBurstCell();
        w.transform.SetParent(patternRow, false);
        w.gameObject.SetActive(true);
        w.ResetVisual();

        if (forceBurstCenterAnchors && w && w.Rect)
        {
            var rt = w.Rect;
            if (rt.anchorMin.x != 0.5f || rt.anchorMax.x != 0.5f)
            {
                rt.anchorMin = new Vector2(0.5f, rt.anchorMin.y);
                rt.anchorMax = new Vector2(0.5f, rt.anchorMax.y);
            }
            if (!Mathf.Approximately(rt.pivot.x, 0.5f))
            {
                var pv = rt.pivot; pv.x = 0.5f; rt.pivot = pv;
            }
        }

        w.transform.SetAsLastSibling();

        float lead = Mathf.Max(0.01f, leadTimeSec);

        var b = new Burst
        {
            widget         = w,
            startSec       = startSec,
            endSec         = endSec,
            leadTimeSec    = lead,
            departStartSec = startSec - lead,
            departEndSec   = endSec   - lead,
            baseScale      = w.InitialScale,
            alive          = true
        };
        _bursts.Add(b);

        float spawnLeftX = GetSpawnLeftX();
        SetBurstRectBetween(w.Rect, spawnLeftX, spawnLeftX);
    }

    // ===== 统一“只判最右边”的选择器 =====
    int PickRightmostOverall(out NoteType type, out JudgeKind zoneKind)
    {
        type = NoteType.Tap; zoneKind = JudgeKind.None;

        int idxPerfect = -1, idxGood = -1, idxMiss = -1;
        float xPerfect = float.NegativeInfinity, xGood = float.NegativeInfinity, xMiss = float.NegativeInfinity;
        NoteType tPerfect = NoteType.Tap, tGood = NoteType.Tap, tMiss = NoteType.Tap;

        for (int i = 0; i < _notes.Count; i++)
        {
            var n = _notes[i];
            if (n.judged || !n.widget) continue;

            float cx = GetRectCenterXInTrack(n.widget.Rect);

            if (InsideZoneXInTrack(cx, zonePerfect) && cx > xPerfect) { xPerfect = cx; idxPerfect = i; tPerfect = n.type; }
            else if (InsideZoneXInTrack(cx, zoneGood) && cx > xGood)  { xGood    = cx; idxGood    = i; tGood    = n.type; }
            else if (InsideZoneXInTrack(cx, zoneMiss) && cx > xMiss)  { xMiss    = cx; idxMiss    = i; tMiss    = n.type; }
        }

        if (idxPerfect >= 0) { type = tPerfect; zoneKind = JudgeKind.Perfect; return idxPerfect; }
        if (idxGood    >= 0) { type = tGood;    zoneKind = JudgeKind.Good;    return idxGood; }
        if (idxMiss    >= 0) { type = tMiss;    zoneKind = JudgeKind.Miss;    return idxMiss; }
        return -1;
    }

    int FindRightmostAwaitingDouble(out JudgeKind zoneKind)
    {
        zoneKind = JudgeKind.None;

        int idxPerfect = -1, idxGood = -1, idxMiss = -1;
        float xPerfect = float.NegativeInfinity, xGood = float.NegativeInfinity, xMiss = float.NegativeInfinity;

        for (int i = 0; i < _notes.Count; i++)
        {
            var n = _notes[i];
            if (n.judged || !n.widget) continue;
            if (n.type != NoteType.Double || !n.dblAwaiting) continue;

            float cx = GetRectCenterXInTrack(n.widget.Rect);

            if (InsideZoneXInTrack(cx, zonePerfect) && cx > xPerfect) { xPerfect = cx; idxPerfect = i; }
            else if (InsideZoneXInTrack(cx, zoneGood) && cx > xGood)  { xGood    = cx; idxGood    = i; }
            else if (InsideZoneXInTrack(cx, zoneMiss) && cx > xMiss)  { xMiss    = cx; idxMiss    = i; }
        }

        if (idxPerfect >= 0) { zoneKind = JudgeKind.Perfect; return idxPerfect; }
        if (idxGood    >= 0) { zoneKind = JudgeKind.Good;    return idxGood; }
        if (idxMiss    >= 0) { zoneKind = JudgeKind.Miss;    return idxMiss; }
        return -1;
    }

    // ===== 指定索引判 Tap / Double =====
    bool HandleKey_TapAt(int idx, JudgeKind zoneKindTap)
    {
        if (idx < 0 || idx >= _notes.Count) return false;
        var n = _notes[idx];
        if (n.judged || n.type != NoteType.Tap) return false;

        n.judged = true;
        n.judgedKind = zoneKindTap;

        // ★ 用叠色替代换图：Perfect/Good/Miss 各自染色；强度由 tintAlpha 控
        TintNote(n.widget, n.judgedKind);

        if (zoneKindTap == JudgeKind.Miss)
        {
            SetLabel("MISS");
            FlashBar(JudgeKind.Miss);
            ApplyViewerDelta(JudgeKind.Miss);
            HitJudge.RaiseMiss();
        }
        else
        {
            SetLabel(zoneKindTap == JudgeKind.Perfect ? "PERFECT" : "GOOD");
            FlashBar(n.judgedKind); // Good 已在 FlashBar 中被忽略
            ApplyViewerDelta(n.judgedKind);
            if (n.judgedKind == JudgeKind.Perfect) HitJudge.RaisePerfect();
            else                                    HitJudge.RaiseGood();
            PlayJudgeSfxOnce(1f);
        }

        n.animStarted = false; n.animT = 0f;
        _notes[idx] = n;
        return true;
    }

    bool HandleKey_DoubleAt(int idx, KeyCode key, JudgeKind zoneKindAtPick)
    {
        if (idx < 0 || idx >= _notes.Count) return false;
        var n = _notes[idx];
        if (n.judged || n.type != NoteType.Double) return false;

        if (!n.dblAwaiting)
        {
            n.dblAwaiting = true;
            n.dblFirstKey = key;
            n.dblExpireU  = Time.unscaledTime + doubleSecondGapSec;
            _notes[idx] = n;
            SetLabel("DOUBLE…");
            return true;
        }

        if (key != n.dblFirstKey && Time.unscaledTime <= n.dblExpireU && IsNoteInAnyZone(idx))
        {
            n.judged = true;
            var k = zoneKindAtPick;
            if (k == JudgeKind.None) k = JudgeKind.Good;
            n.judgedKind = k;

            // ★ 叠色
            TintNote(n.widget, n.judgedKind);

            if (n.judgedKind == JudgeKind.Miss)
            {
                SetLabel("MISS");
                FlashBar(JudgeKind.Miss);
                ApplyViewerDelta(JudgeKind.Miss);
                HitJudge.RaiseMiss();
            }
            else
            {
                SetLabel(n.judgedKind == JudgeKind.Perfect ? "PERFECT (2x)" : "GOOD (2x)");
                FlashBar(n.judgedKind); // Good 被忽略
                ApplyViewerDelta(n.judgedKind);
                if (n.judgedKind == JudgeKind.Perfect) HitJudge.RaisePerfect();
                else                                    HitJudge.RaiseGood();
                PlayJudgeSfxOnce(doubleSfxMultiplier);
            }

            n.animStarted = false; n.animT = 0f;
            _notes[idx] = n;
            return true;
        }

        return false;
    }

    void HandleKey_Burst(KeyCode key)
    {
        // 与 Good/Perfect 重叠即判
        JudgeKind kind = JudgeKind.None;
        bool anyOverlap = false;

        for (int i = 0; i < _bursts.Count; i++)
        {
            var b = _bursts[i];
            if (!b.alive || !b.widget) continue;

            GetBurstEdgesInTrack(b.widget.Rect, out float leftX, out float rightX);

            bool overlapPerfect = SegmentIntersectsZone(leftX, rightX, zonePerfect, out _);
            bool overlapGood    = SegmentIntersectsZone(leftX, rightX, zoneGood,    out _);

            if (overlapPerfect) { kind = JudgeKind.Perfect; anyOverlap = true; break; }
            if (overlapGood)    { kind = JudgeKind.Good;    anyOverlap = true; /*继续看看有没有Perfect*/ }
        }

        if (anyOverlap && (kind == JudgeKind.Perfect || kind == JudgeKind.Good))
        {
            SetLabel(kind == JudgeKind.Perfect ? "PERFECT (Burst)" : "GOOD (Burst)");
            FlashBar(kind);
            ApplyViewerDelta(kind);
            if (kind == JudgeKind.Perfect) HitJudge.RaisePerfect();
            else                            HitJudge.RaiseGood();

            PlayJudgeSfxOnce(1f);

            // ★ Burst 自身叠色（可连点连闪）：对所有当前重叠的 burst 施加一次 flash
            for (int i = 0; i < _bursts.Count; i++)
            {
                var b = _bursts[i];
                if (!b.alive || !b.widget) continue;
                GetBurstEdgesInTrack(b.widget.Rect, out float l, out float r);
                bool overlapP = SegmentIntersectsZone(l, r, zonePerfect, out _);
                bool overlapG = SegmentIntersectsZone(l, r, zoneGood,    out _);
                if (overlapP || overlapG)
                    b.widget.FlashTint(burstHitTint, burstTintAlpha, burstTintIn, burstTintHold, burstTintOut);
            }
        }
    }

    // ===== 叠色助手 =====
    void TintNote(PatternCell w, JudgeKind k)
    {
        if (!useOverlayTint || !w) return;
        Color c = k switch
        {
            JudgeKind.Perfect => tintPerfect,
            JudgeKind.Good    => tintGood,
            JudgeKind.Miss    => tintMiss,
            _ => tintGood
        };
        w.FlashTint(c, tintAlpha, tintFadeIn, tintHold, tintFadeOut);
    }

    // ===== 辅助：是否仍在任一区域内 =====
    bool IsNoteInAnyZone(int i)
    {
        var n = _notes[i];
        float cx = GetRectCenterXInTrack(n.widget.Rect);
        return InsideZoneXInTrack(cx, zonePerfect) || InsideZoneXInTrack(cx, zoneGood) || InsideZoneXInTrack(cx, zoneMiss);
    }

    // ===== 动画 / 回收（Note）=====
    void AnimateAndCullNotes()
    {
        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            var n = _notes[i];
            if (n.widget == null) { _notes.RemoveAt(i); continue; }
            if (!n.judged) continue;

            if (!n.animStarted)
            {
                n.animStarted = true;
                n.animT = 0f;
                _notes[i] = n;
                continue;
            }

            n.animT += Time.unscaledDeltaTime;
            float t01 = Mathf.Clamp01(n.animT / Mathf.Max(0.0001f, scaleSeconds));

            if (n.judgedKind == JudgeKind.Miss)
            {
                float s = Mathf.Lerp(n.baseScale, 0f, t01);
                n.widget.SetScale(s);
            }
            else
            {
                float target = n.baseScale * Mathf.Max(1f, hitScaleFactor);
                float s = Mathf.Lerp(n.baseScale, target, t01);
                n.widget.SetScale(s);
            }

            if (t01 >= 1f)
            {
                ReturnCell(n.widget, n.type);
                _notes.RemoveAt(i);
            }
            else
            {
                _notes[i] = n;
            }
        }
    }

    // ===== Bar 闪色（Good 不再闪；Perfect/Miss 保留）=====
    void FlashBar(JudgeKind kind)
    {
        if (!rhythmBar) return;
        if (kind == JudgeKind.Good) return; // ★ 忽略 Good 的闪烁

        Color c; float dur;
        switch (kind)
        {
            case JudgeKind.Perfect: c = perfectFlashColor; dur = Mathf.Max(0f, perfectFlashSeconds); break;
            case JudgeKind.Miss:    c = missFlashColor;    dur = Mathf.Max(0f, missFlashSeconds);    break;
            default: return;
        }

        if (_barDefaultColor.a <= 0f) _barDefaultColor = rhythmBar.color;
        _barFlashFrom     = c;
        _barFlashTo       = _barDefaultColor;
        _barFlashTotal    = (dur <= 0 ? 0.0001f : dur);
        _barFlashTimeLeft = _barFlashTotal;

        rhythmBar.color = c;
    }

    void TickBarFlash()
    {
        if (!rhythmBar) return;
        if (_barFlashTimeLeft <= 0f)
        {
            if (rhythmBar.color != _barDefaultColor)
                rhythmBar.color = _barDefaultColor;
            return;
        }

        _barFlashTimeLeft -= Time.unscaledDeltaTime;
        float t = 1f - Mathf.Clamp01(_barFlashTimeLeft / _barFlashTotal);
        rhythmBar.color = Color.Lerp(_barFlashFrom, _barFlashTo, t);

        if (_barFlashTimeLeft <= 0f)
            rhythmBar.color = _barDefaultColor;
    }

    // ===== 对象池 =====
    void PrewarmPools()
    {
        if (tapPrefab && prewarmTap > 0)
            for (int i = 0; i < prewarmTap; i++)
            {
                var c = Instantiate(tapPrefab);
                c.gameObject.SetActive(false);
                _poolTap.Enqueue(c);
            }

        if (doublePrefab && prewarmDouble > 0)
            for (int i = 0; i < prewarmDouble; i++)
            {
                var c = Instantiate(doublePrefab);
                c.gameObject.SetActive(false);
                _poolDouble.Enqueue(c);
            }

        if (burstPrefab && prewarmBurst > 0)
            for (int i = 0; i < prewarmBurst; i++)
            {
                var c = Instantiate(burstPrefab);
                c.gameObject.SetActive(false);
                _poolBurst.Enqueue(c);
            }
    }

    PatternCell GetTapCell()
    {
        if (_poolTap.Count > 0)
        {
            var c = _poolTap.Dequeue();
            c.ResetVisual();
            c.gameObject.SetActive(true);
            return c;
        }
        return Instantiate(tapPrefab);
    }

    PatternCell GetDoubleCell()
    {
        if (_poolDouble.Count > 0)
        {
            var c = _poolDouble.Dequeue();
            c.ResetVisual();
            c.gameObject.SetActive(true);
            return c;
        }
        return Instantiate(doublePrefab);
    }

    PatternCell GetBurstCell()
    {
        if (_poolBurst.Count > 0)
        {
            var c = _poolBurst.Dequeue();
            c.ResetVisual();
            c.gameObject.SetActive(true);
            return c;
        }
        return Instantiate(burstPrefab);
    }

    void ReturnCell(PatternCell c, NoteType type)
    {
        if (!c) return;
        c.gameObject.SetActive(false);
        if (type == NoteType.Tap) _poolTap.Enqueue(c);
        else _poolDouble.Enqueue(c);
    }

    void ReturnBurst(PatternCell c)
    {
        if (!c) return;
        c.gameObject.SetActive(false);
        _poolBurst.Enqueue(c);
    }

    // ===== 输入 & 音效 =====
    static readonly KeyCode[] sPlayableKeys = BuildPlayableKeys();

    static KeyCode[] BuildPlayableKeys()
    {
        return new[] { KeyCode.W, KeyCode.O };
    }

    List<KeyCode> GetPlayableKeysDownThisFrame()
    {
        var r = new List<KeyCode>(4);
        if (Input.GetKeyDown(KeyCode.Escape)) return r;
        for (int i = 0; i < sPlayableKeys.Length; i++)
            if (Input.GetKeyDown(sPlayableKeys[i]))
                r.Add(sPlayableKeys[i]);
        return r;
    }

    void PlayJudgeSfxOnce(float mul)
    {
        if (sfxSource && keyPressSfx)
        {
            float v = Mathf.Max(0f, keyPressSfxVolume * mul);
            sfxSource.PlayOneShot(keyPressSfx, v);
        }
    }

    // ===== 几何工具 =====
    float GetPerfectCenterXInRow()
    {
        float centerT = GetZoneCenterXInTrack(zonePerfect);
        Vector3 world = trackRect.TransformPoint(new Vector3(centerT, 0f, 0f));
        return patternRow.InverseTransformPoint(world).x;
    }

    float GetSpawnLeftX() => -_innerHalf - Mathf.Abs(spawnLeftPaddingPx);

    float GetZoneCenterXInTrack(RectTransform z)
    {
        GetZoneBoundsInTrack(z, out float l, out float r);
        return 0.5f * (l + r);
    }

    float GetZoneRightXInTrack(RectTransform z)
    {
        GetZoneBoundsInTrack(z, out float l, out float r);
        return r;
    }

    bool InsideZoneXInTrack(float xTrack, RectTransform z)
    {
        GetZoneBoundsInTrack(z, out float left, out float right);
        return xTrack >= left && xTrack <= right;
    }

    void GetZoneBoundsInTrack(RectTransform z, out float left, out float right)
    {
        Vector3[] c = _tmpCorners ??= new Vector3[4];
        z.GetWorldCorners(c);
        left  = float.PositiveInfinity;
        right = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            float x = trackRect.InverseTransformPoint(c[i]).x;
            if (x < left) left = x;
            if (x > right) right = x;
        }
    }

    float GetRectCenterXInTrack(RectTransform rt)
    {
        Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
        return trackRect.InverseTransformPoint(worldCenter).x;
    }

    float GetRectLeftXInTrack(RectTransform rt)
    {
        Vector3[] c = _tmpCorners ??= new Vector3[4];
        rt.GetWorldCorners(c);
        float left = float.PositiveInfinity;
        for (int i = 0; i < 4; i++)
        {
            float x = trackRect.InverseTransformPoint(c[i]).x;
            if (x < left) left = x;
        }
        return left;
    }

    float GetRectRightXInTrack(RectTransform rt)
    {
        Vector3[] c = _tmpCorners ??= new Vector3[4];
        rt.GetWorldCorners(c);
        float right = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            float x = trackRect.InverseTransformPoint(c[i]).x;
            if (x > right) right = x;
        }
        return right;
    }

    void RecalcInnerHalf()
    {
        float rowW   = patternRow ? patternRow.rect.width : (trackRect ? trackRect.rect.width : 0f);
        float rowHalf = rowW * 0.5f;
        float radius  = 0f;

        var probe = tapPrefab ? tapPrefab.GetComponent<RectTransform>() :
                   (doublePrefab ? doublePrefab.GetComponent<RectTransform>() : null);
        if (probe) radius = Mathf.Abs(probe.rect.width) * 0.5f;

        _innerHalf = Mathf.Max(0f, rowHalf - radius);
    }

    void EnsurePatternRowMatchesTrack(bool force = false)
    {
        if (!trackRect || !patternRow) return;
        float w = trackRect.rect.width;
        if (!force && Mathf.Approximately(w, _lastTrackW)) return;
        patternRow.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        _lastTrackW = w;
    }

    static void SetX_Unclamped(RectTransform rt, float x)
    {
        if (!rt) return;
        var p = rt.anchoredPosition; p.x = x; rt.anchoredPosition = p;
    }

    void SetBurstRectBetween(RectTransform rt, float x0, float x1)
    {
        if (!rt) return;
        float cx = 0.5f * (x0 + x1);
        float w  = Mathf.Abs(x1 - x0);
        var size = rt.sizeDelta;
        size.x = Mathf.Max(8f, w);
        rt.sizeDelta = size;

        var p = rt.anchoredPosition; p.x = cx; rt.anchoredPosition = p;
    }

    void GetBurstEdgesInTrack(RectTransform rt, out float leftX, out float rightX)
    {
        Vector3[] c = _tmpCorners ??= new Vector3[4];
        rt.GetWorldCorners(c);
        leftX  = float.PositiveInfinity;
        rightX = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            float x = trackRect.InverseTransformPoint(c[i]).x;
            if (x < leftX) leftX = x;
            if (x > rightX) rightX = x;
        }
    }

    bool SegmentIntersectsZone(float segLeft, float segRight, RectTransform zone, out float overlapWidth)
    {
        GetZoneBoundsInTrack(zone, out float zl, out float zr);
        float l = Mathf.Max(segLeft, zl);
        float r = Mathf.Min(segRight, zr);
        overlapWidth = r - l;
        return overlapWidth > 0.0001f;
    }

    void SetLabel(string s)
    {
        if (judgeLabel) judgeLabel.text = s;
    }

    bool ZonesReady()
    {
        if (!trackRect || trackRect.rect.width <= 1f) return false;
        return ZoneHasWidth(zonePerfect) && ZoneHasWidth(zoneGood) && ZoneHasWidth(zoneMiss);
    }

    bool ZoneHasWidth(RectTransform z)
    {
        if (!z) return false;
        GetZoneBoundsInTrack(z, out float l, out float r);
        return (r - l) > 1f;
    }

    void ApplyViewerDelta(JudgeKind kind)
    {
        if (!viewers) return;
        switch (kind)
        {
            case JudgeKind.Perfect: viewers.ApplyJudgement(NoteJudgement.Perfect); break;
            case JudgeKind.Good:    viewers.ApplyJudgement(NoteJudgement.Good);    break;
            case JudgeKind.Miss:    viewers.ApplyJudgement(NoteJudgement.Miss);    break;
        }
    }

    public void ResetForNewLevel()
    {
        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            if (_notes[i].widget) ReturnCell(_notes[i].widget, _notes[i].type);
        }
        _notes.Clear();

        for (int i = _bursts.Count - 1; i >= 0; i--)
        {
            if (_bursts[i].widget) ReturnBurst(_bursts[i].widget);
        }
        _bursts.Clear();

        SetLabel(string.Empty);
        if (rhythmBar) rhythmBar.color = _barDefaultColor;
        _barFlashTimeLeft = 0f;
    }
}
