using UnityEngine;

public class AnalyticsSession : MonoBehaviour
{
    public static AnalyticsSession Instance { get; private set; }
    public string SessionId { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        
        if (string.IsNullOrEmpty(SessionId))
        {
            int randomNum = Random.Range(10000, 999999);
            SessionId = $"sess_id_{randomNum}";
            Debug.Log($"[AnalyticsSession] Created global session: {SessionId}");
        }
    }
}
