using System;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject root;   // 全屏面板（可不填，默认用自身）
    [SerializeField] private Button nextButton; // “Next” 按钮

    private Action _onNext;

    void Awake()
    {
        if (!root) root = gameObject;
        if (root.activeSelf) root.SetActive(false);
    }

    /// <summary>显示商店，并立刻停止所有正在播放的 AudioSource。</summary>
    public void Show(Action onNext)
    {
        _onNext = onNext;

        // ☆ 不新建脚本：这里直接停掉场景里所有正在播放的 AudioSource
        var sources = FindObjectsOfType<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            var s = sources[i];
            if (s && s.isPlaying) s.Stop();
        }

        // 打开 UI（置顶避免被其它 UI 覆盖）
        root.transform.SetAsLastSibling();
        root.SetActive(true);
        Time.timeScale = 0f;

        // 每次显示时都重新绑定一次，避免重复监听
        if (nextButton)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNext);
        }

        Debug.Log("[ShopUI] Show → audio stopped, game paused");
    }

    void HandleNext()
    {
        if (root) root.SetActive(false);
        Time.timeScale = 1f;

        var cb = _onNext; _onNext = null;
        cb?.Invoke();
    }
}