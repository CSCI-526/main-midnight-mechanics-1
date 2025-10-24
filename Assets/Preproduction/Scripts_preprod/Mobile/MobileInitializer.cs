using UnityEngine;
using UnityEngine.SceneManagement;

public class MobileInitializer : MonoBehaviour
{
    [SerializeField] private GameObject mobileControlsPrefab;

    void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        TrySpawnControls();
#elif UNITY_WEBGL
        if (Input.touchSupported)
            TrySpawnControls();
#endif
    }

    private void TrySpawnControls()
    {
        if (SceneManager.GetActiveScene().name != "PlayScene") return;

        if (mobileControlsPrefab != null)
        {
            GameObject instance = Instantiate(mobileControlsPrefab);
            DontDestroyOnLoad(instance); 
        }
        else
        {
            Debug.LogWarning("Mobile Controls Prefab is not assigned!");
        }
    }
}
