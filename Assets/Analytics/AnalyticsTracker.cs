// using System.Collections;
// using UnityEngine;
// using UnityEngine.Networking;

// public class AnalyticsTracker : MonoBehaviour
// {
//     [Header("Debug")]
//     [SerializeField] private bool logSubmissions = true;

//     private string sess_id_no;

//     public static AnalyticsTracker Instance { get; private set; }

//     void Awake()
//     {
//         if (Instance && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }

//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//     }

//     void Start()
//     {
//         StartCoroutine(InitAfterSessionReady());
//     }

//     private IEnumerator InitAfterSessionReady()
//     {
//         // Wait until AnalyticsSession is initialized
//         while (AnalyticsSession.Instance == null)
//             yield return null;

//         // First: print AnalyticsSession ID in the log
//         Debug.Log($"[AnalyticsTracker] AnalyticsSession ID: {AnalyticsSession.Instance.SessionId}");

//         // Then: assign to tracker
//         sess_id_no = AnalyticsSession.Instance.SessionId;
//         Debug.Log($"[AnalyticsTracker] Linked to global session: {sess_id_no}");
//     }

//     private string GetCurrentLevelName()
//     {
//         if (SongSelection.Instance != null && !string.IsNullOrEmpty(SongSelection.Instance.CurrentSongName))
//             return SongSelection.Instance.CurrentSongName;

//         return "Unknown_Level";
//     }

//     public void TrackHitAuto(string judgement)
//     {
//         string levelName = GetCurrentLevelName();
//         StartCoroutine(SubmitFormAsync(levelName, judgement));
//     }

//     private IEnumerator SubmitFormAsync(string level, string judgement)
//     {
//         if (string.IsNullOrEmpty(sess_id_no))
//         {
//             Debug.LogWarning("[AnalyticsTracker] Session ID not ready yet. Skipping submission.");
//             yield break;
//         }

//         string baseUrl = "https://docs.google.com/forms/d/e/1FAIpQLSc5TmraNiYCBrhiS_l78VQJMfpLDIqUNMhjeleeQe9XuzzQMg/formResponse";

//         string sessionEntry = "entry.688029435=" + UnityWebRequest.EscapeURL(sess_id_no);
//         string levelEntry   = "entry.2117415998=" + UnityWebRequest.EscapeURL(level);
//         string hitEntry     = "entry.2099938677=" + UnityWebRequest.EscapeURL(judgement);

//         string fullUrl = $"{baseUrl}?{sessionEntry}&{levelEntry}&{hitEntry}";

//         if (logSubmissions) Debug.Log($"[Analytics] → {fullUrl}");

//         using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
//         {
//             www.timeout = 3;
//             yield return www.SendWebRequest();

//             if (www.result == UnityWebRequest.Result.Success)
//                 Debug.Log("[Analytics] ✓ DATA SAVED!");
//             else
//                 Debug.LogError($"[Analytics] ✗ {www.error}");
//         }
//     }

//     // Public API for easy access
//     public static void LogPerfect() => Instance?.TrackHitAuto("Perfect");
//     public static void LogGood() => Instance?.TrackHitAuto("Good");
//     public static void LogMiss() => Instance?.TrackHitAuto("Miss");
// }




// --------------------- NEW FORM---------------------------
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AnalyticsTracker : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logSubmissions = true;

    private string sess_id_no;

    public static AnalyticsTracker Instance { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(InitAfterSessionReady());
    }

    private IEnumerator InitAfterSessionReady()
    {
        // Wait until AnalyticsSession is initialized
        while (AnalyticsSession.Instance == null)
            yield return null;

        // Print AnalyticsSession ID
        // Debug.Log($"[AnalyticsTracker] AnalyticsSession ID: {AnalyticsSession.Instance.SessionId}");

        sess_id_no = AnalyticsSession.Instance.SessionId;
        // Debug.Log($"[AnalyticsTracker] Linked to global session: {sess_id_no}");
    }

    private string GetCurrentLevelName()
    {
        if (SongSelection.Instance != null && !string.IsNullOrEmpty(SongSelection.Instance.CurrentSongName))
            return SongSelection.Instance.CurrentSongName;

        return "Unknown_Level";
    }

    public void TrackHitAuto(string judgement)
    {
        string levelName = GetCurrentLevelName();
        StartCoroutine(SubmitFormAsync(levelName, judgement));
    }

    private IEnumerator SubmitFormAsync(string level, string judgement)
    {
        if (string.IsNullOrEmpty(sess_id_no))
        {
            Debug.LogWarning("[AnalyticsTracker] Session ID not ready yet. Skipping submission.");
            yield break;
        }

        string baseUrl = "https://docs.google.com/forms/d/e/1FAIpQLSduQhjDzhjBJSNnBAVLksbSpEjCKe_j3m6wTKyPQBGe5RFClw/formResponse";

        string sessionEntry = "entry.528639816=" + UnityWebRequest.EscapeURL(sess_id_no);
        string levelEntry   = "entry.1709020502=" + UnityWebRequest.EscapeURL(level);
        string hitEntry     = "entry.935876386=" + UnityWebRequest.EscapeURL(judgement);

        string fullUrl = $"{baseUrl}?{sessionEntry}&{levelEntry}&{hitEntry}";

        if (logSubmissions) Debug.Log($"[Analytics] → {fullUrl}");

        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("[Analytics] ✓ DATA SAVED!");
            else
                Debug.LogError($"[Analytics] ✗ {www.error}");
        }
    }

    public static void LogPerfect() => Instance?.TrackHitAuto("Perfect");
    public static void LogGood() => Instance?.TrackHitAuto("Good");
    public static void LogMiss() => Instance?.TrackHitAuto("Miss");
}
