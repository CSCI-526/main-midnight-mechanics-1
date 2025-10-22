using System;
using UnityEngine;
using TMPro;

public class HitJudge : MonoBehaviour
{
    [SerializeField] private RhythmSystem rhythm;
    [SerializeField] private TMP_Text hitLabel;

    public static event Action OnBasicHit;
    public static event Action OnMiss;
    
    private bool _lockedThisRound = false;

    void Reset()
    {
        if (!rhythm) rhythm = FindObjectOfType<RhythmSystem>();
        if (!hitLabel)
        {
            var t = GameObject.Find("HitLabel");
            if (t) hitLabel = t.GetComponent<TMP_Text>();
        }
    }

    void OnEnable()
    {
        if (rhythm)
        {
            rhythm.OnRoundStart += HandleRoundStart;
            rhythm.OnRoundEnd   += HandleRoundEnd; 
        }
    }

    void OnDisable()
    {
        if (rhythm)
        {
            rhythm.OnRoundStart -= HandleRoundStart;
            rhythm.OnRoundEnd   -= HandleRoundEnd;
        }
    }

    void HandleRoundStart()
    {
        _lockedThisRound = false; 
        if (hitLabel) hitLabel.SetText(""); 
    }

    void HandleRoundEnd()
    {

    }

    void Update()
    {
        if (!rhythm) return;


        if (_lockedThisRound) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _lockedThisRound = true;

            if (rhythm.IsInHitWindow())
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
            
        }
    }
}