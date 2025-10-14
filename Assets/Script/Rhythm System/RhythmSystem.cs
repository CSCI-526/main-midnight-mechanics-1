using System;
using UnityEngine;
using UnityEngine.UI;

public class RhythmSystem : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform trackRect;
    [SerializeField] private RectTransform indicatorRect;
    [SerializeField] private RectTransform hitZoneRect;

    [Header("Timing")]
    [SerializeField, Tooltip("左->右完整一轮的时长(秒)")]
    private float forwardSeconds = 1.2f;

    [Header("Hit Window (normalized on Track width)")]
    [Range(0f, 1f)] public float hitCenter = 0.82f;
    [Range(0f, 0.5f)] public float hitHalfWidth = 0.04f;

    public float Progress01 { get; private set; } = 0f;

    public event Action OnRoundStart;
    public event Action OnRoundEnd;

    float Speed01 => 1f / Mathf.Max(0.01f, forwardSeconds);

    public void SetCycleSeconds(float seconds) => forwardSeconds = Mathf.Max(0.01f, seconds); // ← 只保留这一份

    void Start()
    {
        Progress01 = 0f;
        OnRoundStart?.Invoke();
        ApplyHitZoneVisual();
    }

    void Update()
    {
        Progress01 += Speed01 * Time.deltaTime;

        if (Progress01 >= 1f)
        {
            OnRoundEnd?.Invoke();
            Progress01 -= 1f;
            OnRoundStart?.Invoke();
        }

        MoveIndicator();
        ApplyHitZoneVisual();
    }

    void MoveIndicator()
    {
        if (!trackRect || !indicatorRect) return;

        float trackHalf = trackRect.rect.width * 0.5f;
        float radius    = indicatorRect.rect.width * 0.5f;
        float innerHalf = Mathf.Max(0f, trackHalf - radius);

        var p = indicatorRect.anchoredPosition;
        p.x = Mathf.Lerp(-innerHalf, +innerHalf, Progress01);
        indicatorRect.anchoredPosition = p;
    }

    void ApplyHitZoneVisual()
    {
        if (!trackRect || !hitZoneRect) return;

        float trackW  = trackRect.rect.width;
        float centerX = Mathf.Lerp(-trackW * 0.5f, +trackW * 0.5f, hitCenter);
        float width   = Mathf.Clamp01(hitHalfWidth * 2f) * trackW;

        var lp = hitZoneRect.anchoredPosition; lp.x = centerX; hitZoneRect.anchoredPosition = lp;
        var sz = hitZoneRect.sizeDelta;        sz.x = width;   hitZoneRect.sizeDelta = sz;
    }

    public bool IsInHitWindow()
    {
        return Mathf.Abs(Progress01 - hitCenter) <= hitHalfWidth;
    }

    public void ForceNextRound()
    {
        OnRoundEnd?.Invoke();
        Progress01 = 0f;
        OnRoundStart?.Invoke();
    }
}
