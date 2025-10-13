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
            OnRoundEnd?.Invoke();   // 到达最右端：本轮结束
            Progress01 -= 1f;       // 立刻回到左端（保留溢出部分更平滑）
            OnRoundStart?.Invoke(); // 新一轮开始
        }

        MoveIndicator();
        ApplyHitZoneVisual();
    }

    void MoveIndicator()
    {
        if (!trackRect || !indicatorRect) return;
        
        float trackHalf = trackRect.rect.width * 0.5f;

        
        float radius = indicatorRect.rect.width * 0.5f;
        
        float innerHalf = Mathf.Max(0f, trackHalf - radius);
        
        var p = indicatorRect.anchoredPosition;
        p.x = Mathf.Lerp(-innerHalf, +innerHalf, Progress01);
        indicatorRect.anchoredPosition = p;
    }

    void ApplyHitZoneVisual()
    {
        if (!trackRect || !hitZoneRect) return;

        float trackW = trackRect.rect.width;
        float centerX = Mathf.Lerp(-trackW * 0.5f, +trackW * 0.5f, hitCenter);
        float width   = Mathf.Clamp01(hitHalfWidth * 2f) * trackW;

        var lp = hitZoneRect.anchoredPosition; lp.x = centerX; hitZoneRect.anchoredPosition = lp;
        var sz = hitZoneRect.sizeDelta;        sz.x = width;   hitZoneRect.sizeDelta = sz;
    }

    public bool IsInHitWindow()
    {
        // 只看区间即可（不再区分正/反向）
        return Mathf.Abs(Progress01 - hitCenter) <= hitHalfWidth;
    }
    
    public void ForceNextRound()
    {
        // 先发出“本回合结束”
        OnRoundEnd?.Invoke();

        // 把进度重置到起点（左端）
        Progress01 = 0f;

        // 立刻开始下一回合
        OnRoundStart?.Invoke();
    }
    
    public void SetCycleSeconds(float seconds)
    {
        var sec = Mathf.Max(0.01f, seconds);
        forwardSeconds = sec;
    }
}

