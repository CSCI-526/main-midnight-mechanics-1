using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class AnalyticsTracker : MonoBehaviour
{
    [Header("Google Form")]
    // [SerializeField] private string googleFormUrl = "https://docs.google.com/forms/d/e/1FAIpQLSdrfZmZKMYr9gtkkPsWzEIMtZ4j2NWLxYOGup3g9ncUpb8y3g/viewform?usp=pp_url";
    // Google Form URL:"
    // [SerializeField] private string googleFormUrl ="https://docs.google.com/forms/d/e/1FAIpQLSc5TmraNiYCBrhiS_l78VQJMfpLDIqUNMhjeleeQe9XuzzQMg/formResponse";
    
    [Header("Session ID")]
    // [SerializeField] private string sessionId = "test_session_123"; // Auto-generated in Awake
    private string sessionId;
    
    [Header("Debug")]
    [SerializeField] private bool logSubmissions = true;
    
    public static AnalyticsTracker Instance { get; private set; }
    
    // void Awake()
    // {
    //     if (Instance && Instance != this) { Destroy(gameObject); return; }
    //     Instance = this;
    //     DontDestroyOnLoad(gameObject);
        
    //     // Generate unique session ID
    //     if (string.IsNullOrEmpty(sessionId))
    //         sessionId = $"sess_{SystemInfo.deviceUniqueIdentifier}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
    // }
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Always generate fresh ID
        sessionId = $"sess_{SystemInfo.deviceUniqueIdentifier}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
    }
    
    public void TrackHit(string levelName, string judgement)
    {
        StartCoroutine(SubmitFormAsync(levelName, judgement));
    }
    
IEnumerator SubmitFormAsync(string level, string judgement)
{
    string baseUrl = "https://docs.google.com/forms/d/e/1FAIpQLSc5TmraNiYCBrhiS_l78VQJMfpLDIqUNMhjeleeQe9XuzzQMg/formResponse";
    
    string sessionEntry = "entry.688029435=" + UnityWebRequest.EscapeURL(sessionId);
    string levelEntry   = "entry.2117415998=" + UnityWebRequest.EscapeURL(level);
    string hitEntry     = "entry.2099938677=" + UnityWebRequest.EscapeURL(judgement);
    
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
    
    // Public API for easy access
    public static void LogPerfect(string levelName) => Instance?.TrackHit(levelName, "Perfect");
    public static void LogGood(string levelName) => Instance?.TrackHit(levelName, "Good");
    public static void LogMiss(string levelName) => Instance?.TrackHit(levelName, "Miss");
}



// using UnityEngine;
// using UnityEngine.Networking;
// using System.Collections;
// using System;

// public class AnalyticsTracker : MonoBehaviour
// {
//     [Header("Google Form")]
//     // FIXED: Removed commented-out old URL for cleanliness
//     [SerializeField] private string googleFormUrl = "https://docs.google.com/forms/d/e/1FAIpQLSc5TmraNiYCBrhiS_l78VQJMfpLDIqUNMhjeleeQe9XuzzQMg/formResponse";
    
//     [Header("Session ID")]
//     // [SerializeField] private string sessionId = "test_session_123"; // Auto-generated in Awake
//     private string sessionId;
    
//     [Header("Debug")]
//     [SerializeField] private bool logSubmissions = true;
    
//     public static AnalyticsTracker Instance { get; private set; }
    
//     void Awake()
//     {
//         if (Instance && Instance != this) { Destroy(gameObject); return; }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);
        
//         // Always generate fresh ID
//         sessionId = $"sess_{SystemInfo.deviceUniqueIdentifier}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
//     }
    
//     public void TrackHit(string levelName, string judgement)
//     {
//         // FIXED: Added null check for safety (prevents errors if called too early)
//         if (Instance == null) 
//         {
//             Debug.LogWarning("[Analytics] Instance not ready – skipping submission.");
//             return;
//         }
//         StartCoroutine(SubmitFormAsync(levelName, judgement));
//     }
    
//     // FIXED: Made method private (no need for public access)
//     private IEnumerator SubmitFormAsync(string level, string judgement)
//     {
//         // FIXED: Use the serialized URL (not hardcoded) – easier to change in Inspector
//         string baseUrl = googleFormUrl;
//         if (string.IsNullOrEmpty(baseUrl))
//         {
//             Debug.LogError("[Analytics] Google Form URL is empty!");
//             yield break;
//         }
        
//         string sessionEntry = "entry.688029435=" + UnityWebRequest.EscapeURL(sessionId);
//         string levelEntry   = "entry.2117415998=" + UnityWebRequest.EscapeURL(level ?? "Unknown");
//         string hitEntry     = "entry.2099938677=" + UnityWebRequest.EscapeURL(judgement ?? "Unknown");
        
//         string fullUrl = $"{baseUrl}?{sessionEntry}&{levelEntry}&{hitEntry}";
        
//         if (logSubmissions) Debug.Log($"[Analytics] → {fullUrl}");
        
//         using (UnityWebRequest www = UnityWebRequest.Get(fullUrl))
//         {
//             www.timeout = 3;
//             yield return www.SendWebRequest();
            
//             // FIXED: More detailed success check (200-299 status codes)
//             if (www.result == UnityWebRequest.Result.Success && www.responseCode >= 200 && www.responseCode < 300)
//                 Debug.Log("[Analytics] ✓ DATA SAVED!");
//             else
//                 Debug.LogError($"[Analytics] ✗ {www.error} (Code: {www.responseCode}) | URL: {fullUrl}");
//         }
//     }
    
//     // Public API for easy access
//     public static void LogPerfect(string levelName) => Instance?.TrackHit(levelName, "Perfect");
//     public static void LogGood(string levelName) => Instance?.TrackHit(levelName, "Good");
//     public static void LogMiss(string levelName) => Instance?.TrackHit(levelName, "Miss");
// }