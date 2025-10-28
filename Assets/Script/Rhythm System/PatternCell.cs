using UnityEngine;
using UnityEngine.UI;

public class PatternCell : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform rect;
    [SerializeField] private Image icon;

    [Header("Judge Sprites")]
    [SerializeField] private Sprite hitSprite;   // 命中后
    [SerializeField] private Sprite missSprite;  // Miss 后

    private Sprite _initialSprite;
    private float  _initialScale = 1f;

    public RectTransform Rect => rect;
    public float InitialScale => _initialScale;

    void Awake()
    {
        if (icon) _initialSprite = icon.sprite;
        if (rect) _initialScale  = rect.localScale.x;
    }

    public bool ValidateSetup(bool log)
    {
        bool ok = (rect != null) && (icon != null) && (hitSprite != null) && (missSprite != null);
        if (log && !ok) Debug.LogError("[PatternCell] setup invalid.", this);
        return ok;
    }

    public void ResetVisual()
    {
        if (icon) icon.sprite = _initialSprite;
        if (rect) rect.localScale = new Vector3(_initialScale, _initialScale, 1f);
    }

    public void SetOk()
    {
        if (icon && hitSprite) icon.sprite = hitSprite;
    }

    public void SetWrong()
    {
        if (icon && missSprite) icon.sprite = missSprite;
    }

    public void SetScale(float s)
    {
        if (!rect) return;
        rect.localScale = new Vector3(s, s, 1f);
    }
}