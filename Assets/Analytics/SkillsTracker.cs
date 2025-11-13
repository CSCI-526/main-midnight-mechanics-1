using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class SkillsTracker : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool logSubmissions = true;

    private string sess_id_no;

    public static SkillsTracker Instance { get; private set; }

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
        Debug.Log($"[SkillsTracker] Linked to global session: {sess_id_no}");
    }

    // private string GetCurrentLevelName()
    // {
    //     if (SongSelection.Instance != null && !string.IsNullOrEmpty(SongSelection.Instance.CurrentSongName))
    //         return SongSelection.Instance.CurrentSongName;

    //     return SceneManager.GetActiveScene().name;
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


    public void LogEquippedSkills(List<string> skillNames)
    {
        if (skillNames == null || skillNames.Count == 0)
        {
            Debug.LogWarning("[SkillsTracker] No equipped skills to log.");
            return;
        }

        string levelName = GetCurrentLevelName();
        string joinedSkills = string.Join(",", skillNames);

        StartCoroutine(SubmitFormAsync(levelName, joinedSkills));
    }

    private IEnumerator SubmitFormAsync(string level, string skills)
    {
        if (string.IsNullOrEmpty(sess_id_no))
        {
            Debug.LogWarning("[SkillsTracker] Session ID not ready yet. Skipping submission.");
            yield break;
        }

        string baseUrl = "https://docs.google.com/forms/d/e/1FAIpQLSfHmoh3Ucqd_YTwbH-Gyij1BlhCMwpITqtYETGGZ9GKqcczYw/formResponse";

        string sessionEntry = "entry.1239327925=" + UnityWebRequest.EscapeURL(sess_id_no);
        string levelEntry   = "entry.130323180=" + UnityWebRequest.EscapeURL(level);
        string skillEntry   = "entry.1571228947=" + UnityWebRequest.EscapeURL(skills);

        string fullUrl = $"{baseUrl}?{sessionEntry}&{levelEntry}&{skillEntry}";

        if (logSubmissions)
            Debug.Log($"[SkillsTracker] → {fullUrl}");

        using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                Debug.Log("[SkillsTracker] ✓ Skill data saved!");
            else
                Debug.LogError($"[SkillsTracker] ✗ {www.error}");
        }
    }
}
