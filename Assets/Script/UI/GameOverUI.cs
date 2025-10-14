using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject root;     // 弹窗/全屏（可不填，默认用自身）
    [SerializeField] private Button backButton;   // “Back to Select”
    [SerializeField] private SceneFlow sceneFlow; // 可留空，找不到就用 SceneManager

    void Awake()
    {
        if (!root) root = gameObject;
        if (root.activeSelf) root.SetActive(false);
        if (!sceneFlow) sceneFlow = FindObjectOfType<SceneFlow>(true);
    }

    /// <summary>显示 Game Over，并立刻停止所有正在播放的 AudioSource。</summary>
    public void Show()
    {
        // ☆ 不新建脚本：这里直接停掉场景里所有正在播放的 AudioSource
        var sources = FindObjectsOfType<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            var s = sources[i];
            if (s && s.isPlaying) s.Stop();
        }

        root.transform.SetAsLastSibling();
        root.SetActive(true);
        Time.timeScale = 0f;

        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(HandleBack);
        }

        Debug.Log("[GameOverUI] Show → audio stopped, game paused");
    }

    void HandleBack()
    {
        Time.timeScale = 1f;
        if (sceneFlow) sceneFlow.LoadLevelSelector();
        else SceneManager.LoadScene("LevelSelector", LoadSceneMode.Single);
    }
}