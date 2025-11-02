using UnityEngine;
using UnityEngine.EventSystems;

public sealed class DropZoneOwnedSlot : MonoBehaviour, IDropHandler
{
    [SerializeField, Min(0)] private int slotIndex = 0;
    [SerializeField] private ShopPanel panel;

    public int SlotIndex => slotIndex;

    public void OnDrop(PointerEventData eventData)
    {
        if (!panel) return;
        var card = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableSkillCard>() : null;
        if (!card) return;
        panel.HandleDropToOwned(this, card);
    }
}