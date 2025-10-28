using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] private PatternCell tapPrefab;      // 单击音符的预制
    [SerializeField] private PatternCell doublePrefab;   // 双键音符的预制

    [Header("Pool Prewarm")]
    [SerializeField] private int prewarmTap = 12;
    [SerializeField] private int prewarmDouble = 6;

    [Header("Visual Spawn")]
    [SerializeField] private float spawnLeftPaddingPx = 40f; // 从左侧屏外进场的额外像素

    [Header("Hit FX")]
    [SerializeField] private float onHitFreezeSeconds     = 0.06f;
    [SerializeField] private float onPerfectScaleUpFactor = 1.25f;  // 相对“基础尺寸”（Prefab 的初始缩放）
    [SerializeField] private float onGoodScaleUpFactor    = 1.12f;
    [SerializeField] private float onHitScaleUpSeconds    = 0.08f;

    [Header("Miss FX")]
    [SerializeField] private float onMissFreezeSeconds    = 0.04f;
    [SerializeField] private float onMissScaleDownSeconds = 0.10f;

    [Header("Double Settings")]
    [SerializeField] private float doubleSecondGapSec = 0.08f; // 第二键需不同键，且在该间隔内

    [Header("Debug UI (TMP)")]
    [SerializeField] private TMP_Text judgeLabel;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip   keyPressSfx;
    [SerializeField, Range(0f,1f)] private float keyPressSfxVolume = 1f;

    // —— 由 LevelRunner / RhythmChartPlayer 驱动 ——
    bool   chartMode  = true;
    double chartNowSec = 0.0;

    enum JudgeKind { None, Perfect, Good, Miss }
    enum NoteType  { Tap, Double }

    struct Note
    {
        public NoteType type;
        public PatternCell widget;

        public double tStartSec;   // 命中时刻（秒）
        public float  leadTimeSec; // 飞行时长（秒）

        public float  baseScale;   // 以 Prefab 初始缩放为基线（动画相对它缩放）

        public bool      judged;
        public JudgeKind judgedKind;

        // Double 判定缓存
        public bool     dblAwaiting;
        public KeyCode  dblFirstKey;
        public float    dblExpireU;

        // 命中/失误后的冻结与动画
        public float freezeUntilU;
        public bool  animStarted;
        public float animT;
    }

    readonly List<Note> _notes = new();

    // 两套对象池
    readonly Queue<PatternCell> _poolTap    = new();
    readonly Queue<PatternCell> _poolDouble = new();

    float _innerHalf;
    float _lastTrackW = -1f;
    Vector3[] _tmpCorners;

    // ===== Unity =====
    void Awake()
    {
        if (!patternRow) patternRow = transform as RectTransform;
        EnsurePatternRowMatchesTrack(true);
        RecalcInnerHalf();
        PrewarmPools();

        // 校验基本设置
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
    }

    void Update()
    {
        if (!enabled || !chartMode) return;

        EnsurePatternRowMatchesTrack();
        RecalcInnerHalf();

        // 收集可用按键；有按键则播按键音（可关/不填则无声）
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

            // 从左到命中：t01=0→1
            float t01 = 1f - (float)((n.tStartSec - chartNowSec) / n.leadTimeSec);
            float x   = Mathf.LerpUnclamped(spawnLeftX, centerRowX, t01);
            SetX_Unclamped(n.widget.Rect, x);

            // 若已过右侧 Miss 区边界且时间也过了命中时刻 -> 自动 Miss
            float cxTrack = GetRectCenterXInTrack(n.widget.Rect);
            if (cxTrack > missRightT && chartNowSec > n.tStartSec)
            {
                n.judged       = true;
                n.judgedKind   = JudgeKind.Miss;
                n.freezeUntilU = Time.unscaledTime + onMissFreezeSeconds;
                n.animStarted  = false;
                n.animT        = 0f;
                n.widget.SetWrong();
                _notes[i] = n;
                SetLabel("MISS");
            }
        }

        // 输入：每个按键逐个处理（Double 优先）
        for (int i = 0; i < pressed.Count; i++)
            HandleKey(pressed[i]);

        // 命中/失误的动画与回收
        AnimateAndCull();
    }

    // ===== 外部 API =====
    public void EnableChartMode(bool on) => chartMode = on;
    public void SetChartNow(double nowSec) => chartNowSec = nowSec;

    public void EnqueueTap(double hitTimeSec, float leadTimeSec)
    {
        var w = GetTapCell();
        w.transform.SetParent(patternRow, false);
        w.gameObject.SetActive(true);
        w.ResetVisual();                          // 恢复 Prefab 默认图与缩放
        SetX_Unclamped(w.Rect, GetSpawnLeftX());

        _notes.Add(new Note
        {
            type       = NoteType.Tap,
            widget     = w,
            tStartSec  = hitTimeSec,
            leadTimeSec= Mathf.Max(0.01f, leadTimeSec),

            baseScale  = w.InitialScale,

            judged = false, judgedKind = JudgeKind.None,
            dblAwaiting = false, dblFirstKey = KeyCode.None, dblExpireU = 0f,
            freezeUntilU = 0f, animStarted = false, animT = 0f
        });
    }

    public void EnqueueDouble(double hitTimeSec, float leadTimeSec)
    {
        var w = GetDoubleCell();
        w.transform.SetParent(patternRow, false);
        w.gameObject.SetActive(true);
        w.ResetVisual();                          // 使用 Double Prefab 的默认图与缩放
        SetX_Unclamped(w.Rect, GetSpawnLeftX());

        _notes.Add(new Note
        {
            type       = NoteType.Double,
            widget     = w,
            tStartSec  = hitTimeSec,
            leadTimeSec= Mathf.Max(0.01f, leadTimeSec),

            baseScale  = w.InitialScale,

            judged = false, judgedKind = JudgeKind.None,
            dblAwaiting = false, dblFirstKey = KeyCode.None, dblExpireU = 0f,
            freezeUntilU = 0f, animStarted = false, animT = 0f
        });
    }

    // ===== 输入判定 =====
    void HandleKey(KeyCode key)
    {
        // Double 优先：当前按键只消耗一个音符，取落在判定区里“最右侧”的 Double
        int iDouble = PickRightmostInZones(NoteType.Double, out JudgeKind zoneKindDouble);
        if (iDouble >= 0)
        {
            var n = _notes[iDouble];
            if (!n.dblAwaiting)
            {
                // 记录第一键
                n.dblAwaiting = true;
                n.dblFirstKey = key;
                n.dblExpireU  = Time.unscaledTime + doubleSecondGapSec;
                _notes[iDouble] = n;
                SetLabel("DOUBLE…");
                return; // 本次按键被消费
            }
            else
            {
                // 第二键：不同键、限时、仍在判定区
                if (key != n.dblFirstKey && Time.unscaledTime <= n.dblExpireU && IsNoteInAnyZone(iDouble))
                {
                    n.judged = true;
                    n.judgedKind = zoneKindDouble == JudgeKind.Perfect ? JudgeKind.Perfect :
                                   zoneKindDouble == JudgeKind.Good    ? JudgeKind.Good    : JudgeKind.Miss;
                    if (n.judgedKind == JudgeKind.Miss)
                    {
                        n.freezeUntilU = Time.unscaledTime + onMissFreezeSeconds;
                        n.widget.SetWrong();
                        SetLabel("MISS");
                    }
                    else
                    {
                        n.freezeUntilU = Time.unscaledTime + onHitFreezeSeconds;
                        n.widget.SetOk();
                        SetLabel(n.judgedKind == JudgeKind.Perfect ? "PERFECT (2x)" : "GOOD (2x)");
                        HitJudge.RaiseBasicHit();
                    }
                    n.animStarted = false; n.animT = 0f;
                    _notes[iDouble] = n;
                    return; // 本次按键被消费
                }
                // 第二键失败：不立刻 Miss，留给右侧越界或后续输入；继续尝试 Tap
            }
        }

        // Tap：取最右侧
        int iTap = PickRightmostInZones(NoteType.Tap, out JudgeKind zoneKindTap);
        if (iTap >= 0)
        {
            var hit = _notes[iTap];
            hit.judged = true;

            if (zoneKindTap == JudgeKind.Miss)
            {
                hit.judgedKind   = JudgeKind.Miss;
                hit.freezeUntilU = Time.unscaledTime + onMissFreezeSeconds;
                hit.widget.SetWrong();
                SetLabel("MISS");
            }
            else
            {
                hit.judgedKind   = zoneKindTap;
                hit.freezeUntilU = Time.unscaledTime + onHitFreezeSeconds;
                hit.widget.SetOk();
                SetLabel(zoneKindTap == JudgeKind.Perfect ? "PERFECT" : "GOOD");
                HitJudge.RaiseBasicHit();
            }

            hit.animStarted = false; hit.animT = 0f;
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

    // ===== 动画 / 回收（以 baseScale 为基线） =====
    void AnimateAndCull()
    {
        float nowU = Time.unscaledTime;

        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            var n = _notes[i];
            if (n.widget == null) { _notes.RemoveAt(i); continue; }
            if (!n.judged) continue;

            if (nowU < n.freezeUntilU) continue;

            if (!n.animStarted)
            {
                n.animStarted = true;
                n.animT = 0f;
                _notes[i] = n;
                continue;
            }

            n.animT += Time.unscaledDeltaTime;

            if (n.judgedKind == JudgeKind.Miss)
            {
                float t01 = Mathf.Clamp01(n.animT / Mathf.Max(0.0001f, onMissScaleDownSeconds));
                float s   = Mathf.Lerp(n.baseScale, 0f, t01);
                n.widget.SetScale(s);
                if (t01 >= 1f) { ReturnCell(n.widget, n.type); _notes.RemoveAt(i); }
                else _notes[i] = n;
            }
            else
            {
                float up     = Mathf.Max(0.0001f, onHitScaleUpSeconds);
                float factor = (n.judgedKind == JudgeKind.Perfect) ? onPerfectScaleUpFactor : onGoodScaleUpFactor;
                float target = n.baseScale * factor;
                float t01    = Mathf.Clamp01(n.animT / up);
                float s      = Mathf.Lerp(n.baseScale, target, t01);
                n.widget.SetScale(s);
                if (t01 >= 1f) { ReturnCell(n.widget, n.type); _notes.RemoveAt(i); }
                else _notes[i] = n;
            }
        }
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
        if (Input.GetKeyDown(KeyCode.Escape)) return r; // 忽略 ESC
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

        // 用 Tap 或 Double 的 Rect 宽估半径，避免为 0
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

    public void ResetForNewLevel()
    {
        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            if (_notes[i].widget) ReturnCell(_notes[i].widget, _notes[i].type);
        }
        _notes.Clear();
        SetLabel(string.Empty);
    }
}
