using UnityEngine;

public class GreenBoxTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform hitIndicator;   
    [SerializeField] private PlayerInputTracker tracker;

    private RectTransform zoneRect;
    private bool isInside = false;

    void Awake()
    {
        zoneRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (hitIndicator == null || tracker == null || zoneRect == null)
            return;

        
        Rect zoneScreenRect = GetScreenRect(zoneRect);
        Rect indicatorScreenRect = GetScreenRect(hitIndicator);

        bool overlap = zoneScreenRect.Overlaps(indicatorScreenRect);

        if (overlap && !isInside)
        {
            tracker.EnterGreenZone();
            isInside = true;
            Debug.Log("[Trigger ✅] ENTER zone");
        }
        else if (!overlap && isInside)
        {
            tracker.ExitGreenZone();
            isInside = false;
            Debug.Log("[Trigger ❌] EXIT zone");
        }
    }

    private Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return new Rect(corners[0].x, corners[0].y,
                        corners[2].x - corners[0].x,
                        corners[2].y - corners[0].y);
    }
}
