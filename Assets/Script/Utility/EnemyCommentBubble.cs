using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto-builds a world-space chat bubble above this object.
/// - Uses your 9-sliced Sprite as background (Image.Type = Sliced).
/// - Sizes to text automatically within [min,max] width and wraps.
/// - Fades in, holds, fades out.
/// - Optional auto-refresh on a random interval.
/// Attach to your Enemy prefab; no manual Canvas sizing required.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyCommentBubble : MonoBehaviour
{
    [Header("9-slice Bubble")]
    [Tooltip("Your 9-sliced sprite (Sprite Editor -> set Border).")]
    [SerializeField] private Sprite bubbleSprite;
    [Tooltip("Background color tint (alpha also affects overall opacity).")]
    [SerializeField] private Color bubbleColor = Color.white;

    [Header("Text")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField, Min(6)] private int fontSize = 24;
    [SerializeField, TextArea(1, 6)] private string[] commentPool;

    [Header("Layout")]
    [Tooltip("Bubble width clamps; text wraps when reaching max width.")]
    [SerializeField] private float minWidth = 120f;
    [SerializeField] private float maxWidth = 360f;
    [Tooltip("Padding L,T,R,B in pixels (applied via VerticalLayoutGroup padding).")]
    [SerializeField] private Vector4 padding = new Vector4(18, 12, 18, 12);
    [Tooltip("Local vertical offset from this transform to place the bubble.")]
    [SerializeField] private float verticalOffset = 1.2f;
    [Tooltip("World-space scale of the bubble Canvas (keep small in 2D).")]
    [SerializeField] private float canvasScale = 0.01f;
    [Tooltip("Keep the bubble upright in 2D (zero rotation).")]
    [SerializeField] private bool lockRotation2D = true;

    [Header("Show / Refresh")]
    [SerializeField] private bool showOnEnable = true;
    [SerializeField] private float fadeInSeconds = 0.12f;
    [SerializeField] private float holdSeconds   = 2.0f;
    [SerializeField] private float fadeOutSeconds= 0.15f;
    [Tooltip("If true, shows a new random comment every interval.")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private Vector2 refreshIntervalSeconds = new Vector2(3f, 6f);

    // built graph
    Canvas _canvas;
    RectTransform _root;     // bubble root with Image + layout + fitter
    Image _bg;
    TMP_Text _txt;
    CanvasGroup _cg;

    Coroutine _playCo;
    float _nextRefreshU;

    void OnEnable()
    {
        EnsureGraph();
        PositionBubble();
        if (showOnEnable) ShowRandom();
        if (autoRefresh)  ScheduleNextRefresh();
    }

    void OnDisable()
    {
        if (_playCo != null) StopCoroutine(_playCo);
    }

    void LateUpdate()
    {
        if (!_root) return;

        // keep above the owner
        PositionBubble();

        // keep upright for 2D
        if (lockRotation2D)
            _root.rotation = Quaternion.identity;

        // periodic refresh
        if (autoRefresh && Time.unscaledTime >= _nextRefreshU)
        {
            ShowRandom();
            ScheduleNextRefresh();
        }
    }

    /// <summary>Shows a specific message now.</summary>
    public void Show(string message)
    {
        EnsureGraph();
        ApplyVisuals(message ?? string.Empty);
        if (_playCo != null) StopCoroutine(_playCo);
        _playCo = StartCoroutine(CoPlayOnce());
    }

    /// <summary>Shows a random message from the pool.</summary>
    public void ShowRandom()
    {
        string msg = PickRandom(commentPool);
        if (string.IsNullOrEmpty(msg)) msg = "…";
        Show(msg);
    }

    // --- build graph once ---
    void EnsureGraph()
    {
        if (_canvas) return;

        // World-space canvas under this enemy
        var goCanvas = new GameObject("BubbleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        goCanvas.transform.SetParent(transform, false);
        _canvas = goCanvas.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        //_canvas.sortingOrder = 2000; // above sprites
        _cg = goCanvas.GetComponent<CanvasGroup>();
        _cg.alpha = 0f;

        var rtCanvas = (RectTransform)goCanvas.transform;
        rtCanvas.localScale = Vector3.one * Mathf.Max(0.0001f, canvasScale);
        rtCanvas.sizeDelta = new Vector2(1, 1); // world-space canvas size isn't critical here

        // Root: Image + VerticalLayoutGroup + ContentSizeFitter
        var goRoot = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        goRoot.transform.SetParent(goCanvas.transform, false);
        _root = (RectTransform)goRoot.transform;

        _bg = goRoot.GetComponent<Image>();
        _bg.sprite = bubbleSprite;
        _bg.type   = Image.Type.Sliced;
        _bg.color  = bubbleColor;
        _bg.raycastTarget = false;

        var vlg = goRoot.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(Mathf.RoundToInt(padding.x), Mathf.RoundToInt(padding.z),
                                     Mathf.RoundToInt(padding.y), Mathf.RoundToInt(padding.w));
        vlg.spacing = 0f;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;

        var csf = goRoot.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var rootLE = goRoot.GetComponent<LayoutElement>();
        rootLE.minWidth  = minWidth;
        rootLE.preferredWidth = maxWidth; // upper clamp; we'll still clamp text size below

        // Text
        var goText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        goText.transform.SetParent(goRoot.transform, false);
        _txt = goText.GetComponent<TextMeshProUGUI>();
        _txt.enableWordWrapping = true;
        _txt.fontSize = fontSize;
        _txt.color    = textColor;
        _txt.alignment = TextAlignmentOptions.TopLeft;
        _txt.raycastTarget = false;
        if (font) _txt.font = font;

        var textLE = goText.GetComponent<LayoutElement>();
        textLE.flexibleWidth  = 0f; // don't try to expand
        textLE.flexibleHeight = 0f;

        // Start hidden
        _cg.alpha = 0f;
    }

    void PositionBubble()
    {
        if (!_root) return;
        var rtCanvas = (RectTransform)_canvas.transform;
        rtCanvas.localPosition = new Vector3(0f, verticalOffset, 0f);
    }

    void ApplyVisuals(string message)
    {
        if (_bg)
        {
            _bg.sprite = bubbleSprite;
            _bg.type   = Image.Type.Sliced;
            _bg.color  = bubbleColor;
        }

        if (_txt)
        {
            _txt.text     = message;
            _txt.color    = textColor;
            _txt.fontSize = fontSize;
            if (font) _txt.font = font;

            // Compute preferred size under maxWidth and clamp
            // First set a temp width for wrapping evaluation
            float clampW = Mathf.Max(minWidth, maxWidth);
            var pref = _txt.GetPreferredValues(message, clampW, 0f);
            float finalW = Mathf.Clamp(pref.x, minWidth, maxWidth);

            var textRT = (RectTransform)_txt.transform;
            textRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalW);

            // Re-evaluate height at final width for a tight fit
            var pref2 = _txt.GetPreferredValues(message, finalW, 0f);
            textRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(pref2.y, fontSize * 1.2f));

            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        }
    }

    IEnumerator CoPlayOnce()
    {
        // fade in
        float t = 0f, durIn = Mathf.Max(0.0001f, fadeInSeconds);
        while (t < durIn)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Clamp01(t / durIn);
            yield return null;
        }
        _cg.alpha = 1f;

        // hold
        float hold = Mathf.Max(0f, holdSeconds);
        float uEnd = Time.unscaledTime + hold;
        while (Time.unscaledTime < uEnd) yield return null;

        // fade out
        t = 0f; float durOut = Mathf.Max(0.0001f, fadeOutSeconds);
        while (t < durOut)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = 1f - Mathf.Clamp01(t / durOut);
            yield return null;
        }
        _cg.alpha = 0f;
        _playCo = null;
    }

    void ScheduleNextRefresh()
    {
        float a = Mathf.Min(refreshIntervalSeconds.x, refreshIntervalSeconds.y);
        float b = Mathf.Max(refreshIntervalSeconds.x, refreshIntervalSeconds.y);
        _nextRefreshU = Time.unscaledTime + Random.Range(a, b);
    }

    static string PickRandom(IReadOnlyList<string> arr)
    {
        if (arr == null || arr.Count == 0) return string.Empty;
        int i = Random.Range(0, arr.Count);
        return arr[i];
    }

    // --- Public setters (optional) ---

    public void SetBubbleSprite(Sprite s) { bubbleSprite = s; if (_bg) _bg.sprite = s; }
    public void SetComments(IEnumerable<string> list)
    {
        if (list == null) return;
        var tmp = new List<string>(list);
        commentPool = tmp.ToArray();
    }
}
