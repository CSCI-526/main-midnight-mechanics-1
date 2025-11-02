using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static SkillLibrary;
using System.Linq;

public sealed class ShopPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SkillLibrary library;
    [SerializeField] private PlayerSkills playerSkills;
    [SerializeField] private ShopUI       shopUI;

    [Header("UI Roots")]
    [SerializeField] private RectTransform poolContent;   // 左侧网格（最多8个）
    [SerializeField] private DropZonePool  poolDropZone;  // 左侧投递区
    [SerializeField] private DropZoneOwnedSlot[] ownedSlots = new DropZoneOwnedSlot[4]; // 右侧4槽位

    [Header("Prefabs")]
    [SerializeField] private DraggableSkillCard cardPrefab;

    [Header("Drag Layer")]
    [SerializeField] private Transform dragLayer; // 通常是最上层Canvas下的一个空物体

    [Header("Limits")]
    [SerializeField, Min(1)] private int maxPoolCards = 8;

    // runtime
    private readonly Dictionary<ActiveSkillId, DraggableSkillCard> _cards = new();

    void Awake()
    {
        if (!library)      library      = FindObjectOfType<SkillLibrary>(true);
        if (!playerSkills) playerSkills = FindObjectOfType<PlayerSkills>(true);
        if (!shopUI)       shopUI       = FindObjectOfType<ShopUI>(true);

        if (!poolDropZone) poolDropZone = FindObjectOfType<DropZonePool>(true);
        if (poolDropZone)  typeof(DropZonePool).GetField("panel",
           System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.SetValue(poolDropZone, this);

        // 将自己注入到每个 owned slot
        for (int i = 0; i < ownedSlots.Length; i++)
        {
            var s = ownedSlots[i];
            if (!s) continue;
            typeof(DropZoneOwnedSlot).GetField("panel",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.SetValue(s, this);
        }

        BuildCardsFromLibrary();
        SyncFromPlayerSkills();
        UpdateContinueInteractable();
    }

    void OnEnable()
    {
        if (playerSkills != null) playerSkills.OnChanged += HandleEquipChanged;
    }
    void OnDisable()
    {
        if (playerSkills != null) playerSkills.OnChanged -= HandleEquipChanged;
    }

    void HandleEquipChanged() => UpdateContinueInteractable();

    void UpdateContinueInteractable()
    {
        int cnt = playerSkills ? playerSkills.Actives.Count : 0;
        if (shopUI) shopUI.SetNextInteractable(cnt >= 1); // 至少1个才能继续:contentReference[oaicite:4]{index=4}
    }

    // ====== 构建左侧卡池 ======
    void BuildCardsFromLibrary()
    {
        _cards.Clear();
        if (!library || library.actives == null || cardPrefab == null || !poolContent) return;

        int built = 0;
        for (int i = 0; i < library.actives.Length && built < maxPoolCards; i++)
        {
            var e = library.actives[i];
            if (e == null || e.implementation == null) continue;

            var go = Instantiate(cardPrefab, poolContent);
            string display = string.IsNullOrEmpty(e.displayName) ? e.id.ToString() : e.displayName;
            go.Init(this, (ActiveSkillId)i, e.icon, display, dragLayer);
            _cards[(ActiveSkillId)i] = go;
            built++;
        }
    }

    // ====== 同步：把已拥有技能放入右侧槽位 ======
    void SyncFromPlayerSkills()
    {
        if (!playerSkills) return;

        // 1) 先把所有卡片回收至左侧
        foreach (var kv in _cards)
        {
            var card = kv.Value;
            if (card) card.transform.SetParent(poolContent, false);
        }

        // 2) 依次把已装备的放到右侧空槽
        int nextSlot = 0;
        var eq = playerSkills.Actives;
        for (int i = 0; i < eq.Count && nextSlot < ownedSlots.Length; i++)
        {
            var id = eq[i];
            if (!_cards.TryGetValue(id, out var card) || !card) continue;

            // 找到下一个空槽
            int slotIdx = FindFirstEmptySlot(nextSlot);
            if (slotIdx < 0) break;
            var slot = ownedSlots[slotIdx].transform;
            card.transform.SetParent(slot, false);
            nextSlot = slotIdx + 1;
        }
    }

    int FindFirstEmptySlot(int start = 0)
    {
        for (int i = Mathf.Clamp(start, 0, ownedSlots.Length - 1); i < ownedSlots.Length; i++)
            if (ownedSlots[i] && ownedSlots[i].transform.childCount == 0)
                return i;
        for (int i = 0; i < ownedSlots.Length; i++)
            if (ownedSlots[i] && ownedSlots[i].transform.childCount == 0)
                return i;
        return -1;
    }

    // ====== 外部：投递到右侧槽位 ======
    public void HandleDropToOwned(DropZoneOwnedSlot slot, DraggableSkillCard card)
    {
        if (!slot || !card) return;
        // 槽位必须为空（简化：不做交换）
        if (slot.transform.childCount > 0) { SnapBack(card); return; }

        // 若尚未装备 → 尝试添加
        bool already = playerSkills && playerSkills.Actives.Contains(card.Id);
        if (!already)
        {
            if (!playerSkills || !playerSkills.TryAdd(card.Id))
            {
                SnapBack(card);
                return;
            }
        }

        // 成功：放到槽位内
        card.MarkDroppedTo(slot.transform);
        UpdateContinueInteractable();
    }

    // ====== 外部：投递回左侧池 ======
    public void HandleDropToPool(DraggableSkillCard card)
    {
        if (!card) return;

        // 如果当前在槽位 → 从装备里移除
        var parentSlot = card.transform.parent ? card.transform.parent.GetComponent<DropZoneOwnedSlot>() : null;
        if (parentSlot && playerSkills)
        {
            playerSkills.Remove(card.Id); // 触发 OnChanged → gating 更新:contentReference[oaicite:5]{index=5}
        }

        card.MarkDroppedTo(poolContent);
    }

    void SnapBack(DraggableSkillCard card)
    {
        // 若来自槽位，仍然留在原槽；若来自池，回池
        var parentSlot = card.transform.parent ? card.transform.parent.GetComponent<DropZoneOwnedSlot>() : null;
        if (parentSlot) card.MarkDroppedTo(parentSlot.transform);
        else            card.MarkDroppedTo(poolContent);
    }
}
