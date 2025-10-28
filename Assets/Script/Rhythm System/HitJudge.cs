using System;
using UnityEngine;
using TMPro;

public class HitJudge : MonoBehaviour
{
    [SerializeField] private TMP_Text hitLabel;

    public static event Action OnBasicHit;
    public static event Action OnMiss;

    // 由外部调用的安全触发器
    public static void RaiseBasicHit() => OnBasicHit?.Invoke();
    public static void RaiseMiss()     => OnMiss?.Invoke();

    void OnEnable()
    {
        OnBasicHit += ShowHit;
        OnMiss     += ShowMiss;
    }

    void OnDisable()
    {
        OnBasicHit -= ShowHit;
        OnMiss     -= ShowMiss;
    }

    void ShowHit()
    {
        if (!hitLabel) return;
        hitLabel.SetText("HIT");
        hitLabel.color = Color.green;
    }

    void ShowMiss()
    {
        if (!hitLabel) return;
        hitLabel.SetText("MISS");
        hitLabel.color = Color.red;
    }
}