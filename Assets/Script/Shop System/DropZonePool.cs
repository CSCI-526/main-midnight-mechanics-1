using UnityEngine;
using UnityEngine.EventSystems;

public sealed class DropZonePool : MonoBehaviour, IDropHandler
{
    [SerializeField] private ShopPanel panel;

    public void OnDrop(PointerEventData eventData)
    {
        if (!panel) return;
        var card = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableSkillCard>() : null;
        if (!card) return;
        panel.HandleDropToPool(card);
    }
}