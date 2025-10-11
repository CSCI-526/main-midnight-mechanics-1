using System;
using UnityEngine;
using TMPro;

public class HitJudge : MonoBehaviour
{
    [SerializeField] private RhythmSystem rhythm;
    [SerializeField] private TMP_Text hitLabel;

    public static event Action OnBasicHit;
    public static event Action OnMiss;

    void Reset()
    {
        if (!rhythm) rhythm = FindObjectOfType<RhythmSystem>();
        if (!hitLabel)
        {
            var t = GameObject.Find("HitLabel");
            if (t) hitLabel = t.GetComponent<TMP_Text>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (rhythm && rhythm.IsInHitWindow())
            {
                hitLabel?.SetText("HIT");
                if (hitLabel) hitLabel.color = Color.green;
                OnBasicHit?.Invoke();
            }
            else
            {
                hitLabel?.SetText("MISS");
                if (hitLabel) hitLabel.color = Color.red;
                OnMiss?.Invoke();
            }
            
            rhythm?.ForceNextRound();
        }
    }
}