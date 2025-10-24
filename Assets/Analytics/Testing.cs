using UnityEngine;

public class Testing : MonoBehaviour
{
    void Start()
    {
        var analytics = Object.FindFirstObjectByType<Analytics>();
        if (analytics == null)
        {
            Debug.LogError("GoogleFormAnalytics not found!");
            return;
        }

        Debug.Log("Testing send...");
        analytics.LogAction("TestSession", 1, "Space", true);
    }
}
