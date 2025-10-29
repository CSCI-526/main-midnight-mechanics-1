using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;      // ★ 新增：要用 Image
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
    [SerializeField] private PatternCell tapPrefab;      // 单击音符
    [SerializeField] private PatternCell doublePrefab;   // 双键音符

    [Header("Pool Prewarm")]
    [SerializeField] private int prewarmTap = 12;
    [SerializeField] private int prewarmDouble = 6;

    [Header("Visual Spawn")]
    [SerializeField] private float spawnLeftPaddingPx = 40f;

    [Header("Judge FX (Simple)")]
    [SerializeField, Tooltip("命中放大的倍数（相对Prefab初始缩放）")]
    private float hitScaleFactor = 1.2f;
    [SerializeField, Tooltip("命中/失误缩放动画时长（秒）")]
    private float scaleSeconds = 0.10f;

    [Header("Double Settings")]
    [SerializeField, Tooltip("双键需要在该间隔内按下第二键，且与第一键不同")]
    private float doubleSecondGapSec = 0.08f;

    [Header("Debug UI (TMP)")]
    [SerializeField] private TMP_Text judgeLabel;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip   keyPressSfx;
    [SerializeField, Range(0f,1f)] private float keyPressSfxVolume = 1f;

    // === Rhythm Bar 闪色 ===
    [Header("Rhythm Bar Flash")]
    [SerializeField, Tooltip("要闪色的条（一般是 trackRect 上的 Image）")]
    private Image rhythmBar;                        // ← 在 Inspector 里拖进来
    [SerializeField] private Color perfectFlashColor = new Color(0.60f, 1.00f, 0.60f, 1f);
    [SerializeField] private Color goodFlashColor    = new Color(0.60f, 0.80f, 1.00f, 1f);
    [SerializeField] private Color missFlashColor    = new Color(1.00f, 0.40f, 0.40f, 1f);
    [SerializeField, Min(0f)] private float perfectFlashSeconds = 0.08f;
    [SerializeField, Min(0f)] private float goodFlashSeconds    = 0.08f;
    [SerializeField, Min(0f)] private float missFlashSeconds    = 0.10f;

    // —— 外部驱动 —— 
    bool   chartMode  = true;
    double chartNowSec = 0.0;

    enum JudgeKind { None, Perfect, Good, Miss }
    enum NoteType  { Tap, Double }

    struct Note
    {
        public NoteType type;
        public PatternCell widget;

        public double tStartSec;    // 命中时刻
        public float  leadTimeSec;  // 飞行时长
        public double departSec;    // 出发时刻 = tStartSec - leadTimeSec

        public float  baseScale;    // Prefab 初始缩放

        public bool      judged;
        public JudgeKind judgedKind;

        // Double 判定缓存
        public bool     dblAwaiting;
        public KeyCode  dblFirstKey;
        public float    dblExpireU;

        // 动画
        public bool  animStarted;
        public float animT;
    }

    readonly List<Note> _notes = new();
    readonly Queue<PatternCell> _poolTap    = new();
    readonly Queue<PatternCell> _poolDouble = new();

    float _innerHalf;
    float _lastTrackW = -1f;
    Vector3[] _tmpCorners;

    // Rhythm Bar 闪色状态
    Color _barDefaultColor;
    float _barFlashTimeLeft = 0f;
    float _barFlashTotal    = 0f;
    Color _barFlashFrom;
    Color _barFlashTo;

    // ===== Unity =====
    void Awake()
    {
        if (!patternRow) patternRow = transform as RectTransform;
        EnsurePatternRowMatchesTrack(true);
        RecalcInnerHalf();
        PrewarmPools();

        if (!tapPrefab || !doublePrefab)
        {
            Debug.LogError("[PatternSystem] tapPrefab / doublePrefab 未设置。", this);
            enabled = false; return;
        }
        if (!tapPrefab.ValidateSetup(true) || !doublePrefab.ValidateSetup(true))
        {
            Debug.LogError("[PatternSystem] 某个 PatternCell Prefab 的必填项未设置（Rect/Icon/Hit/Miss）。", this);
            enabled = false; return;
        }
        if (!trackRect || !zonePerfect || !zoneGood || !zoneMiss)
        {
            Debug.LogError("[PatternSystem] 判定区（trackRect/perfect/good/miss）未设置。", this);
            enabled = false; return;
        }

        // 记录 RhythmBar 默认颜色（若未指定 rhythmBar，则不做闪色）
        if (rhythmBar) _barDefaultColor = rhythmBar.color;
    }

    void Update()
    {
        if (!enabled || !chartMode) return;
        if (!ZonesReady()) return;

        EnsurePatternRowMatchesTrack();
        RecalcInnerHalf();

        var pressed = GetPlayableKeysDownThisFrame();
        if (pressed.Count > 0) PlayKeyPressSfx();

        float centerRowX = GetPerfectCenterXInRow();
        float spawnLeftX = GetSpawnLeftX();
        float missRightT = GetZoneRightXInTrack(zoneMiss);

        // 推进位置 & 右侧越界自动 Miss
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
                n.widget.SetWrong();
                SetLabel("MISS");
                FlashBar(JudgeKind.Miss);       // ★ Miss 闪色
                n.animStarted = false;
                n.animT = 0f;
                _notes[i] = n;
            }
        }

        // 输入：Double 优先
        for (int i = 0; i < pressed.Count; i++)
            HandleKey(pressed[i]);

        // 命中/失误缩放动画
        AnimateAndCull();

        // RhythmBar 闪色渐变回默认
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

    // ===== 输入判定（无冻结，立即进入缩放） =====
    void HandleKey(KeyCode key)
    {
        // Double 优先
        int iDouble = PickRightmostInZones(NoteType.Double, out JudgeKind zoneKindDouble);
        if (iDouble >= 0)
        {
            var n = _notes[iDouble];
            if (!n.dblAwaiting)
            {
                n.dblAwaiting = true;
                n.dblFirstKey = key;
                n.dblExpireU  = Time.unscaledTime + doubleSecondGapSec;
                _notes[iDouble] = n;
                SetLabel("DOUBLE…");
                return;
            }
            else
            {
                if (key != n.dblFirstKey && Time.unscaledTime <= n.dblExpireU && IsNoteInAnyZone(iDouble))
                {
                    n.judged = true;
                    n.judgedKind = zoneKindDouble == JudgeKind.Perfect ? JudgeKind.Perfect :
                                   zoneKindDouble == JudgeKind.Good    ? JudgeKind.Good    : JudgeKind.Miss;

                    if (n.judgedKind == JudgeKind.Miss) n.widget.SetWrong();
                    else                                 n.widget.SetOk();

                    SetLabel(n.judgedKind == JudgeKind.Miss ? "MISS"
                         : (n.judgedKind == JudgeKind.Perfect ? "PERFECT (2x)" : "GOOD (2x)"));

                    FlashBar(n.judgedKind);      // ★ 命中类型对应闪色
                    if (n.judgedKind != JudgeKind.Miss) HitJudge.RaiseBasicHit();

                    n.animStarted = false; n.animT = 0f;
                    _notes[iDouble] = n;
                    return;
                }
            }
        }

        // Tap
        int iTap = PickRightmostInZones(NoteType.Tap, out JudgeKind zoneKindTap);
        if (iTap >= 0)
        {
            var hit = _notes[iTap];
            hit.judged = true;
            hit.judgedKind = zoneKindTap;

            if (zoneKindTap == JudgeKind.Miss) hit.widget.SetWrong();
            else { hit.widget.SetOk(); HitJudge.RaiseBasicHit(); }

            SetLabel(zoneKindTap == JudgeKind.Miss ? "MISS"
                 : (zoneKindTap == JudgeKind.Perfect ? "PERFECT" : "GOOD"));

            FlashBar(zoneKindTap);        // ★ 命中类型对应闪色

            hit.animStarted = false;
            hit.animT = 0f;
            _notes[iTap] = hit;
        }
    }

    int PickRightmostInZones(NoteType type, out JudgeKind zoneKind)
    {
        int idxPerfect = -1; float xPerfect = float.NegativeInfinity;
        int idxGood    = -1; float xGood    = float.NegativeInfinity;
        int idxMiss    = -1; float xMiss    = float.NegativeInfinity;
        zoneKind = JudgeKind.None;

        for (int i = 0; i < _notes.Count; i++)
        {
            var n = _notes[i];
            if (n.type != type || n.judged || !n.widget) continue;

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

    bool IsNoteInAnyZone(int i)
    {
        var n = _notes[i];
        float cx = GetRectCenterXInTrack(n.widget.Rect);
        return InsideZoneXInTrack(cx, zonePerfect) || InsideZoneXInTrack(cx, zoneGood) || InsideZoneXInTrack(cx, zoneMiss);
    }

    // ===== 动画 / 回收：判定即刻开始缩放 =====
    void AnimateAndCull()
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

    // ===== RhythmBar 闪色逻辑 =====
    void FlashBar(JudgeKind kind)
    {
        if (!rhythmBar) return;

        Color c; float dur;
        switch (kind)
        {
            case JudgeKind.Perfect: c = perfectFlashColor; dur = Mathf.Max(0f, perfectFlashSeconds); break;
            case JudgeKind.Good:    c = goodFlashColor;    dur = Mathf.Max(0f, goodFlashSeconds);    break;
            case JudgeKind.Miss:    c = missFlashColor;    dur = Mathf.Max(0f, missFlashSeconds);    break;
            default: return;
        }

        if (_barDefaultColor.a <= 0f) _barDefaultColor = rhythmBar.color; // 兜底记一次默认色
        _barFlashFrom    = c;
        _barFlashTo      = _barDefaultColor;
        _barFlashTotal   = (dur <= 0f ? 0.0001f : dur);
        _barFlashTimeLeft= _barFlashTotal;

        rhythmBar.color  = c;  // 立即变成闪色
    }

    void TickBarFlash()
    {
        if (!rhythmBar) return;
        if (_barFlashTimeLeft <= 0f)
        {
            // 确保回到默认色
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

    void ReturnCell(PatternCell c, NoteType type)
    {
        if (!c) return;
        c.gameObject.SetActive(false);
        if (type == NoteType.Tap) _poolTap.Enqueue(c);
        else _poolDouble.Enqueue(c);
    }

    // ===== 输入 & 按键音效 =====
    static readonly KeyCode[] sPlayableKeys = BuildPlayableKeys();

    static KeyCode[] BuildPlayableKeys()
    {
        var list = new List<KeyCode>(80);
        for (var k = KeyCode.A; k <= KeyCode.Z; k++) list.Add(k);
        for (var k = KeyCode.Alpha0; k <= KeyCode.Alpha9; k++) list.Add(k);
        list.Add(KeyCode.Space);
        list.Add(KeyCode.UpArrow); list.Add(KeyCode.DownArrow);
        list.Add(KeyCode.LeftArrow); list.Add(KeyCode.RightArrow);
        list.Add(KeyCode.Period); list.Add(KeyCode.Comma);
        list.Add(KeyCode.Semicolon); list.Add(KeyCode.Quote);
        list.Add(KeyCode.Slash); list.Add(KeyCode.Backslash);
        list.Add(KeyCode.LeftBracket); list.Add(KeyCode.RightBracket);
        list.Add(KeyCode.Minus); list.Add(KeyCode.Equals);
        return list.ToArray();
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

    void PlayKeyPressSfx()
    {
        if (sfxSource && keyPressSfx)
            sfxSource.PlayOneShot(keyPressSfx, keyPressSfxVolume);
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
        left = float.PositiveInfinity; right = float.NegativeInfinity;
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

    public void ResetForNewLevel()
    {
        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            if (_notes[i].widget) ReturnCell(_notes[i].widget, _notes[i].type);
        }
        _notes.Clear();
        SetLabel(string.Empty);

        // 还原 RhythmBar 颜色
        if (rhythmBar) rhythmBar.color = _barDefaultColor;
        _barFlashTimeLeft = 0f;
    }
}
