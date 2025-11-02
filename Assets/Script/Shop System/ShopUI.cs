using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject root;   // Fullscreen panel root. If null, uses self.
    [SerializeField] private Button nextButton; // "Next" button

    private Action _onNext;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (!root) root = gameObject;
        if (root.activeSelf) root.SetActive(false);
        IsOpen = false;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNext);
        }
    }

    public void Show(Action onNext)
    {
        // ★ 兜底：就算 Awake 还没跑，也能给到 root
        if (!root) root = gameObject;

        if (IsOpen) return;
        IsOpen  = true;
        _onNext = onNext;

        // 置顶并显示
        root.transform.SetAsLastSibling();
        root.SetActive(true);

        // 暂停游戏
        Time.timeScale = 0f;

        // 可选：这里也可以再安全刷新一次 Continue 状态
        var panel = GetComponentInChildren<ShopPanel>(true);
        if (panel) panel.Refresh();
    }

    public void Hide()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (root) root.SetActive(false);
        Time.timeScale = 1f;

        var cb = _onNext; _onNext = null;
        cb?.Invoke();
    }

    private void HandleNext() => Hide();

    public void SetNextInteractable(bool on)
    {
        if (nextButton) nextButton.interactable = on;
    }
}