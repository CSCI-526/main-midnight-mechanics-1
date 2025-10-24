using System;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button backToSelectButton; 
    [SerializeField] private Button retryButton;

    private Action _onBack;
    private Action _onRetry;

    void Awake()
    {
        if (!root) root = gameObject;
        if (root.activeSelf) root.SetActive(false);

        if (backToSelectButton)
        {
            backToSelectButton.onClick.RemoveAllListeners();
            backToSelectButton.onClick.AddListener(() => { Hide(); _onBack?.Invoke(); });
        }
        if (retryButton)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => { Hide(); _onRetry?.Invoke(); });
        }
    }

    public void Show(Action onBackToSelect, Action onRetry = null)
    {
        _onBack  = onBackToSelect;
        _onRetry = onRetry;
        root.transform.SetAsLastSibling();
        root.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        root.SetActive(false);
        Time.timeScale = 1f;
    }

    void OnDisable()
    {
        // 安全防挂起
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}