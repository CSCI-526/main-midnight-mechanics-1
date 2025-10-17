// using Firebase;
// using Firebase.Analytics;
// using UnityEngine;

// public class FirebaseManager : MonoBehaviour
// {
//     public static FirebaseManager Instance { get; private set; }

//     void Awake()
//     {
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);

//         FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
//         {
//             var status = task.Result;
//             if (status == DependencyStatus.Available)
//                 Debug.Log("Firebase ready!");
//             else
//                 Debug.LogError("Firebase dependencies missing: " + status);
//         });
//     }

//     public void LogEvent(string eventName, string paramName = null, string paramValue = null)
//     {
//         if (!string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(paramValue))
//             FirebaseAnalytics.LogEvent(eventName, paramName, paramValue);
//         else
//             FirebaseAnalytics.LogEvent(eventName);
//     }
// }
