using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LevelClearUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root; // 面板根节点（默认 Inactive）

    [Header("Texts (5 slots)")]
    [SerializeField] private TMP_Text donateText;
    [SerializeField] private TMP_Text topViewersText;
    [SerializeField] private TMP_Text perfectText;
    [SerializeField] private TMP_Text goodText;
    [SerializeField] private TMP_Text missText;

    [Header("Buttons")]
    [SerializeField] private Button backToSelectButton;

    float _prevTimeScale = 1f;
    Action _onBack;

    void Awake()
    {
        if (!root) root = gameObject;
        if (root.activeSelf) root.SetActive(false);

        if (backToSelectButton)
        {
            backToSelectButton.onClick.RemoveAllListeners();
            backToSelectButton.onClick.AddListener(() =>
            {
                Hide();
                _onBack?.Invoke();
            });
        }
    }

    public void Show(SessionStats stats, Action onBackToSelect)
    {
        _onBack = onBackToSelect;

        if (donateText)     donateText.SetText($"${(stats != null ? stats.DonateUSD : 0):N0}");
        if (topViewersText) topViewersText.SetText((stats != null ? stats.TopViewers : 0).ToString());
        if (perfectText)    perfectText.SetText((stats != null ? stats.PerfectCount : 0).ToString());
        if (goodText)       goodText.SetText((stats != null ? stats.GoodCount : 0).ToString());
        if (missText)       missText.SetText((stats != null ? stats.MissCount : 0).ToString());

        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        root.transform.SetAsLastSibling();
        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
        Time.timeScale = _prevTimeScale;
    }

    void OnDisable()
    {
        // 防止挂起
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}