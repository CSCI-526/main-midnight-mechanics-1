using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ViewCount : MonoBehaviour
{
    [Header("References (auto-assigned)")]
    [SerializeField] private ViewerSystem viewerSystem;
    [SerializeField] private LevelRunner levelRunner;

    [Header("Settings")]
    [SerializeField] private float logInterval = 1f; 

    // Google Form Config
    private const string FORM_URL = "https://docs.google.com/forms/d/e/1FAIpQLSe2_CKXNePDGGRJtB_LqcWSMatGSD4ZC-EfBnVLQisHfM9gmQ/formResponse";
    private const string ENTRY_SESS_ID_NO    = "entry.899778555"; // sess_id_no
    private const string ENTRY_SONG_NAME     = "entry.147327817"; // Song Name
    private const string ENTRY_VIEWER_COUNT  = "entry.482687153"; // Viewer Count

    private string sess_id_no;
    private string songName;
    private bool running;
    private bool ended;
    private int lastValidViewers;

    void Start()
    {
        // Auto-find references
        if (!viewerSystem)
            viewerSystem = FindFirstObjectByType<ViewerSystem>();
        if (!levelRunner)
            levelRunner = FindFirstObjectByType<LevelRunner>();

        // Subscribe to events
        if (levelRunner != null)
            levelRunner.OnLevelEnded += HandleLevelEnded;
        if (viewerSystem != null)
            viewerSystem.OnDepleted += HandleGameOver;

        // Get shared sess_id_no from AnalyticsSession
        if (AnalyticsSession.Instance != null)
        {
            sess_id_no = AnalyticsSession.Instance.SessionId;
            //Debug.Log($"[ViewerAnalytics] Using global sess_id_no: {sess_id_no}");
        }
        else
        {
            // Fallback if AnalyticsSession missing
            int randomNum = Random.Range(10000, 999999);
            sess_id_no = $"sess_id_{randomNum}";
            //Debug.LogWarning($"[ViewerAnalytics] No AnalyticsSession found! Generated fallback ID: {sess_id_no}");
        }

        // Get song name
        if (SongSelection.Instance != null && !string.IsNullOrEmpty(SongSelection.Instance.CurrentSongName))
            songName = SongSelection.Instance.CurrentSongName;
        else
            songName = SceneManager.GetActiveScene().name;

        //Debug.Log($"[ViewerAnalytics] Song: {songName}");

        StartCoroutine(LogViewerTrend());
    }

    IEnumerator LogViewerTrend()
    {
        running = true;

        while (running)
        {
            yield return new WaitForSecondsRealtime(logInterval);
            if (ended) yield break;

            int current = viewerSystem ? viewerSystem.Current : 0;
            if (current > 0)
                lastValidViewers = current;

            SendViewerMetric(current);
        }
    }

    void HandleLevelEnded()
    {
        if (ended) return;
        ended = true;

        int finalViewers = viewerSystem ? viewerSystem.Current : lastValidViewers;
        Debug.Log($"[ViewerAnalytics] ✅ Level ended | Final Viewers = {finalViewers}");
        SendViewerMetric(finalViewers);
        StopLogging();
    }

    void HandleGameOver()
    {
        if (ended) return;
        ended = true;

        int finalViewers = lastValidViewers > 0 ? lastValidViewers : 0;
        Debug.Log($"[ViewerAnalytics] ☠ Game Over | Final Viewers = {finalViewers}");
        SendViewerMetric(finalViewers);
        StopLogging();
    }

    void SendViewerMetric(int viewerCount)
    {
        WWWForm form = new WWWForm();
        form.AddField(ENTRY_SESS_ID_NO, sess_id_no);
        form.AddField(ENTRY_SONG_NAME, songName);
        form.AddField(ENTRY_VIEWER_COUNT, viewerCount.ToString());
        StartCoroutine(PostForm(form));
    }

    IEnumerator PostForm(WWWForm form)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(FORM_URL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("[ViewerAnalytics] ❌ Failed: " + www.error);
        }
    }

    public void StopLogging()
    {
        if (!running) return;
        running = false;
        Debug.Log("[ViewerAnalytics] Logging stopped.");
    }

    void OnDestroy()
    {
        if (levelRunner)
            levelRunner.OnLevelEnded -= HandleLevelEnded;
        if (viewerSystem)
            viewerSystem.OnDepleted -= HandleGameOver;
    }
}
