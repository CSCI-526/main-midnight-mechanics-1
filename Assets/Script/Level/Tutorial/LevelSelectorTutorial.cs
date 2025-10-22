using UnityEngine;

/// <summary>
/// Click handler to open TutorialOverlay from Level Selector.
/// </summary>
public sealed class LevelSelectorTutorial : MonoBehaviour
{
    [SerializeField] private TutorialOverlay overlay;

    private void Awake()
    {
        if (!overlay) overlay = FindObjectOfType<TutorialOverlay>(true);
    }

    public void OnClickOpenTutorial()
    {
        if (overlay != null) overlay.Show();
    }
}