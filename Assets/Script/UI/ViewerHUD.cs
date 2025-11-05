using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ViewerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ViewerSystem viewers;
    [SerializeField] private TMP_Text mainText;    // "Viewers: 978"
    [SerializeField] private TMP_Text deltaText;   // "+20" / "-250"（可选）
    [SerializeField] private Image  pulseBg;       // 可选背景块

    [Header("Style")]
    [SerializeField] private string prefix = "Viewers: ";
    [SerializeField] private float flashTime = 0.15f;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color gainColor   = new Color(0.6f, 1f, 0.6f);
    [SerializeField] private Color lossColor   = new Color(1f, 0.6f, 0.6f);

    // ★ 缩写开关（>=1000 显示成 1.2K；也支持 M/B）
    [Header("Abbreviation")]
    [SerializeField] private bool abbreviateCounts = true;
    [SerializeField, Min(100)] private int abbreviateFrom = 1000;

    float _flashTimer = 0f;
    Color _targetColor;

    void Awake()
    {
        if (!viewers) viewers = FindObjectOfType<ViewerSystem>(true);
    }

    void OnEnable()
    {
        if (!viewers) return;
        viewers.OnViewersChanged += OnChanged;
        viewers.OnDeltaApplied   += OnDelta;

        OnChanged(viewers.Current);
        if (deltaText) deltaText.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        if (!viewers) return;
        viewers.OnViewersChanged -= OnChanged;
        viewers.OnDeltaApplied   -= OnDelta;
    }

    void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            float t = Mathf.InverseLerp(flashTime, 0f, _flashTimer);
            var col = Color.Lerp(_targetColor, normalColor, t);

            if (mainText) mainText.color = col;
            if (pulseBg)  pulseBg.color  = new Color(col.r, col.g, col.b, 0.15f);
            if (_flashTimer <= 0f && deltaText) deltaText.gameObject.SetActive(false);
        }
    }

    void OnChanged(int current)
    {
        if (!mainText) return;
        string num = FormatCount(current);
        mainText.text = $"{prefix}{num}";
        if (_flashTimer <= 0f) mainText.color = normalColor;
    }

    void OnDelta(int delta)
    {
        bool gain = delta > 0;
        _targetColor = gain ? gainColor : lossColor;
        _flashTimer = flashTime;

        if (deltaText)
        {
            deltaText.text  = FormatSigned(delta);
            deltaText.color = _targetColor;
            deltaText.gameObject.SetActive(true);
        }
    }

    // === Abbrev helpers ===
    string FormatCount(int v)
    {
        long a = Mathf.Max(0, v);
        if (!abbreviateCounts || Mathf.Abs(v) < abbreviateFrom)
            return a.ToString("N0");                     // 1,234 样式（可改成 "D" 保持无逗号）
        return AbbrevInt(a);
    }

    string FormatSigned(int delta)
    {
        string sign = delta > 0 ? "+" : (delta < 0 ? "-" : "");
        long mag = Mathf.Abs(delta);
        string body = (!abbreviateCounts || mag < abbreviateFrom)
                        ? mag.ToString("N0")
                        : AbbrevInt(mag);
        return sign + body;
    }

    // 与你捐赠里的 AbbrevUSD 风格一致（0.## 保留两位）
    static string AbbrevInt(long val)
    {
        if (val >= 1_000_000_000) return $"{val / 1_000_000_000f:0.##}B";
        if (val >= 1_000_000)     return $"{val / 1_000_000f:0.##}M";
        /*>=*/                      return $"{val / 1_000f:0.##}K";
    }
}
