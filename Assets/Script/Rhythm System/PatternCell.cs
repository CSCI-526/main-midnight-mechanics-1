using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PatternCell : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image icon;

    [Header("Judge Sprites (仍保留但不再强制使用)")]
    [SerializeField] private Sprite hitSprite;   // 旧逻辑备用
    [SerializeField] private Sprite missSprite;  // 旧逻辑备用

    [Header("Tint Overlay (叠加色层)")]
    [Tooltip("如未手动指定，会在运行时基于 icon 复制一层 Overlay。")]
    [SerializeField] private Image tintOverlay;
    [SerializeField, Range(0f, 1f)] private float defaultMaxAlpha = 0.85f;
    [SerializeField] private float defaultFadeIn  = 0.06f;
    [SerializeField] private float defaultHold    = 0.06f;
    [SerializeField] private float defaultFadeOut = 0.22f;

    private Sprite _initialSprite;
    private float  _initialScale = 1f;
    private Coroutine _tintCo;

    public RectTransform Rect => rect;
    public float InitialScale => _initialScale;

    void Awake()
    {
        if (icon) _initialSprite = icon.sprite;
        if (rect) _initialScale  = rect.localScale.x;
        EnsureOverlayReady();
    }

    void OnDisable()
    {
        if (_tintCo != null) StopCoroutine(_tintCo);
        if (tintOverlay) tintOverlay.color = new Color(0,0,0,0);
    }

    void EnsureOverlayReady()
    {
        if (!rect || !icon) return;

        if (!tintOverlay)
        {
            var go = new GameObject("TintOverlay", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(icon.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot     = icon.rectTransform.pivot;

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            // 复制 icon 的外观形态（Simple/Sliced 等）
            img.sprite = icon.sprite;
            img.type   = icon.type;
            img.pixelsPerUnitMultiplier = icon.pixelsPerUnitMultiplier;

            tintOverlay = img;
        }

        // 叠到最上层，保证覆盖 icon
        tintOverlay.transform.SetAsLastSibling();
        tintOverlay.color = new Color(0,0,0,0);
    }

    public bool ValidateSetup(bool log)
    {
        bool ok = (rect != null) && (icon != null);
        if (log && !ok) Debug.LogError("[PatternCell] setup invalid (Rect/Icon).", this);
        return ok;
    }

    public void ResetVisual()
    {
        if (_tintCo != null) StopCoroutine(_tintCo);
        if (icon) icon.sprite = _initialSprite;
        if (rect) rect.localScale = new Vector3(_initialScale, _initialScale, 1f);
        if (tintOverlay) tintOverlay.color = new Color(0,0,0,0);
    }

    // 兼容旧接口（不再强制换图）
    public void SetOk()   { /* 保留空实现或按需换图： if (icon && hitSprite) icon.sprite = hitSprite; */ }
    public void SetWrong(){ /* 保留空实现或按需换图： if (icon && missSprite) icon.sprite = missSprite; */ }

    public void SetScale(float s)
    {
        if (!rect) return;
        rect.localScale = new Vector3(s, s, 1f);
    }

    /// <summary>
    /// 叠加色层闪烁：更深的染色效果（建议 alpha 0.7~0.9）。
    /// </summary>
    public void FlashTint(Color color, float maxAlpha = -1f, float fadeIn = -1f, float hold = -1f, float fadeOut = -1f)
    {
        EnsureOverlayReady();
        if (!tintOverlay) return;

        if (_tintCo != null) StopCoroutine(_tintCo);
        _tintCo = StartCoroutine(CoFlashTint(color,
            maxAlpha  < 0f ? defaultMaxAlpha : Mathf.Clamp01(maxAlpha),
            fadeIn    < 0f ? defaultFadeIn   : Mathf.Max(0.0001f, fadeIn),
            hold      < 0f ? defaultHold     : Mathf.Max(0f, hold),
            fadeOut   < 0f ? defaultFadeOut  : Mathf.Max(0.0001f, fadeOut)
        ));
    }

    IEnumerator CoFlashTint(Color c, float aMax, float tIn, float tHold, float tOut)
    {
        // 先把 overlay sprite 与 icon 对齐（防止运行时替换了图）
        if (tintOverlay && icon)
        {
            tintOverlay.sprite = icon.sprite;
            tintOverlay.type   = icon.type;
            tintOverlay.pixelsPerUnitMultiplier = icon.pixelsPerUnitMultiplier;
            tintOverlay.transform.SetAsLastSibling();
        }

        // Fade In
        float t = 0f;
        while (t < tIn)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tIn);
            var col = c; col.a = aMax * k;
            tintOverlay.color = col;
            yield return null;
        }

        // Hold
        if (tHold > 0f)
        {
            var col = c; col.a = aMax;
            tintOverlay.color = col;
            yield return new WaitForSecondsRealtime(tHold);
        }

        // Fade Out
        t = 0f;
        while (t < tOut)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / tOut);
            var col = c; col.a = aMax * k;
            tintOverlay.color = col;
            yield return null;
        }

        tintOverlay.color = new Color(0,0,0,0);
        _tintCo = null;
    }
}
