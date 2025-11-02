using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[DisallowMultipleComponent]
public sealed class DraggableSkillCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Skill")]
    [SerializeField] private Game.Skills.ActiveSkillBase skill;
    [SerializeField] private string     overrideTitle;
    [SerializeField, TextArea] private string     overrideDescription;
    [SerializeField] private Sprite     overrideIcon;

    [Header("UI")]
    [SerializeField] private Image     icon;
    [SerializeField] private TMP_Text  title;       // 不用显示也没关系
    [SerializeField] private TMP_Text  description; // 同上

    [Header("Hover / Tooltip (可选)")]
    [SerializeField] private Image          hoverRing;
    [SerializeField] private RectTransform  tooltipRoot;
    [SerializeField] private TMP_Text       tooltipTitle;
    [SerializeField] private TMP_Text       tooltipDesc;

    [Header("Drag")]
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private Transform   dragLayer;     // 建议：Canvas 下全屏空物体
    [SerializeField] private Canvas      rootCanvas;    // 为空自动找
    [SerializeField, Range(0.6f,1f)] private float dragScale = 0.92f;
    [SerializeField] private bool        bringToFrontOnDrag = true;

    [Header("Layout Fit")]
    [SerializeField] private bool fitToParentWhenInSlot = true; // 进槽填满

    [Header("Panel Ref")]
    [SerializeField] private ShopPanel shopPanel; // 留空自动找

    // runtime
    RectTransform _rt;
    Transform _originalParent;
    int _originalSiblingIndex;
    Vector3 _originalScale;
    bool _dropped;
    bool _dragging;

    // “家”（SellerPanel下的初始格）
    Transform _homeParent;
    int _homeIndex;

    // 初始布局（回家用）
    Vector2 _origSizeDelta, _origAnchorMin, _origAnchorMax, _origPivot, _origAnchoredPos;

    // 运行时兜底拖拽层缓存
    Transform _runtimeDragOverlay;

    public Game.Skills.ActiveSkillBase Skill => skill;

    void Awake()
    {
        _rt = (RectTransform)transform;
        if (!cg) cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (!shopPanel)  shopPanel  = Object.FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);

        SetupVisual();
        EnsureDragLayerOrCreateOverlay(false);

        // 记录初始布局&“家”
        _origSizeDelta   = _rt.sizeDelta;
        _origAnchorMin   = _rt.anchorMin;
        _origAnchorMax   = _rt.anchorMax;
        _origPivot       = _rt.pivot;
        _origAnchoredPos = _rt.anchoredPosition;

        _homeParent = transform.parent;
        _homeIndex  = transform.GetSiblingIndex();

        SetHover(false);
    }

    void OnValidate()
    {
        if (!Application.isPlaying) SetupVisual();
    }

    void SetupVisual()
    {
        string ttl  = !string.IsNullOrEmpty(overrideTitle) ? overrideTitle : (skill ? skill.name : "Skill");
        string desc = !string.IsNullOrEmpty(overrideDescription) ? overrideDescription : "";

        if (tooltipTitle) tooltipTitle.text = ttl;
        if (tooltipDesc)  tooltipDesc.text  = desc;

        if (icon && overrideIcon) icon.sprite = overrideIcon;

        if (title)       title.text       = ttl;
        if (description) description.text = desc;
    }

    // ---------- Drag overlay 准备 ----------
    void EnsureDragLayerOrCreateOverlay(bool forceCreate)
    {
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();

        // 优先使用手动指定的 dragLayer（且必须处于同一 Canvas 树）
        if (!forceCreate && dragLayer && rootCanvas && dragLayer.GetComponentInParent<Canvas>() == rootCanvas)
            return;

        // 找找是否已有名为 DragOverlay 的节点（同一 Canvas 下）
        if (!forceCreate && rootCanvas)
        {
            var found = rootCanvas.transform.Find("DragOverlay");
            if (found)
            {
                dragLayer = found;
                return;
            }
        }

        // 运行时兜底：创建一个全屏的 DragOverlay（只建一次）
        if (!_runtimeDragOverlay && rootCanvas)
        {
            var go = new GameObject("DragOverlay_Runtime", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rootCanvas.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            _runtimeDragOverlay = rt;

            // 放到最顶
            _runtimeDragOverlay.SetAsLastSibling();
        }
        dragLayer = _runtimeDragOverlay ? _runtimeDragOverlay : dragLayer;
    }

    // ---------- Hover ----------
    public void OnPointerEnter(PointerEventData _) => SetHover(true);
    public void OnPointerExit (PointerEventData _) { if (!_dragging) SetHover(false); }

    void SetHover(bool on)
    {
        if (hoverRing)   hoverRing.enabled = on;
        if (tooltipRoot) tooltipRoot.gameObject.SetActive(on);
    }

    // ---------- Drag ----------
    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = true;
        _dropped = false;

        _originalParent = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalScale = transform.localScale;

        SetHover(false);
        cg.blocksRaycasts = false;

        // 确保有可见的顶层拖拽父物体
        EnsureDragLayerOrCreateOverlay(false);

        // ★ 保留世界坐标换父，避免“消失”
        var parentForDrag = dragLayer ? dragLayer : (Transform)rootCanvas.transform;
        transform.SetParent(parentForDrag, true);

        // 保证拖拽层在最顶
        parentForDrag.SetAsLastSibling();
        if (bringToFrontOnDrag) transform.SetAsLastSibling();

        // 视觉缩放
        transform.localScale = _originalScale * dragScale;

        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData) => MoveToPointer(eventData);

    void MoveToPointer(PointerEventData eventData)
    {
        if (!rootCanvas) { _rt.position = eventData.position; return; }

        Vector2 local;
        var cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)rootCanvas.transform, eventData.position, cam, out local))
        {
            _rt.position = rootCanvas.transform.TransformPoint(local);
        }
        else
        {
            _rt.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData _)
    {
        _dragging = false;
        cg.blocksRaycasts = true;

        if (!_dropped)
        {
            var fromZone = _originalParent ? _originalParent.GetComponent<ShopDropZone>() : null;
            if (fromZone && fromZone.ZoneKind == ShopDropZone.Kind.Slot)
            {
                // 槽里起拖→空白松手：卸下并回家
                var panel = shopPanel ? shopPanel : Object.FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
                panel?.ForceReturnToSeller(this);
            }
            else
            {
                // 卖家面板起拖→空白松手：回家
                SnapToHome();
            }
        }

        transform.localScale = _originalScale;
    }

    // 进槽成功
    public void MarkDroppedTo(Transform newParent, bool intoSlot)
    {
        _dropped = true;
        transform.SetParent(newParent, false);

        if (intoSlot && fitToParentWhenInSlot)
            FitToParentRect();
        else
            RestoreLayoutToOriginalRect();

        transform.SetAsLastSibling();
    }

    // 回家
    public void SnapToHome()
    {
        _dropped = true;
        if (_homeParent)
        {
            transform.SetParent(_homeParent, false);
            transform.SetSiblingIndex(_homeIndex);
            RestoreLayoutToOriginalRect();
        }
    }

    void FitToParentRect()
    {
        _rt.anchorMin = Vector2.zero;
        _rt.anchorMax = Vector2.one;
        _rt.pivot     = new Vector2(0.5f, 0.5f);
        _rt.sizeDelta = Vector2.zero;
        _rt.anchoredPosition = Vector2.zero;
    }

    void RestoreLayoutToOriginalRect()
    {
        _rt.anchorMin = _origAnchorMin;
        _rt.anchorMax = _origAnchorMax;
        _rt.pivot     = _origPivot;
        _rt.sizeDelta = _origSizeDelta;
        _rt.anchoredPosition = _origAnchoredPos; // ★ 回到最初手摆的位置
    }

    public bool HasDragLayer() => dragLayer != null;
    public void SetDragLayer(Transform t) { dragLayer = t; }
}
