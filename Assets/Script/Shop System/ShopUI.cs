using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen shop panel. Shows on level-end, pauses the game, and resumes on Next.
/// No audio management here.
/// </summary>
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

    /// <summary>Show the shop UI and pause the game.</summary>
    public void Show(Action onNext)
    {
        if (IsOpen) return; // prevent double-open
        IsOpen  = true;
        _onNext = onNext;

        root.transform.SetAsLastSibling();
        root.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>Hide the shop UI, resume the game, then invoke the callback.</summary>
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