using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public sealed class DraggableSkillCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text title;
    [SerializeField] private CanvasGroup cg;

    [Header("Runtime")]
    [SerializeField] private SkillLibrary.ActiveSkillId skillId;

    private RectTransform _rt;
    private Transform _originalParent;
    private ShopPanel _panel;
    private bool _dropped;
    private Transform _dragLayer;

    public SkillLibrary.ActiveSkillId Id => skillId;

    public void Init(ShopPanel panel,
                     SkillLibrary.ActiveSkillId id,
                     Sprite sp,
                     string display,
                     Transform dragLayer)
    {
        _panel = panel;
        skillId = id;
        if (icon)  icon.sprite = sp;
        if (title) title.text = display;
        _dragLayer = dragLayer;
    }

    void Awake()
    {
        _rt = transform as RectTransform;
        if (!cg) cg = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dropped = false;
        _originalParent = transform.parent;
        cg.blocksRaycasts = false; // 允许被 DropZone 接收
        if (_dragLayer) transform.SetParent(_dragLayer, worldPositionStays: false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _rt.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;
        if (!_dropped)
        {
            // 没有投递到任何有效区：还原
            transform.SetParent(_originalParent, worldPositionStays: false);
        }
    }

    // 由 ShopPanel 在投递成功后调用
    public void MarkDroppedTo(Transform newParent)
    {
        _dropped = true;
        transform.SetParent(newParent, worldPositionStays: false);
    }
}
