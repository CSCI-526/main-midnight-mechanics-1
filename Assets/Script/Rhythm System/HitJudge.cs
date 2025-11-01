using System;
using UnityEngine;
using TMPro;

public class HitJudge : MonoBehaviour
{
    [SerializeField] private TMP_Text hitLabel;

    public static event Action OnPerfect;
    public static event Action OnGood;
    public static event Action OnMiss;

    public static void RaisePerfect() => OnPerfect?.Invoke();
    public static void RaiseGood()    => OnGood?.Invoke();
    public static void RaiseMiss()    => OnMiss?.Invoke();

    void OnEnable()
    {
        OnPerfect += ShowPerfect;
        OnGood    += ShowGood;
        OnMiss    += ShowMiss;
    }

    void OnDisable()
    {
        OnPerfect -= ShowPerfect;
        OnGood    -= ShowGood;
        OnMiss    -= ShowMiss;
    }

    void ShowPerfect()
    {
        if (!hitLabel) return;
        hitLabel.SetText("PERFECT");
        hitLabel.color = new Color(0.6f, 1f, 0.6f);
    }

    void ShowGood()
    {
        if (!hitLabel) return;
        hitLabel.SetText("GOOD");
        hitLabel.color = new Color(0.6f, 0.8f, 1f);
    }

    void ShowMiss()
    {
        if (!hitLabel) return;
        hitLabel.SetText("MISS");
        hitLabel.color = Color.red;
    }
}