using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class ShopDropZone : MonoBehaviour, IDropHandler
{
    public enum Kind { Pool, Slot }

    [Header("Assign in Inspector (可被 ShopPanel 自动覆盖)")]
    [SerializeField] private Kind kind = Kind.Pool;
    [SerializeField, Min(0)] private int slotIndex = 0; // Pool 时无用
    [SerializeField] private ShopPanel panel;

    public Kind ZoneKind => kind;
    public int  SlotIndex => slotIndex;

    /// <summary>由 ShopPanel 统一调用，确保 Kind / Panel 正确。</summary>
    public void Configure(ShopPanel p, Kind k)
    {
        panel = p;
        kind  = k;

        // 友好提示：确保此节点可被点击命中
        var img = GetComponent<Image>();
        if (!img)
        {
            Debug.LogWarning($"[ShopDropZone] '{name}' 建议挂一个 Image（可透明，Raycast Target 勾上），否则可能收不到 OnDrop。", this);
        }
        else if (!img.raycastTarget)
        {
            Debug.LogWarning($"[ShopDropZone] '{name}' 的 Image 没勾 Raycast Target，可能收不到 OnDrop。", this);
        }
    }

    private void Awake()
    {
        // 若忘了在 Inspector 里绑 Panel，尝试自动查找（只作为兜底）
        if (!panel)
        {
            panel = GetComponentInParent<ShopPanel>();
            if (!panel)
                panel = Object.FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableSkillCard>() : null;

        // 强化日志：看到这行，说明 OnDrop 已经触发
        Debug.Log($"[ShopDropZone] OnDrop@{name} kind={kind}, panel={(panel?panel.name:"NULL")}, card={(card?card.name:"NULL")}");

        if (!panel || !card) return;
        panel.HandleDrop(this, card);
    }
}