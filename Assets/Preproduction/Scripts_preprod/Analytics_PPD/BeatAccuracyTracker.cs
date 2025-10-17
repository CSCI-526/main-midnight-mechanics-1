// using UnityEngine;

// public class BeatAccuracyTracker : MonoBehaviour
// {
//     private int totalHits = 0;
//     private int perfectHits = 0;

//     [SerializeField] private HitJudge hitJudge;

//     void OnEnable()
//     {
//         HitJudge.OnBasicHit += HandleHit;
//         HitJudge.OnMiss += HandleMiss;
//     }

//     void OnDisable()
//     {
//         HitJudge.OnBasicHit -= HandleHit;
//         HitJudge.OnMiss -= HandleMiss;
//     }

//     void HandleHit()
//     {
//         totalHits++;
//         // Determine hit quality
//         float progress = hitJudge != null ? hitJudge.GetRhythmProgress() : 0f;
//         if (Mathf.Abs(progress - hitJudge.HitCenter) <= hitJudge.HitHalfWidth / 2f)
//             perfectHits++;

//         LogBeatAccuracy();
//     }

//     void HandleMiss()
//     {
//         totalHits++;
//         LogBeatAccuracy();
//     }

//     void LogBeatAccuracy()
//     {
//         if (totalHits == 0) return;

//         float accuracy = (float)perfectHits / totalHits * 100f;
//         FirebaseManager.Instance?.LogEvent(
//             "beat_accuracy",
//             "accuracy_percent",
//             accuracy.ToString("F2")
//         );

//         Debug.Log($"[Analytics] Beat Accuracy: {accuracy:F2}%");
//     }

//     public void ResetTracker()
//     {
//         totalHits = 0;
//         perfectHits = 0;
//     }
// }
