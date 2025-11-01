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
        if (mainText) mainText.text = $"{prefix}{current}";
        if (_flashTimer <= 0f && mainText) mainText.color = normalColor;
    }

    void OnDelta(int delta)
    {
        bool gain = delta > 0;
        _targetColor = gain ? gainColor : lossColor;
        _flashTimer = flashTime;

        if (deltaText)
        {
            deltaText.text = (gain ? "+" : "") + delta.ToString();
            deltaText.color = _targetColor;
            deltaText.gameObject.SetActive(true);
        }
    }
}
