using UnityEngine;

public class MobileControls : MonoBehaviour
{
    public RectTransform arrowsContainer; // Up, Down, Left, Right buttons
    public RectTransform hitButton;

    [Header("Distance from bottom in pixels")]
    public float portraitBottomOffset = 50f;
    public float landscapeBottomOffset = 30f;

    [Header("Horizontal offset from sides")]
    public float portraitArrowsX = 100f;
    public float portraitHitX = -100f;
    public float landscapeArrowsX = 50f;
    public float landscapeHitX = -50f;

    private ScreenOrientation lastOrientation;

    void Start()
    {
        lastOrientation = Screen.orientation;
        RepositionControls();
    }

    void Update()
    {
        if (Screen.orientation != lastOrientation)
        {
            lastOrientation = Screen.orientation;
            RepositionControls();
        }
    }

    void RepositionControls()
    {
        bool isPortrait = Screen.height >= Screen.width;

        if (isPortrait)
        {
            arrowsContainer.anchoredPosition = new Vector2(portraitArrowsX, portraitBottomOffset);
            hitButton.anchoredPosition = new Vector2(portraitHitX, portraitBottomOffset);
        }
        else
        {
            arrowsContainer.anchoredPosition = new Vector2(landscapeArrowsX, landscapeBottomOffset);
            hitButton.anchoredPosition = new Vector2(landscapeHitX, landscapeBottomOffset);
        }
    }
}
