using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Analytics : MonoBehaviour
{
    private static Analytics instance;

    // Replace this with your form's POST URL
    private const string FORM_URL = "https://docs.google.com/forms/d/e/1FAIpQLSe2m1XGXLreR-V6pJw09mygq-2-t_K-aLXLAzwQdDti4YPryQ/formResponse";

// Replace these with your actual entry IDs
    private const string ENTRY_SESSION = "entry.1502920223";
    private const string ENTRY_LEVEL   = "entry.593008345";
    private const string ENTRY_KEY     = "entry.611574249";
    private const string ENTRY_SUCCESS = "entry.782525270";


    void Awake()
    {
        // Singleton pattern: ensure only one instance exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static Analytics Instance
    {
        get
        {
            // Auto-create an AnalyticsManager if missing
            if (instance == null)
            {
                GameObject go = new GameObject("AnalyticsManager");
                instance = go.AddComponent<Analytics>();
                DontDestroyOnLoad(go);
                Debug.Log("Created new AnalyticsManager at runtime");
            }
            return instance;
        }
    }

    public void LogAction(string sessionId, int level, string key, bool success)
    {
        StartCoroutine(Send(sessionId, level, key, success));
    }

    private IEnumerator Send(string sessionId, int level, string key, bool success)
    {
        WWWForm form = new WWWForm();
        form.AddField(ENTRY_SESSION, sessionId);
        form.AddField(ENTRY_LEVEL, level.ToString());
        form.AddField(ENTRY_KEY, key);
        form.AddField(ENTRY_SUCCESS, success ? "TRUE" : "FALSE");

        using (UnityWebRequest www = UnityWebRequest.Post(FORM_URL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("Google Form send failed: " + www.error);
            else
                Debug.Log("Analytics sent: " + key + " | Success: " + success);
        }
    }
}
