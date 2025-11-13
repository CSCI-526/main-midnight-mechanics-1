using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DoubleNoteAnalyticsTracker : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logSubmissions = true;

    private string sess_id_no;

    public static DoubleNoteAnalyticsTracker Instance { get; private set; }

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

        sess_id_no = AnalyticsSession.Instance.SessionId;
        Debug.Log($"[DoubleNoteAnalytics] Linked to global session: {sess_id_no}");
    }

    // private string GetCurrentLevelName()
    // {
    //     if (SongSelection.Instance != null && !string.IsNullOrEmpty(SongSelection.Instance.CurrentSongName))
    //         return SongSelection.Instance.CurrentSongName;

    //     return "Unknown_Level";
    // }
    private string GetCurrentLevelName()
    {
        // If a valid song has been selected (Challenge songs)
        if (SongSelection.Instance != null &&
            !string.IsNullOrEmpty(SongSelection.Instance.CurrentSongName) &&
            SongSelection.Instance.CurrentSongName != "Unknown")
        {
            return SongSelection.Instance.CurrentSongName;
        }

        // Otherwise, it's the Tutorial level
        return "Tutorial";
    }

    public void TrackDoubleNoteHit(string judgement)
    {
        string levelName = GetCurrentLevelName();
        StartCoroutine(SubmitFormAsync(levelName, judgement));
    }

    private IEnumerator SubmitFormAsync(string level, string judgement)
    {
        if (string.IsNullOrEmpty(sess_id_no))
        {
            Debug.LogWarning("[DoubleNoteAnalytics] Session ID not ready yet. Skipping submission.");
            yield break;
        }

        string baseUrl = "https://docs.google.com/forms/d/e/1FAIpQLScgl9ZLhQKPwL5NDmGDa0p3J7ZHgGZrjW--j-IFeqSrRzDu6g/formResponse";

        string sessionEntry = "entry.1277239261=" + UnityWebRequest.EscapeURL(sess_id_no);
        string levelEntry   = "entry.1203030886=" + UnityWebRequest.EscapeURL(level);
        string hitEntry     = "entry.259975037=" + UnityWebRequest.EscapeURL(judgement);

        string fullUrl = $"{baseUrl}?{sessionEntry}&{levelEntry}&{hitEntry}";

        if (logSubmissions) Debug.Log($"[DoubleNoteAnalytics] → {fullUrl}");

        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("[DoubleNoteAnalytics] ✓ DATA SAVED!");
            else
                Debug.LogError($"[DoubleNoteAnalytics] ✗ {www.error}");
        }
    }

    public static void LogPerfect() => Instance?.TrackDoubleNoteHit("Perfect");
    public static void LogGood() => Instance?.TrackDoubleNoteHit("Good");
    public static void LogMiss() => Instance?.TrackDoubleNoteHit("Miss");
}
